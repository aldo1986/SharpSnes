// Program.cs
using Silk.NET.Windowing;
using Silk.NET.OpenGL;
using System.Drawing;
using Silk.NET.Maths;
using Silk.NET.Input;

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
    private static IKeyboard primaryKeyboard;
    private static int spriteX = 100;
    private static int spriteY = 100;
    private static int currentScanline = 0;

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
        var inputContext = window.CreateInput();
        primaryKeyboard = inputContext.Keyboards.FirstOrDefault();
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
        bus.ConnectCPU(cpu);
        ppu = bus.Ppu;

        Console.WriteLine("Cargando datos para un fondo de 4bpp (16 colores)...");

        // --- 1. CARGAR UNA PALETA DE 16 COLORES ---
        ppu.Write(0x2121, 0x00); // Apuntar al inicio de CGRAM (Paleta #0)

        // Color 0 es transparente
        ppu.Write(0x2122, 0x00); ppu.Write(0x2122, 0x00);

        // Cargaremos un gradiente de rojo
        for (int i = 1; i < 16; i++)
        {
            // El valor de rojo va de 2 a 31
            byte redValue = (byte)(i * 2);
            ushort color = (ushort)(redValue & 0x1F); // Formato BGR555, solo componente R
            ppu.Write(0x2122, (byte)(color & 0xFF));
            ppu.Write(0x2122, (byte)(color >> 8));
        }

        // --- 2. CARGAR DATOS DE UN TILE DE 4BPP (32 bytes) ---
        // Este tile será un cuadrado que usa los 16 colores de la paleta.
        byte[] tile4bpp = new byte[32];
        for (int i = 0; i < 8; i++) // Para cada fila (8)
        {
            // Plano 0
            tile4bpp[i * 2] = 0b11110000;
            // Plano 1
            tile4bpp[i * 2 + 1] = 0b11001100;
            // Plano 2
            tile4bpp[i * 2 + 16] = 0b10101010;
            // Plano 3 (no se usa en esta prueba)
            tile4bpp[i * 2 + 17] = 0b00000000;
        }

        // Escribimos el tile en la nueva dirección base para tiles 4bpp (VRAM $2020 para Tile #1)
        ppu.Write(0x2116, 0x20); ppu.Write(0x2117, 0x20);
        for (int i = 0; i < tile4bpp.Length; i++) { ppu.Write(0x2118, tile4bpp[i]); }

        // --- 3. CREAR EL TILEMAP ---
        // Haremos que toda la pantalla muestre nuestro nuevo Tile #1
        ppu.Write(0x2116, 0x00); ppu.Write(0x2117, 0x10); // Apuntar a VRAM $1000
        for (int i = 0; i < 32 * 32; i++)
        {
            ushort tileInfo = 1; // Tile #1, Paleta #0
            ppu.Write(0x2118, (byte)(tileInfo & 0xFF));
            ppu.Write(0x2118, (byte)(tileInfo >> 8));
        }

        // Por ahora, quitamos la animación y los sprites para enfocarnos en el fondo
        // window.Update += ...

        window.Run();
    }
}