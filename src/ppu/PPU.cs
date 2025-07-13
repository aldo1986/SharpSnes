// PPU.cs
public class PPU
{
    private readonly Bus bus;

    public PPU(Bus bus)
    {
        this.bus = bus;
    }

    public byte Read(ushort address)
    {
        // El CPU está pidiendo leer un registro de la PPU
        switch (address)
        {
            // PPU Status Register
            case 0x213F:
                // Devolvemos un valor de ejemplo. El bit 7 (0x80) indica
                // que la PPU está en V-Blank. Muchos juegos esperan esto.
                return 0x80; 

            default:
                return 0;
        }
    }

    public void Write(ushort address, byte data)
    {
        
    }
}