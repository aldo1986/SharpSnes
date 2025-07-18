// Bus.cs
public class Bus
{
    // El Bus ahora "posee" los componentes principales
    private readonly PPU _ppu;
     private CPU cpu; // No es readonly para poder conectarlo después
    private readonly byte[] wram = new byte[128 * 1024];
    private byte[] romData = new byte[65536];
    public PPU Ppu => _ppu;
    private readonly Controller controller1 = new Controller();
    public Controller Controller1 => controller1;

    public Bus()
    {
        this._ppu = new PPU(this);
    }
    public void ConnectCPU(CPU cpu)
    {
        this.cpu = cpu;
    }
    
    // El resto de los métodos de Bus...
    public void LoadRom(byte[] rom) { /* sin cambios */ }
    public void TriggerNMI()
    {
        cpu.RequestNMI();
    }

    public byte Read(ushort address)
    {
        // --- NUEVA RUTA PARA LA PPU ---
        // Los registros de la PPU están en este rango
        if (address == 0x4016)
        {
            return controller1.Read();
        }
        if (address >= 0x2100 && address <= 0x21FF)
        {
            return _ppu.Read(address);
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
        if (address == 0x4016)
        {
            // Escribir un 1 y luego un 0 en $4016 resetea el contador de botones
            if ((data & 1) == 0)
            {
                controller1.Strobe();
            }
        }
        // --- NUEVA RUTA PARA LA PPU ---
        if (address >= 0x2100 && address <= 0x21FF)
        {
            _ppu.Write(address, data);
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