public class Bus
{
    private CPU cpu;
    public readonly PPU ppu;
    public readonly Controller controller1 = new Controller();
    
    private readonly byte[] wram = new byte[128 * 1024];
    private byte[] romData;
    private int romHeaderSize = 0;

    public Bus()
    {
        this.ppu = new PPU(this);
    }

    public void ConnectCPU(CPU cpu)
    {
        this.cpu = cpu;
    }

    public void LoadRom(byte[] rom)
    {
        this.romData = rom;
        if ((rom.Length % 1024) == 512)
        {
            this.romHeaderSize = 512;
            Console.WriteLine("Encabezado de 512 bytes detectado y omitido.");
        }
    }

    public void TriggerNMI()
    {
        cpu?.RequestNMI();
    }

    public byte Read(uint address)
    {
        byte bank = (byte)(address >> 16);
        ushort offset = (ushort)(address & 0xFFFF);

        // Mapeo de WRAM y Registros
        if ((bank >= 0x00 && bank <= 0x3F) || (bank >= 0x80 && bank <= 0xBF))
        {
            if (offset < 0x2000) return wram[offset];
            if (offset >= 0x2100 && offset <= 0x21FF) return ppu.Read(offset);
            if (offset == 0x4016) return controller1.Read();
        }

        // Mapeo de ROM (LoROM)
        // La ROM se mapea en la mitad superior de los bancos ($8000-$FFFF)
        if (offset >= 0x8000)
        {
            // Fórmula de mapeo LoROM que convierte una dirección de CPU a un índice de archivo
            uint romAddress = (uint)(((bank & 0x7F) * 32768) + (offset - 0x8000)) + (uint)romHeaderSize;
            if (romAddress < romData.Length)
            {
                return romData[romAddress];
            }
        }
        if (offset == 0x4218) // JOY1L - Low byte del control 1
        {
            return (byte)(controller1.JoypadState & 0xFF);
        }
        if (offset == 0x4219) // JOY1H - High byte del control 1
        {
            return (byte)(controller1.JoypadState >> 8);
        }

        return 0; // Bus abierto
    }

    public void Write(uint address, byte data)
    {
        byte bank = (byte)(address >> 16);
        ushort offset = (ushort)(address & 0xFFFF);
        
        if ((bank >= 0x00 && bank <= 0x3F) || (bank >= 0x80 && bank <= 0xBF))
        {
            if (offset < 0x2000) { wram[offset] = data; return; }
            if (offset >= 0x2100 && offset <= 0x21FF) { ppu.Write(offset, data); return; }
            if (offset == 0x4016) { if ((data & 1) == 0) controller1.Strobe(); return; }
        }
    }
}