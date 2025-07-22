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
    public void LoadRom(byte[] rom)
    {
        Console.WriteLine($"Cargando ROM de {rom.Length} bytes.");
        this.romData = rom;
        // Aquí leeríamos el header de la ROM para detectar si es LoROM, HiROM, etc.
        // Por ahora, asumiremos que todas son LoROM.
    }
    public void TriggerNMI()
    {
        cpu.RequestNMI();
    }

    public byte Read(ushort address)
    {
        int bank = address >> 8; // Simplificado, el banco real es más complejo

        // Mapeo de WRAM ($0000-$1FFF en los bancos $7E y $7F)
        if (address >= 0x0000 && address <= 0x1FFF)
        {
            return wram[address];
        }

        // Mapeo de Registros PPU ($2100-$21FF)
        if (address >= 0x2100 && address <= 0x21FF)
        {
            return _ppu.Read(address);
        }

        // Mapeo de Controles ($4016)
        if (address == 0x4016)
        {
            return controller1.Read();
        }

        // Mapeo de la ROM (LoROM)
        // El contenido de la ROM se mapea en la mitad superior de los bancos ($8000-$FFFF)
        if (address >= 0x8000)
        {
            // Calculamos el índice en el array de la ROM
            // Esto es una simplificación, pero funciona para muchas ROMs LoROM.
            int romAddress = (bank * 0x8000) + (address & 0x7FFF);
            if (romAddress < romData.Length)
            {
                return romData[romAddress];
            }
        }

        return 0; // Dirección no mapeada
    }

    public void Write(ushort address, byte data)
    {
        int bank = address >> 8;

        // Mapeo de WRAM
        if (address >= 0x0000 && address <= 0x1FFF)
        {
            wram[address] = data;
            return;
        }

        // Mapeo de Registros PPU
        if (address >= 0x2100 && address <= 0x21FF)
        {
            _ppu.Write(address, data);
            return;
        }

        // Mapeo de Controles
        if (address == 0x4016)
        {
            if ((data & 1) == 0) controller1.Strobe();
            return;
        }
    }
}