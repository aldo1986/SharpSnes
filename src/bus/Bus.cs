// Bus.cs
public class Bus
{
    // La SNES tenía 128KB de Work RAM (WRAM)
    private readonly byte[] wram = new byte[128 * 1024];
    private byte[] romData = new byte[65536];

    // Método para cargar la ROM en el bus
    public void LoadRom(byte[] rom)
    {
        rom.CopyTo(romData, 0);
    }

    // El CPU llamará a este método para leer de la memoria
    public byte Read(ushort address)
    {
        // Aquí es donde ocurre la magia del ruteo.
        // Por ahora, solo tenemos dos rutas: RAM y ROM.
        
        // Mapeo simple para la WRAM (ej. banco $00, direcciones $0000-$1FFF)
        if (address >= 0x0000 && address <= 0x1FFF)
        {
            return wram[address];
        }

        // Mapeo simple para la ROM (ej. banco $80, direcciones $8000 en adelante)
        if (address >= 0x8000)
        {
            // El PC del CPU es de 16-bit, pero el código de la ROM
            // está al principio de nuestro array romData.
            // Hacemos un mapeo simple para que $8000 lea desde el inicio del array.
            return romData[address];
        }
        
        // Si la dirección no está mapeada, devolvemos 0.
        return 0;
    }

    // El CPU llamará a este método para escribir en la memoria
    public void Write(ushort address, byte data)
    {
        // Por ahora, solo podemos escribir en la WRAM
        if (address >= 0x0000 && address <= 0x1FFF)
        {
            wram[address] = data;
        }
        else if (address >= 0x8000)
        {
            romData[address] = data;
        }
        // Intentar escribir en la ROM no hace nada.
    }
}