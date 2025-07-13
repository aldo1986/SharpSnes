// PPU.cs
public class PPU
{
    private readonly Bus bus;

    // Memorias internas de la PPU
    private readonly byte[] vram = new byte[64 * 1024]; // 64KB VRAM
    private readonly byte[] oam = new byte[544];        // OAM para 128 sprites
    private readonly byte[] cgram = new byte[512];      // CGRAM para la paleta de 256 colores

    // Registros internos de la PPU (estado simulado)
    private byte screenBrightness = 0;
    private ushort vramAddress = 0;

    public PPU(Bus bus)
    {
        this.bus = bus;
    }

    public byte Read(ushort address)
    {
        switch (address)
        {
            case 0x213F: // PPU Status Register 1
                return 0x80; // Devolvemos V-Blank como antes

            // Este es el puerto para leer datos de la VRAM
            case 0x2139: // VRAM Data Read
            {
                byte data = vram[vramAddress];
                // Si el modo de incremento está activo (lo estará por defecto),
                // la dirección de VRAM se incrementa después de cada lectura/escritura.
                vramAddress++;
                return data;
            }

            default:
                return 0;
        }
    }

    public void Write(ushort address, byte data)
    {
        // El CPU está escribiendo en un registro de la PPU
        switch (address)
        {
            // --- Registro de Brillo de Pantalla ---
            case 0x2100:
                // Los 4 bits inferiores controlan el brillo
                this.screenBrightness = (byte)(data & 0x0F);
                Console.WriteLine($"[PPU] Brillo de pantalla establecido en: {this.screenBrightness}");
                break;
            
            // --- Registros de Acceso a VRAM ---
            case 0x2116: // VRAM Address Low Byte
                // Establece el byte bajo de la dirección de VRAM a la que queremos acceder
                vramAddress = (ushort)((vramAddress & 0xFF00) | data);
                break;
            case 0x2117: // VRAM Address High Byte
                // Establece el byte alto de la dirección de VRAM
                vramAddress = (ushort)((vramAddress & 0x00FF) | (data << 8));
                break;
            case 0x2118: // VRAM Data Write
                // Escribe un dato en la VRAM en la dirección actual
                vram[vramAddress] = data;
                // Luego incrementa la dirección para la siguiente escritura
                vramAddress++;
                break;
        }
    }
}