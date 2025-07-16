// Program.cs
using Silk.NET.Windowing;
using Silk.NET.OpenGL;
using System.Drawing;
using Silk.NET.Maths;

class Program
{
    private static IWindow window;
    private static GL Gl;

    // Emulador
    private static Bus bus;
    private static CPU cpu;
    private static PPU ppu;

    // Renderizado
    private static byte[] pixelBuffer = new byte[256 * 224 * 4];
    private static uint TextureHandle;
    private static uint Vao;
    private static uint Vbo;
    private static uint Ebo;
    private static uint ShaderProgram;

    // Datos del cuadrado que llenará la pantalla
    private static readonly float[] Vertices =
    {
        // Posición        Textura Coords
         1.0f,  1.0f, 0.0f, 1.0f, 0.0f, // Top-right
         1.0f, -1.0f, 0.0f, 1.0f, 1.0f, // Bottom-right
        -1.0f, -1.0f, 0.0f, 0.0f, 1.0f, // Bottom-left
        -1.0f,  1.0f, 0.0f, 0.0f, 0.0f  // Top-left
    };

    private static readonly uint[] Indices =
    {
        0, 1, 3,
        1, 2, 3
    };

    // Shaders (pequeños programas que corren en la GPU)
    private static readonly string VertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 aPos;
        layout (location = 1) in vec2 aTexCoord;

        out vec2 TexCoord;

        void main()
        {
            gl_Position = vec4(aPos, 1.0);
            TexCoord = aTexCoord;
        }";

    private static readonly string FragmentShaderSource = @"
        #version 330 core
        out vec4 FragColor;

        in vec2 TexCoord;

        uniform sampler2D ourTexture;

        void main()
        {
            FragColor = texture(ourTexture, TexCoord);
        }";




    private static unsafe void OnLoad()
    {
        Gl = window.CreateOpenGL();

        // --- Configuración de Buffers y Shaders ---
        Vao = Gl.GenVertexArray();
        Gl.BindVertexArray(Vao);

        Vbo = Gl.GenBuffer();
        Gl.BindBuffer(BufferTargetARB.ArrayBuffer, Vbo);
        fixed (float* buf = Vertices)
        {
            Gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(Vertices.Length * sizeof(float)), buf, BufferUsageARB.StaticDraw);
        }

        Ebo = Gl.GenBuffer();
        Gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, Ebo);
        fixed (uint* buf = Indices)
        {
            Gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(Indices.Length * sizeof(uint)), buf, BufferUsageARB.StaticDraw);
        }

        // Compilar Shaders
        uint vertexShader = Gl.CreateShader(ShaderType.VertexShader);
        Gl.ShaderSource(vertexShader, VertexShaderSource);
        Gl.CompileShader(vertexShader);

        uint fragmentShader = Gl.CreateShader(ShaderType.FragmentShader);
        Gl.ShaderSource(fragmentShader, FragmentShaderSource);
        Gl.CompileShader(fragmentShader);

        // Crear Programa de Shaders
        ShaderProgram = Gl.CreateProgram();
        Gl.AttachShader(ShaderProgram, vertexShader);
        Gl.AttachShader(ShaderProgram, fragmentShader);
        Gl.LinkProgram(ShaderProgram);
        Gl.DeleteShader(vertexShader);
        Gl.DeleteShader(fragmentShader);

        // Definir cómo leer los datos del VBO
        Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), (void*)0);
        Gl.EnableVertexAttribArray(0);
        Gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), (void*)(3 * sizeof(float)));
        Gl.EnableVertexAttribArray(1);

        // --- Configuración de la Textura (igual que antes) ---
        TextureHandle = Gl.GenTexture();
        Gl.BindTexture(TextureTarget.Texture2D, TextureHandle);
        Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
        Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
        Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
        Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
    }

    private static unsafe void OnRender(double deltaTime)
    {
        // 1. Generar el frame en la PPU
        ppu.RenderFrame(pixelBuffer);

        // 2. Subir los datos a la textura en la GPU
        Gl.BindTexture(TextureTarget.Texture2D, TextureHandle);
        fixed (byte* ptr = pixelBuffer)
        {
            Gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, 256, 224, 0, PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
        }

        // 3. Limpiar, activar shaders y dibujar
        Gl.ClearColor(Color.CornflowerBlue);
        Gl.Clear((uint)ClearBufferMask.ColorBufferBit);

        Gl.UseProgram(ShaderProgram);
        Gl.BindVertexArray(Vao);

        // Esta única llamada reemplaza todo el bloque Gl.Begin/End
        Gl.DrawElements(PrimitiveType.Triangles, (uint)Indices.Length, DrawElementsType.UnsignedInt, null);
    }

    private static void OnClose()
    {
        // Liberar recursos
        Gl.DeleteBuffer(Vbo);
        Gl.DeleteBuffer(Ebo);
        Gl.DeleteVertexArray(Vao);
        Gl.DeleteProgram(ShaderProgram);
        Gl.DeleteTexture(TextureHandle);
        Gl.Dispose();
    }
    static void Main(string[] args)
    {
        // --- 1. CONFIGURACIÓN INICIAL ---
        var options = WindowOptions.Default;
        options.Size = new Vector2D<int>(256 * 2, 224 * 2);
        options.Title = "Mi Emulador de SNES - ¡Versión Final!";

        window = Window.Create(options);
        window.Load += OnLoad;
        window.Render += OnRender;
        window.Closing += OnClose;

        bus = new Bus();
        cpu = new CPU(bus);
        ppu = bus.Ppu;

        Console.WriteLine("Cargando todos los datos en la PPU...");

        // --- 2. CARGAR PALETAS DE COLORES (VERSIÓN CORREGIDA Y COMPLETA) ---
        ppu.Write(0x2121, 0x00); // Apuntar al inicio de CGRAM (Color #0)

        // Paleta del Fondo (Paleta #0)
        ppu.Write(0x2122, 0x00); ppu.Write(0x2122, 0x00); // Color 0 (Transparente/Negro)
        ppu.Write(0x2122, 0xFF); ppu.Write(0x2122, 0x03); // Color 1 (Amarillo = 0x03FF)

        // Paleta del Sprite (Paleta #8)
        ppu.Write(0x2121, 128); // Apuntar al inicio de la Paleta #8 (Color #128)
        ppu.Write(0x2122, 0x00); ppu.Write(0x2122, 0x00); // Color 0 de la paleta del sprite (Transparente)
        ppu.Write(0x2122, 0x1F); ppu.Write(0x2122, 0x00); // Color 1 de la paleta del sprite (Rojo = 0x001F)

        // --- 3. CARGAR DATOS DE TILES ---
        byte[] tileData = { 0b00111100, 0b00111100, 0b01000010, 0b01000010, 0b10100101, 0b10000001, 0b10000001, 0b10000001, 0b10100101, 0b10100101, 0b10011001, 0b10011001, 0b01000010, 0b01000010, 0b00111100, 0b00111100 };
        ppu.Write(0x2116, 0x10); ppu.Write(0x2117, 0x00);
        for (int i = 0; i < tileData.Length; i++) { ppu.Write(0x2118, tileData[i]); }

        byte[] spriteTileData = { 0x55, 0x00, 0x55, 0x00, 0x55, 0x00, 0x55, 0x00, 0x55, 0x00, 0x55, 0x00, 0x55, 0x00, 0x55, 0x00 };
        ppu.Write(0x2116, 0x20); ppu.Write(0x2117, 0x00);
        for (int i = 0; i < spriteTileData.Length; i++) { ppu.Write(0x2118, spriteTileData[i]); }

        // --- 4. CREAR EL TILEMAP DEL FONDO ---
        ppu.Write(0x2116, 0x00); ppu.Write(0x2117, 0x10);
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                ushort tileInfo = (x % 4 == 0 && y % 4 == 0) ? (ushort)1 : (ushort)0;
                ppu.Write(0x2118, (byte)(tileInfo & 0xFF));
                ppu.Write(0x2118, (byte)(tileInfo >> 8));
            }
        }

        // --- 5. CONFIGURAR EL SPRITE EN LA OAM ---
        ppu.Write(0x2102, 0x00);
        ppu.Write(0x2104, 100);
        ppu.Write(0x2104, 100);
        ppu.Write(0x2104, 2);
        ppu.Write(0x2104, 0x00);

        // --- 6. MOTOR DE ANIMACIÓN ---
        int scrollX = 0;
        int frameCount = 0;
        window.Update += (deltaTime) =>
        {
            scrollX++;
            frameCount++;
            bus.Write(0x210D, (byte)(scrollX & 0xFF));
            bus.Write(0x210F, (byte)((scrollX >> 8) & 0x01));
            bus.Write(0x2102, 0);
            bus.Write(0x2104, (byte)(100 + Math.Sin(frameCount * 0.05) * 20));
            bus.Write(0x2104, (byte)(100 + Math.Cos(frameCount * 0.05) * 20));
        };

        // --- 7. INICIAR LA VENTANA ---
        window.Run();
    }
}