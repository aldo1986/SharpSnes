public class CPU
{
    // Registros
    public ushort PC { get; set; }
    public byte A { get; set; }
    public byte X { get; set; }
    public byte Y { get; set; }
    public StatusFlags P { get; set; }

    // Conexión al Bus de Memoria
    private readonly Bus bus;

    public CPU(Bus bus)
    {
        this.bus = bus;
    }

    private void SetZeroAndNegativeFlags(byte value)
    {
        P = (value == 0) ? (P | StatusFlags.Zero) : (P & ~StatusFlags.Zero);
        P = ((value & 0x80) != 0) ? (P | StatusFlags.Negative) : (P & ~StatusFlags.Negative);
    }

    public void Run()
    {
        bool isRunning = true;
        while (isRunning)
        {
            byte opcode = bus.Read(PC++);

            switch (opcode)
            {
                case 0x00: // BRK
                    isRunning = false;
                    break;

                // --- INSTRUCCIONES DE CARGA (LOAD) ---
                case 0xA9: // LDA Inmediato
                    A = bus.Read(PC++);
                    SetZeroAndNegativeFlags(A);
                    break;
                case 0xAD: // LDA Absoluto
                {
                    ushort address = (ushort)(bus.Read(PC++) | (bus.Read(PC++) << 8));
                    A = bus.Read(address);
                    SetZeroAndNegativeFlags(A);
                    break;
                }
                case 0xA2: // LDX Inmediato
                    X = bus.Read(PC++);
                    SetZeroAndNegativeFlags(X);
                    break;

                // --- INSTRUCCIONES DE ALMACENAMIENTO (STORE) ---
                case 0x8D: // STA Absoluto
                {
                    ushort address = (ushort)(bus.Read(PC++) | (bus.Read(PC++) << 8));
                    bus.Write(address, A);
                    break;
                }
                case 0x85: // STA Página Cero
                {
                    byte zeroPageAddress = bus.Read(PC++);
                    bus.Write(zeroPageAddress, A);
                    break;
                }

                // --- INSTRUCCIONES ARITMÉTICAS ---
                case 0x7D: // ADC Absoluto,X
                {
                    ushort baseAddress = (ushort)(bus.Read(PC++) | (bus.Read(PC++) << 8));
                    ushort finalAddress = (ushort)(baseAddress + X);
                    byte value = bus.Read(finalAddress);
                    
                    int carry = P.HasFlag(StatusFlags.Carry) ? 1 : 0;
                    int sum = A + value + carry;
                    P = (sum > 255) ? (P | StatusFlags.Carry) : (P & ~StatusFlags.Carry);
                    P = (((A ^ sum) & (value ^ sum) & 0x80) != 0) ? (P | StatusFlags.Overflow) : (P & ~StatusFlags.Overflow);
                    A = (byte)sum;
                    SetZeroAndNegativeFlags(A);
                    break;
                }
                case 0x65: // ADC Página Cero
                {
                    byte zeroPageAddress = bus.Read(PC++);
                    byte value = bus.Read(zeroPageAddress);
                    
                    int carry = P.HasFlag(StatusFlags.Carry) ? 1 : 0;
                    int sum = A + value + carry;
                    P = (sum > 255) ? (P | StatusFlags.Carry) : (P & ~StatusFlags.Carry);
                    P = (((A ^ sum) & (value ^ sum) & 0x80) != 0) ? (P | StatusFlags.Overflow) : (P & ~StatusFlags.Overflow);
                    A = (byte)sum;
                    SetZeroAndNegativeFlags(A);
                    break;
                }
                
                // --- INCREMENTOS Y COMPARACIONES ---
                case 0xE8: // INX
                    X++;
                    SetZeroAndNegativeFlags(X);
                    break;
                case 0xE0: // CPX Inmediato
                {
                    byte value = bus.Read(PC++);
                    byte result = (byte)(X - value);
                    SetZeroAndNegativeFlags(result);
                    P = (X >= value) ? (P | StatusFlags.Carry) : (P & ~StatusFlags.Carry);
                    break;
                }

                // --- SALTOS Y BIFURCACIONES ---
                case 0x4C: // JMP Absoluto
                    PC = (ushort)(bus.Read(PC) | (bus.Read((ushort)(PC + 1)) << 8));
                    break;
                case 0xD0: // BNE Relativo
                {
                    sbyte offset = (sbyte)bus.Read(PC++);
                    if (!P.HasFlag(StatusFlags.Zero))
                    {
                        PC = (ushort)(PC + offset);
                    }
                    break;
                }

                default:
                    Console.WriteLine($"Opcode desconocido: {opcode:X2} en la dirección ${PC - 1:X4}");
                    isRunning = false;
                    break;
            }
        }
    }
}