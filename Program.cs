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

        Console.WriteLine("Probando instrucciones de subrutinas y pila...");

    // --- CÓDIGO DE PRUEBA ---
    
    // Subrutina en $9000: Carga 99 en A y regresa.
    bus.Write(0x9000, 0xA9); bus.Write(0x9001, 99); // LDA #99
    bus.Write(0x9002, 0x60);                      // RTS

    // Programa Principal en $8000
    int codePtr = 0x8000;
    bus.Write((ushort)codePtr++, 0xA9); bus.Write((ushort)codePtr++, 55); // LDA #55      (A = 55)
    bus.Write((ushort)codePtr++, 0x48);                                  // PHA          (Guardar A=55 en la pila)
    bus.Write((ushort)codePtr++, 0x20); bus.Write((ushort)codePtr++, 0x00); bus.Write((ushort)codePtr++, 0x90); // JSR $9000  (Llamar subrutina, A se volverá 99)
    bus.Write((ushort)codePtr++, 0x68);                                  // PLA          (Recuperar A de la pila, A vuelve a ser 55)
    bus.Write((ushort)codePtr++, 0x00);                                  // BRK          (Fin)

    // --- EJECUCIÓN ---
    cpu.Reset();
    cpu.PC = 0x8000;
    
    // Ejecutamos paso a paso hasta el BRK
    for (int i = 0; i < 10; i++) // Un número suficiente de pasos para terminar
    {
        cpu.Step();
    }
    
    Console.WriteLine("Ejecución finalizada.");
    Console.WriteLine("--- Verificación Final ---");
    Console.WriteLine($"Valor final del registro A: {cpu.A} (Esperado: 55)");

    if (cpu.A == 55)
    {
        Console.WriteLine("✅ ¡Éxito! Las subrutinas y la pila funcionan correctamente.");
    }
    else
    {
        Console.WriteLine("❌ Error en la lógica de subrutinas o pila.");
    }

    // Quitamos la ventana por ahora para simplificar la prueba
    // window.Run(); 
    }
}