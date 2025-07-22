public class CPU
{
    private ushort _c; 
    // Registros
    public ushort PC { get; set; }
    public byte A
    {
        get => (byte)(_c & 0xFF);
        set => _c = (ushort)((_c & 0xFF00) | value);
    }
    public byte B // Acumulador B (parte alta de C)
    {
        get => (byte)(_c >> 8);
        set => _c = (ushort)((value << 8) | (_c & 0xFF));
    }
    public ushort C // Acumulador C de 16-bit
    {
        get => _c;
        set => _c = value;
    }
    public ushort SP { get; set; } // Stack Pointer ahora es de 16-bit
    public ushort DP { get; set; }
    public byte DBR { get; set; }
    public byte PBR { get; set; }
    public byte X { get; set; }
    public byte Y { get; set; }
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
        bus.Write(SP, data);
        SP--;
    }
    // Método para sacar un byte de la pila
    private byte Pop()
    {
        SP++;
        return bus.Read(SP);
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
        DP = 0;
        DBR = 0;
        PBR = 0;
        SP = 0x01FF; // La pila empieza en la parte alta y crece hacia abajo
    }



    private void SetZeroAndNegativeFlags(byte value)
    {
        P = (value == 0) ? (P | StatusFlags.Zero) : (P & ~StatusFlags.Zero);
        P = ((value & 0x80) != 0) ? (P | StatusFlags.Negative) : (P & ~StatusFlags.Negative);
    }
    private void SetZeroAndNegativeFlags16(ushort value)
    {
        P = (value == 0) ? (P | StatusFlags.Zero) : (P & ~StatusFlags.Zero);
        // El flag Negative se basa en el bit 15
        P = ((value & 0x8000) != 0) ? (P | StatusFlags.Negative) : (P & ~StatusFlags.Negative);
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
            case 0x66: // ROR Zero Page
                {
                    byte address = bus.Read(PC++);
                    byte value = bus.Read(address);
                    bool oldCarry = P.HasFlag(StatusFlags.Carry);

                    // El nuevo Carry será el bit 0 del valor original
                    if ((value & 1) == 1)
                    {
                        P |= StatusFlags.Carry;
                    }
                    else
                    {
                        P &= ~StatusFlags.Carry;
                    }

                    // Rotar el valor hacia la derecha
                    value >>= 1;

                    // Si el Carry antiguo era 1, establecer el bit 7 del nuevo valor
                    if (oldCarry)
                    {
                        value |= 0x80;
                    }

                    bus.Write(address, value);
                    SetZeroAndNegativeFlags(value);
                    break;
                }
            case 0x58: // CLI - Clear Interrupt Disable
                // // Borramos el flag de "InterruptDisable" usando una operación AND
                // // con el complemento de bits del flag.
                P &= ~StatusFlags.InterruptDisable;
                break;
            case 0x4B: // PHK - Push Program Bank Register
                Push(PBR);
                break;
            case 0xE4: // CPX Zero Page
                {
                    // Leer la dirección de 8-bit de la página cero.
                    byte address = bus.Read(PC++);
                    // Obtener el valor desde esa dirección en RAM.
                    byte value = bus.Read(address);
                    // Realizar la comparación (una resta interna).
                    byte result = (byte)(X - value);

                    // Actualizar los flags.
                    SetZeroAndNegativeFlags(result);
                    // El flag de Carry se activa si X >= valor.
                    P = (X >= value) ? (P | StatusFlags.Carry) : (P & ~StatusFlags.Carry);
                    break;
                }
            case 0x9C: // STZ Absolute
                {
                    // Leer la dirección de 16-bit.
                    ushort address = (ushort)(bus.Read(PC++) | (bus.Read(PC++) << 8));
                    // Escribir un cero en esa dirección.
                    bus.Write(address, 0);
                    break;
                }
            case 0xF1: // ADC (Direct Page,X) Indirect
                {
                    // Calcular la dirección indirecta en la página directa
                    byte dpOffset = bus.Read(PC++);
                    ushort indirectAddr = (ushort)((DP + dpOffset + X) & 0xFFFF);

                    // Leer la dirección final de 16-bit desde la dirección indirecta
                    ushort finalAddr = (ushort)(bus.Read(indirectAddr) | (bus.Read((ushort)(indirectAddr + 1)) << 8));

                    // Obtener el valor desde la dirección final
                    byte value = bus.Read(finalAddr);

                    // Realizar la suma (misma lógica que los otros ADC)
                    int carry = P.HasFlag(StatusFlags.Carry) ? 1 : 0;
                    int sum = A + value + carry;

                    P = (sum > 255) ? (P | StatusFlags.Carry) : (P & ~StatusFlags.Carry);
                    P = (((A ^ sum) & (value ^ sum) & 0x80) != 0) ? (P | StatusFlags.Overflow) : (P & ~StatusFlags.Overflow);

                    A = (byte)sum;
                    SetZeroAndNegativeFlags(A);
                    break;
                }
            case 0x70: // BVS - Branch on Overflow Set
                {
                    // Leer el desplazamiento relativo de 8-bit.
                    sbyte offset = (sbyte)bus.Read(PC++);

                    // Si el flag de Overflow (V) está activado, se toma el salto.
                    if (P.HasFlag(StatusFlags.Overflow))
                    {
                        PC = (ushort)(PC + offset);
                    }
                    break;
                }
            case 0x0D: // ORA Absolute
                {
                    // Leer la dirección de 16-bit.
                    ushort address = (ushort)(bus.Read(PC++) | (bus.Read(PC++) << 8));
                    byte value = bus.Read(address);

                    // Realizar la operación OR y guardar en el Acumulador.
                    A |= value;

                    // Actualizar los flags.
                    SetZeroAndNegativeFlags(A);
                    break;
                }
            case 0xFC: // JSR (Absolute,X) Indirect
                {
                    // Leer la dirección base de 16-bit.
                    ushort baseAddr = (ushort)(bus.Read(PC++) | (bus.Read(PC++) << 8));
                    // Sumarle el registro X para encontrar la dirección del puntero.
                    ushort indirectAddr = (ushort)(baseAddr + X);

                    // Leer la dirección final del puntero.
                    ushort finalAddr = (ushort)(bus.Read(indirectAddr) | (bus.Read((ushort)(indirectAddr + 1)) << 8));

                    // Guardar la dirección de retorno en la pila.
                    ushort returnAddr = (ushort)(PC - 1);
                    Push((byte)(returnAddr >> 8));
                    Push((byte)(returnAddr & 0xFF));

                    // Saltar a la subrutina.
                    PC = finalAddr;
                    break;
                }
            case 0xEA: // NOP - No Operation
                // // No hace nada, solo gasta ciclos.
                break;
            case 0xA7: // LDA (Direct Page) Indirect Long
                {
                    // Calcular la dirección indirecta en la página directa.
                    byte dpOffset = bus.Read(PC++);
                    ushort indirectAddr = (ushort)((DP + dpOffset) & 0xFFFF);

                    // Leer la dirección larga de 24-bit desde la dirección indirecta.
                    ushort addrLo = bus.Read(indirectAddr);
                    ushort addrHi = bus.Read((ushort)(indirectAddr + 1));
                    ushort addrBank = bus.Read((ushort)(indirectAddr + 2));

                    uint finalAddress = (uint)((addrBank << 16) | (addrHi << 8) | addrLo);

                    // NOTA: Nuestro Bus aún no maneja direcciones de 24-bit.
                    // Como simplificación temporal, ignoraremos el banco y solo usaremos
                    // los 16-bit inferiores para que el emulador pueda continuar.
                    A = bus.Read((ushort)finalAddress);
                    SetZeroAndNegativeFlags(A);
                    break;
                }
            case 0xDC: // JMP (Absolute) Indirect Long
                {
                    // Leer la dirección indirecta de 16-bit.
                    ushort indirectAddr = (ushort)(bus.Read(PC++) | (bus.Read(PC++) << 8));

                    // Leer la dirección final de 24-bit desde la ubicación indirecta.
                    ushort finalAddrLo = bus.Read(indirectAddr);
                    ushort finalAddrHi = bus.Read((ushort)(indirectAddr + 1));
                    byte finalAddrBank = bus.Read((ushort)(indirectAddr + 2));

                    // Actualizar el PC y el PBR para realizar el salto largo.
                    PC = (ushort)(finalAddrLo | (finalAddrHi << 8));
                    PBR = finalAddrBank;
                    break;
                }
                case 0x3B: // TSC - Transfer Stack to C
                C = SP; // Transferencia directa de 16-bit
                SetZeroAndNegativeFlags16(C); // Actualizar flags con el valor de 16-bit
                break;
                case 0xE1: // SBC (Direct Page,X) Indirect
                {
                    // Calcular la dirección del puntero
                    byte dpOffset = bus.Read(PC++);
                    ushort indirectAddr = (ushort)((DP + dpOffset + X) & 0xFFFF);

                    // Leer la dirección final de 16-bit
                    ushort finalAddr = (ushort)(bus.Read(indirectAddr) | (bus.Read((ushort)(indirectAddr + 1)) << 8));

                    // Obtener el valor desde la dirección final
                    byte value = bus.Read(finalAddr);

                    // Realizar la resta (A - valor - !Carry)
                    int carry = P.HasFlag(StatusFlags.Carry) ? 1 : 0;
                    int diff = A - value - (1 - carry);

                    // Actualizar los flags
                    // Carry se activa si no hubo préstamo (resultado >= 0)
                    P = (diff >= 0) ? (P | StatusFlags.Carry) : (P & ~StatusFlags.Carry);
                    // Overflow se activa si el signo del resultado es incorrecto
                    P = (((A ^ diff) & (~value ^ diff) & 0x80) != 0) ? (P | StatusFlags.Overflow) : (P & ~StatusFlags.Overflow);

                    A = (byte)diff;
                    SetZeroAndNegativeFlags(A);
                    break;
                }
                case 0xDE: // DEC Absolute,X
                {
                    // Leer la dirección base de 16-bit.
                    ushort baseAddr = (ushort)(bus.Read(PC++) | (bus.Read(PC++) << 8));
                    // Sumarle el registro X para obtener la dirección final.
                    ushort finalAddr = (ushort)(baseAddr + X);

                    // Leer el valor, restarle uno y escribirlo de vuelta.
                    byte value = bus.Read(finalAddr);
                    value--;
                    bus.Write(finalAddr, value);

                    // Actualizar los flags.
                    SetZeroAndNegativeFlags(value);
                    break;
                }
                case 0xAE: // LDX Absolute
                {
                    // Leer la dirección de 16-bit.
                    ushort address = (ushort)(bus.Read(PC++) | (bus.Read(PC++) << 8));
                    // Cargar el valor desde esa dirección en el registro X.
                    X = bus.Read(address);

                    // Actualizar los flags.
                    SetZeroAndNegativeFlags(X);
                    break;
                }
                case 0xD4: // PEI - (Direct Page) Indirect
                {
                    // Leer el offset de la página directa.
                    byte dpOffset = bus.Read(PC++);
                    ushort indirectAddr = (ushort)((DP + dpOffset) & 0xFFFF);

                    // Leer la dirección efectiva de 16-bit desde la ubicación indirecta.
                    ushort effectiveAddr = (ushort)(bus.Read(indirectAddr) | (bus.Read((ushort)(indirectAddr + 1)) << 8));

                    // Guardar la dirección en la pila (stack).
                    Push((byte)(effectiveAddr >> 8));   // Byte alto
                    Push((byte)(effectiveAddr & 0xFF)); // Byte bajo
                    break;
                }
                case 0xFD: // SBC Absolute,X
                {
                    // Leer la dirección base de 16-bit.
                    ushort baseAddr = (ushort)(bus.Read(PC++) | (bus.Read(PC++) << 8));
                    // Sumarle el registro X para obtener la dirección final.
                    ushort finalAddr = (ushort)(baseAddr + X);

                    byte value = bus.Read(finalAddr);

                    // Realizar la resta (A - valor - !Carry).
                    int carry = P.HasFlag(StatusFlags.Carry) ? 1 : 0;
                    int diff = A - value - (1 - carry);

                    // Actualizar los flags.
                    P = (diff >= 0) ? (P | StatusFlags.Carry) : (P & ~StatusFlags.Carry);
                    P = (((A ^ diff) & (~value ^ diff) & 0x80) != 0) ? (P | StatusFlags.Overflow) : (P & ~StatusFlags.Overflow);

                    A = (byte)diff;
                    SetZeroAndNegativeFlags(A);
                    break;
                }
                case 0xFF: // SBC Absolute Long,X
                {
                    // Leer la dirección base larga de 24-bit.
                    ushort addrLo = bus.Read(PC++);
                    ushort addrHi = bus.Read(PC++);
                    ushort addrBank = bus.Read(PC++);
                    uint baseAddr = (uint)((addrBank << 16) | (addrHi << 8) | addrLo);

                    // Sumarle el registro X para obtener la dirección final.
                    uint finalAddress = baseAddr + X;

                    // NOTA: Aún usamos nuestro Bus de 16-bit. Ignoramos el banco por ahora.
                    byte value = bus.Read((ushort)finalAddress);

                    // Realizar la resta (A - valor - !Carry).
                    int carry = P.HasFlag(StatusFlags.Carry) ? 1 : 0;
                    int diff = A - value - (1 - carry);

                    // Actualizar los flags.
                    P = (diff >= 0) ? (P | StatusFlags.Carry) : (P & ~StatusFlags.Carry);
                    P = (((A ^ diff) & (~value ^ diff) & 0x80) != 0) ? (P | StatusFlags.Overflow) : (P & ~StatusFlags.Overflow);

                    A = (byte)diff;
                    SetZeroAndNegativeFlags(A);
                    break;
                }
                case 0x69: // ADC Immediate
                {
                    // Leer el valor inmediato que sigue a la instrucción.
                    byte value = bus.Read(PC++);

                    // Realizar la suma (A + valor + Carry).
                    int carry = P.HasFlag(StatusFlags.Carry) ? 1 : 0;
                    int sum = A + value + carry;

                    // Actualizar los flags.
                    P = (sum > 255) ? (P | StatusFlags.Carry) : (P & ~StatusFlags.Carry);
                    P = (((A ^ sum) & (value ^ sum) & 0x80) != 0) ? (P | StatusFlags.Overflow) : (P & ~StatusFlags.Overflow);

                    // Guardar el resultado en el Acumulador.
                    A = (byte)sum;
                    SetZeroAndNegativeFlags(A);
                    break;
                }
                case 0xCE: // DEC Absolute
                {
                    // Leer la dirección de 16-bit.
                    ushort address = (ushort)(bus.Read(PC++) | (bus.Read(PC++) << 8));

                    // Leer el valor, restarle uno y escribirlo de vuelta.
                    byte value = bus.Read(address);
                    value--;
                    bus.Write(address, value);

                    // Actualizar los flags.
                    SetZeroAndNegativeFlags(value);
                    break;
                }
                case 0x9E: // STZ Absolute,X
                {
                    // Leer la dirección base de 16-bit.
                    ushort baseAddr = (ushort)(bus.Read(PC++) | (bus.Read(PC++) << 8));
                    // Sumarle el registro X para obtener la dirección final.
                    ushort finalAddr = (ushort)(baseAddr + X);

                    // Escribir un cero en esa dirección.
                    bus.Write(finalAddr, 0);
                    break;
                }
                case 0x01: // ORA (Direct Page,X) Indirect
                {
                    // Calcular la dirección indirecta en la página directa
                    byte dpOffset = bus.Read(PC++);
                    ushort indirectAddr = (ushort)((DP + dpOffset + X) & 0xFFFF);

                    // Leer la dirección final de 16-bit desde la dirección indirecta
                    ushort finalAddr = (ushort)(bus.Read(indirectAddr) | (bus.Read((ushort)(indirectAddr + 1)) << 8));

                    // Obtener el valor y realizar la operación OR
                    byte value = bus.Read(finalAddr);
                    A |= value;

                    // Actualizar los flags
                    SetZeroAndNegativeFlags(A);
                    break;
                }
                case 0x9B: // TXY - Transfer X to Y
                {
                    Y = X;
                    SetZeroAndNegativeFlags(Y);
                    break;
                }
                case 0x5E: // LSR Absolute,X
                {
                    // Calcular la dirección final.
                    ushort baseAddr = (ushort)(bus.Read(PC++) | (bus.Read(PC++) << 8));
                    ushort finalAddr = (ushort)(baseAddr + X);

                    byte value = bus.Read(finalAddr);

                    // El bit 0 original se convierte en el nuevo Carry.
                    P = (value & 1) == 1 ? (P | StatusFlags.Carry) : (P & ~StatusFlags.Carry);

                    // Desplazar el valor a la derecha.
                    value >>= 1;

                    bus.Write(finalAddr, value);

                    // Actualizar flags Z y N. El flag N siempre será 0.
                    P = value == 0 ? (P | StatusFlags.Zero) : (P & ~StatusFlags.Zero);
                    P &= ~StatusFlags.Negative; // El bit 7 siempre es 0, así que N siempre es 0.
                    break;
                }
                case 0x04: // TSB Direct Page
                {
                    // Calcular la dirección en la página directa.
                    byte dpOffset = bus.Read(PC++);
                    ushort address = (ushort)((DP + dpOffset) & 0xFFFF);

                    byte value = bus.Read(address);

                    // 1. "Test": Realizar un AND para actualizar el flag Zero.
                    P = (A & value) == 0 ? (P | StatusFlags.Zero) : (P & ~StatusFlags.Zero);

                    // 2. "Set": Realizar un OR y escribir el resultado en memoria.
                    value |= A;
                    bus.Write(address, value);
                    break;
                }
                case 0x02: // COP - Co-processor Interrupt
                {
                    // La instrucción COP tiene un operando de 1 byte que se ignora al ejecutar,
                    // pero se guarda en la pila la dirección del siguiente byte.
                    ushort returnAddr = (ushort)(PC + 1);
                    Push((byte)(returnAddr >> 8));
                    Push((byte)(returnAddr & 0xFF));
                    Push((byte)P);

                    // Saltar a la dirección del vector de interrupción COP.
                    ushort lowByte = bus.Read(0xFFF4);
                    ushort highByte = bus.Read(0xFFF5);
                    PC = (ushort)(lowByte | (highByte << 8));
                    break;
                }
                case 0x03: // ORA Stack Relative
                {
                    // Leer el offset de 8-bit desde la instrucción.
                    byte offset = bus.Read(PC++);
                    // Calcular la dirección final sumando el offset al Stack Pointer.
                    ushort finalAddr = (ushort)(SP + offset);

                    // Obtener el valor y realizar la operación OR.
                    byte value = bus.Read(finalAddr);
                    A |= value;

                    // Actualizar los flags.
                    SetZeroAndNegativeFlags(A);
                    break;
                }
                case 0x8F: // STA Absolute Long
                {
                    // Leer la dirección larga de 24-bit.
                    ushort addrLo = bus.Read(PC++);
                    ushort addrHi = bus.Read(PC++);
                    ushort addrBank = bus.Read(PC++);
                    uint finalAddress = (uint)((addrBank << 16) | (addrHi << 8) | addrLo);

                    // NOTA: Aún usamos nuestro Bus de 16-bit. Ignoramos el banco por ahora.
                    bus.Write((ushort)finalAddress, A);
                    break;
                }
                case 0x80: // BRA - Branch Always
                {
                    // Leer el desplazamiento relativo de 8-bit.
                    sbyte offset = (sbyte)bus.Read(PC++);
                    // Sumar el offset al PC para realizar el salto.
                    PC = (ushort)(PC + offset);
                    break;
                }
                case 0x8E: // STX Absolute
                {
                    // Leer la dirección de 16-bit.
                    ushort address = (ushort)(bus.Read(PC++) | (bus.Read(PC++) << 8));
                    // Escribir el valor del registro X en esa dirección.
                    bus.Write(address, X);
                    break;
                }
                case 0x81: // STA (Direct Page,X) Indirect
                {
                    // Calcular la dirección indirecta en la página directa.
                    byte dpOffset = bus.Read(PC++);
                    ushort indirectAddr = (ushort)((DP + dpOffset + X) & 0xFFFF);

                    // Leer la dirección final de 16-bit desde la dirección indirecta.
                    ushort finalAddr = (ushort)(bus.Read(indirectAddr) | (bus.Read((ushort)(indirectAddr + 1)) << 8));

                    // Escribir el valor del acumulador en la dirección final.
                    bus.Write(finalAddr, A);
                    break;
                }
                case 0x57: // EOR (Direct Page) Indirect Long
                {
                    // Calcular la dirección indirecta en la página directa.
                    byte dpOffset = bus.Read(PC++);
                    ushort indirectAddr = (ushort)((DP + dpOffset) & 0xFFFF);

                    // Leer la dirección larga de 24-bit desde la dirección indirecta.
                    ushort addrLo = bus.Read(indirectAddr);
                    ushort addrHi = bus.Read((ushort)(indirectAddr + 1));
                    ushort addrBank = bus.Read((ushort)(indirectAddr + 2));
                    uint finalAddress = (uint)((addrBank << 16) | (addrHi << 8) | addrLo);

                    // NOTA: Aún usamos nuestro Bus de 16-bit. Ignoramos el banco por ahora.
                    byte value = bus.Read((ushort)finalAddress);

                    // Realizar la operación XOR.
                    A ^= value;

                    // Actualizar los flags.
                    SetZeroAndNegativeFlags(A);
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