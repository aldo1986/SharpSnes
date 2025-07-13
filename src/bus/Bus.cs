// Bus.cs
public class Bus
{
    // El Bus ahora "posee" los componentes principales
    private readonly PPU ppu;
    private readonly byte[] wram = new byte[128 * 1024];
    private byte[] romData = new byte[65536];

    public Bus()
    {
        // Creamos la PPU al iniciar el Bus
        this.ppu = new PPU(this);
    }
    
    // El resto de los métodos de Bus...
    public void LoadRom(byte[] rom) { /* sin cambios */ }

    public byte Read(ushort address)
    {
        // --- NUEVA RUTA PARA LA PPU ---
        // Los registros de la PPU están en este rango
        if (address >= 0x2100 && address <= 0x21FF)
        {
            return ppu.Read(address);
        }
        
        if (address >= 0x0000 && address <= 0x1FFF)
        {
            return wram[address];
        }

        if (address >= 0x8000)
        {
            return romData[address];
        }
        
        return 0;
    }

    public void Write(ushort address, byte data)
    {
        // --- NUEVA RUTA PARA LA PPU ---
        if (address >= 0x2100 && address <= 0x21FF)
        {
            ppu.Write(address, data);
            return; // Importante para no seguir evaluando
        }
        
        if (address >= 0x0000 && address <= 0x1FFF)
        {
            wram[address] = data;
        }
        else if (address >= 0x8000)
        {
            romData[address] = data;
        }
    }
}