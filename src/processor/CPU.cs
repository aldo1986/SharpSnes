public class CPU
{
    // Registros
    public ushort PC { get; set; }
    public byte A { get; set; }
    public byte X { get; set; }
    public byte Y { get; set; }
    public byte SP { get; set; }
    public StatusFlags P { get; set; }

    // Conexión al Bus de Memoria
    private readonly Bus bus;
    private bool nmiPending = false;

    public CPU(Bus bus)
    {
        this.bus = bus;
    }
    public void RequestNMI()
    {

        Console.WriteLine("❤️ [CPU] ¡Interrupción NMI recibida!");
        nmiPending = true;
    }
    private void Push(byte data)
    {
        // La pila en la SNES vive en la página $01 de la RAM ($0100-$01FF)
        // y crece hacia abajo.
        bus.Write((ushort)(0x0100 + SP), data);
        SP--;
    }
    // Método para sacar un byte de la pila
    private byte Pop()
    {
        SP++;
        return bus.Read((ushort)(0x0100 + SP));
    }
    private void HandleNMI()
    {
        Console.WriteLine("❤️ [CPU] Atendiendo NMI...");
        nmiPending = false;

        // 1. Guardar el estado actual en la pila
        Push((byte)(PC >> 8));   // Guardar byte alto del PC
        Push((byte)(PC & 0xFF)); // Guardar byte bajo del PC
        Push((byte)P);           // Guardar registro de estado

        // 2. Saltar a la dirección del vector NMI
        ushort lowByte = bus.Read(0xFFFA);
        ushort highByte = bus.Read(0xFFFB);
        PC = (ushort)(lowByte | (highByte << 8));
        Console.WriteLine($"❤️ [CPU] Saltando al manejador de NMI en ${PC:X4}");
    }
    public void Reset()
    {
        // ... (código de Reset) ...
        SP = 0xFF; // La pila empieza en la parte alta y crece hacia abajo
    }



    private void SetZeroAndNegativeFlags(byte value)
    {
        P = (value == 0) ? (P | StatusFlags.Zero) : (P & ~StatusFlags.Zero);
        P = ((value & 0x80) != 0) ? (P | StatusFlags.Negative) : (P & ~StatusFlags.Negative);
    }
    public void Step()
    {
        // Revisa si hay una interrupción NMI pendiente antes de ejecutar la instrucción.
        if (nmiPending)
        {
            HandleNMI();
        }

        // Ejecuta una sola instrucción.
        byte opcode = bus.Read(PC++);

        // El switch con todos los opcodes va aquí, sin el bucle while.
        switch (opcode)
        {
            // ... todos tus 'case' para las instrucciones ...

            // El case 0x00 (BRK) ya no detiene un bucle, sino que podría ser usado
            // para detener el emulador si quisiéramos. Por ahora, no hace nada.
            case 0x00:
                break;
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
            case 0xE6: // INC Zero Page
                {
                    byte address = bus.Read(PC++);
                    byte value = bus.Read(address);
                    value++;
                    bus.Write(address, value);
                    SetZeroAndNegativeFlags(value);
                    break;
                }
            case 0x40: // RTI - Return from Interrupt
                       // Hacemos lo opuesto a la interrupción: sacamos el estado de la pila.
                P = (StatusFlags)Pop();
                byte lo = Pop();
                byte hi = Pop();
                PC = (ushort)(lo | (hi << 8));
                Console.WriteLine($"↪️ [CPU] Regresando de la interrupción a ${PC:X4}");
                break;
            case 0x48: // PHA - Push Accumulator
                Push(A);
                break;

            case 0x68: // PLA - Pull Accumulator
                A = Pop();
                SetZeroAndNegativeFlags(A);
                break;
            // --- INSTRUCCIONES DE SUBRUTINA ---

            case 0x20: // JSR - Jump to Subroutine
                {
                    // Leemos la dirección de la subrutina
                    ushort subAddr = (ushort)(bus.Read(PC++) | (bus.Read(PC++) << 8));

                    // Guardamos la dirección de retorno (la instrucción actual - 1) en la pila
                    ushort returnAddr = (ushort)(PC - 1);
                    Push((byte)(returnAddr >> 8));   // Byte alto primero
                    Push((byte)(returnAddr & 0xFF)); // Byte bajo después

                    // Saltamos a la subrutina
                    PC = subAddr;
                    break;
                }

            case 0x60: // RTS - Return from Subroutine
                {
                    // Sacamos la dirección de retorno de la pila
                    byte low = Pop();
                    byte high = Pop();
                    ushort returnAddr = (ushort)(low | (high << 8));

                    // Apuntamos el PC a la siguiente instrucción después de la llamada original
                    PC = (ushort)(returnAddr + 1);
                    break;
                }


            default:
                // Detenemos la ejecución si no conocemos el opcode.
                Console.WriteLine($"❌ Opcode Desconocido: ${opcode:X2} en la dirección ${PC - 1:X4}");
                // Para detener todo el emulador, podríamos cerrar la ventana.
                // window.Close(); // (Esta línea la pondríamos en Program.cs)
                break;
        }
    }
}