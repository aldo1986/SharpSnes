public class CPU
{
    // Registros
    private ushort _c;
    public byte A { get => (byte)(_c & 0xFF); set => _c = (ushort)((_c & 0xFF00) | value); }
    public byte B { get => (byte)(_c >> 8); set => _c = (ushort)((value << 8) | (_c & 0xFF)); }
    public ushort C { get => _c; set => _c = value; }
    public ushort SP { get; set; }
    public ushort DP { get; set; }
    public byte DBR { get; set; }
    public byte PBR { get; set; }
    public byte X { get; set; }
    public byte Y { get; set; }
    public StatusFlags P { get; set; }
    public ushort PC { get; set; }
    private bool EmulationMode = true;
    private bool nmiPending = false;
    private readonly Bus bus;

    public CPU(Bus bus) { this.bus = bus; }
    public void RequestNMI() { nmiPending = true; }

    private uint GetAddress(byte bank, ushort offset) => (uint)((bank << 16) | offset);
    private void Push(byte data) { bus.Write(SP--, data); }
    private byte Pop() { return bus.Read(++SP); }
    private void SetZeroAndNegativeFlags(byte value) { /* ... sin cambios ... */ }
    private void SetZeroAndNegativeFlags16(ushort value) { /* ... sin cambios ... */ }

    public void Reset()
    {
        ushort lowByte = bus.Read(0x00FFFC);
        ushort highByte = bus.Read(0x00FFFD);
        PC = (ushort)(lowByte | (highByte << 8));

        PBR = 0; DBR = 0; DP = 0;
        SP = 0x01FF;
        A = 0; X = 0; Y = 0; B = 0;
        EmulationMode = true;
        P = StatusFlags.InterruptDisable;
    }

    private void HandleNMI() { /* ... sin cambios ... */ }

    public void Step()
    {
        if (nmiPending) HandleNMI();

        uint currentPCAddr = GetAddress(PBR, PC);
        byte opcode = bus.Read(currentPCAddr);
        PC++;

        switch (opcode)
        {
            // Opcodes implementados...
            #region Opcodes
            case 0x00: break; // BRK
            case 0x01: { byte o = bus.Read(GetAddress(PBR, PC++)); ushort i = (ushort)((DP + o + X) & 0xFFFF); ushort f = (ushort)(bus.Read(GetAddress(DBR, i)) | (bus.Read(GetAddress(DBR, (ushort)(i + 1))) << 8)); byte v = bus.Read(GetAddress(DBR, f)); A |= v; SetZeroAndNegativeFlags(A); break; } // ORA (DP,X),I
            case 0x02: { ushort r = (ushort)(PC + 1); Push((byte)(r >> 8)); Push((byte)(r & 0xFF)); Push((byte)P); ushort l = bus.Read(0x00FFF4); ushort h = bus.Read(0x00FFF5); PC = (ushort)(l | (h << 8)); break; } // COP
            case 0x03: { byte o = bus.Read(GetAddress(PBR, PC++)); ushort a = (ushort)(SP + o); byte v = bus.Read(GetAddress(DBR, a)); A |= v; SetZeroAndNegativeFlags(A); break; } // ORA S
            case 0x04: { byte o = bus.Read(GetAddress(PBR, PC++)); ushort a = (ushort)((DP + o) & 0xFFFF); byte v = bus.Read(GetAddress(DBR, a)); P = (A & v) == 0 ? (P | StatusFlags.Zero) : (P & ~StatusFlags.Zero); v |= A; bus.Write(GetAddress(DBR, a), v); break; } // TSB DP
            case 0x05: { byte o = bus.Read(GetAddress(PBR, PC++)); ushort a = (ushort)((DP + o) & 0xFFFF); byte v = bus.Read(GetAddress(DBR, a)); A |= v; SetZeroAndNegativeFlags(A); break; } // ORA DP
            case 0x08: Push((byte)P); break; // PHP
            case 0x09: { byte v = bus.Read(GetAddress(PBR, PC++)); A |= v; SetZeroAndNegativeFlags(A); break; } // ORA #
            case 0x0A: { P = (A & 0x80) != 0 ? (P | StatusFlags.Carry) : (P & ~StatusFlags.Carry); A <<= 1; SetZeroAndNegativeFlags(A); break; } // ASL A
            case 0x0D: { ushort a = (ushort)(bus.Read(GetAddress(PBR, PC++)) | (bus.Read(GetAddress(PBR, PC++)) << 8)); byte v = bus.Read(GetAddress(DBR, a)); A |= v; SetZeroAndNegativeFlags(A); break; } // ORA A
            case 0x0E: { ushort a = (ushort)(bus.Read(GetAddress(PBR, PC++)) | (bus.Read(GetAddress(PBR, PC++)) << 8)); byte v = bus.Read(GetAddress(DBR, a)); P = (v & 0x80) != 0 ? (P | StatusFlags.Carry) : (P & ~StatusFlags.Carry); v <<= 1; bus.Write(GetAddress(DBR, a), v); SetZeroAndNegativeFlags(v); break; } // ASL A
            case 0x10: { sbyte o = (sbyte)bus.Read(GetAddress(PBR, PC++)); if (!P.HasFlag(StatusFlags.Negative)) PC = (ushort)(PC + o); break; } // BPL
            case 0x13: { byte o = bus.Read(GetAddress(PBR, PC++)); ushort i = (ushort)(SP + o); ushort b = (ushort)(bus.Read(GetAddress(DBR, i)) | (bus.Read(GetAddress(DBR, (ushort)(i + 1))) << 8)); ushort f = (ushort)(b + Y); byte v = bus.Read(GetAddress(DBR, f)); A |= v; SetZeroAndNegativeFlags(A); break; } // ORA (S,Y)
            case 0x14: { byte o = bus.Read(GetAddress(PBR, PC++)); ushort a = (ushort)((DP + o) & 0xFFFF); byte v = bus.Read(GetAddress(DBR, a)); P = (A & v) == 0 ? (P | StatusFlags.Zero) : (P & ~StatusFlags.Zero); v &= (byte)~A; bus.Write(GetAddress(DBR, a), v); break; } // TRB DP
            case 0x18: P &= ~StatusFlags.Carry; break; // CLC
            case 0x19: { ushort b = (ushort)(bus.Read(GetAddress(PBR, PC++)) | (bus.Read(GetAddress(PBR, PC++)) << 8)); ushort f = (ushort)(b + Y); byte v = bus.Read(GetAddress(DBR, f)); A |= v; SetZeroAndNegativeFlags(A); break; } // ORA A,Y
            case 0x1A: A++; SetZeroAndNegativeFlags(A); break; // INC A
            case 0x1B: SP = C; break; // TCS
            case 0x1F: { ushort l = bus.Read(GetAddress(PBR, PC++)); ushort h = bus.Read(GetAddress(PBR, PC++)); byte b = bus.Read(GetAddress(PBR, PC++)); uint ba = (uint)((b << 16) | (h << 8) | l); uint f = ba + X; byte v = bus.Read(f); A |= v; SetZeroAndNegativeFlags(A); break; } // ORA AL,X
            case 0x20: { ushort s = (ushort)(bus.Read(GetAddress(PBR, PC++)) | (bus.Read(GetAddress(PBR, PC++)) << 8)); ushort r = (ushort)(PC - 1); Push((byte)(r >> 8)); Push((byte)(r & 0xFF)); PC = s; break; } // JSR
            case 0x21: { byte o = bus.Read(GetAddress(PBR, PC++)); ushort i = (ushort)((DP + o + X) & 0xFFFF); ushort f = (ushort)(bus.Read(GetAddress(DBR, i)) | (bus.Read(GetAddress(DBR, (ushort)(i + 1))) << 8)); byte v = bus.Read(GetAddress(DBR, f)); A &= v; SetZeroAndNegativeFlags(A); break; } // AND (DP,X),I
            case 0x29: { byte v = bus.Read(GetAddress(PBR, PC++)); A &= v; SetZeroAndNegativeFlags(A); break; } // AND #
            case 0x2C: { ushort a = (ushort)(bus.Read(GetAddress(PBR, PC++)) | (bus.Read(GetAddress(PBR, PC++)) << 8)); byte v = bus.Read(GetAddress(DBR, a)); P = (A & v) == 0 ? (P | StatusFlags.Zero) : (P & ~StatusFlags.Zero); P = (v & 0x80) != 0 ? (P | StatusFlags.Negative) : (P & ~StatusFlags.Negative); P = (v & 0x40) != 0 ? (P | StatusFlags.Overflow) : (P & ~StatusFlags.Overflow); break; } // BIT A
            case 0x2D: { ushort a = (ushort)(bus.Read(GetAddress(PBR, PC++)) | (bus.Read(GetAddress(PBR, PC++)) << 8)); byte v = bus.Read(GetAddress(DBR, a)); A &= v; SetZeroAndNegativeFlags(A); break; } // AND A
            case 0x2F: { ushort l = bus.Read(GetAddress(PBR, PC++)); ushort h = bus.Read(GetAddress(PBR, PC++)); byte b = bus.Read(GetAddress(PBR, PC++)); uint f = (uint)((b << 16) | (h << 8) | l); byte v = bus.Read(f); A &= v; SetZeroAndNegativeFlags(A); break; } // AND AL
            case 0x30: { sbyte o = (sbyte)bus.Read(GetAddress(PBR, PC++)); if (P.HasFlag(StatusFlags.Negative)) PC = (ushort)(PC + o); break; } // BMI
            case 0x33: { byte o = bus.Read(GetAddress(PBR, PC++)); ushort a = (ushort)(SP + o); byte v = bus.Read(GetAddress(DBR, a)); A ^= v; SetZeroAndNegativeFlags(A); break; } // EOR S
            case 0x38: P |= StatusFlags.Carry; break; // SEC
            case 0x3A: A--; SetZeroAndNegativeFlags(A); break; // DEC A
            case 0x3B: C = SP; SetZeroAndNegativeFlags16(C); break; // TSC
            case 0x3E: { ushort b = (ushort)(bus.Read(GetAddress(PBR, PC++)) | (bus.Read(GetAddress(PBR, PC++)) << 8)); ushort f = (ushort)(b + X); byte v = bus.Read(GetAddress(DBR, f)); bool oc = P.HasFlag(StatusFlags.Carry); P = (v & 0x80) != 0 ? (P | StatusFlags.Carry) : (P & ~StatusFlags.Carry); v <<= 1; if (oc) v |= 1; bus.Write(GetAddress(DBR, f), v); SetZeroAndNegativeFlags(v); break; } // ROL A,X
            case 0x3F: { ushort l = bus.Read(GetAddress(PBR, PC++)); ushort h = bus.Read(GetAddress(PBR, PC++)); byte b = bus.Read(GetAddress(PBR, PC++)); uint ba = (uint)((b << 16) | (h << 8) | l); uint f = ba + X; byte v = bus.Read(f); A &= v; SetZeroAndNegativeFlags(A); break; } // AND AL,X
            case 0x40: { P = (StatusFlags)Pop(); byte l = Pop(); byte h = Pop(); PC = (ushort)(l | (h << 8)); break; } // RTI
            case 0x42: PC++; break; // WDM
            case 0x48: Push(A); break; // PHA
            case 0x4B: Push(PBR); break; // PHK
            case 0x4C: PC = (ushort)(bus.Read(GetAddress(PBR, PC)) | (bus.Read(GetAddress(PBR, (ushort)(PC + 1))) << 8)); break; // JMP A
            case 0x4D: { ushort a = (ushort)(bus.Read(GetAddress(PBR, PC++)) | (bus.Read(GetAddress(PBR, PC++)) << 8)); byte v = bus.Read(GetAddress(DBR, a)); A ^= v; SetZeroAndNegativeFlags(A); break; } // EOR A
            case 0x4E: { ushort a = (ushort)(bus.Read(GetAddress(PBR, PC++)) | (bus.Read(GetAddress(PBR, PC++)) << 8)); byte v = bus.Read(GetAddress(DBR, a)); P = (v & 1) == 1 ? (P | StatusFlags.Carry) : (P & ~StatusFlags.Carry); v >>= 1; bus.Write(GetAddress(DBR, a), v); P = v == 0 ? (P | StatusFlags.Zero) : (P & ~StatusFlags.Zero); P &= ~StatusFlags.Negative; break; } // LSR A
            case 0x51: { byte o = bus.Read(GetAddress(PBR, PC++)); ushort i = (ushort)((DP + o) & 0xFFFF); ushort b = (ushort)(bus.Read(GetAddress(DBR, i)) | (bus.Read(GetAddress(DBR, (ushort)(i + 1))) << 8)); ushort f = (ushort)(b + Y); byte v = bus.Read(GetAddress(DBR, f)); A ^= v; SetZeroAndNegativeFlags(A); break; } // EOR (DP),Y
            case 0x57: { byte o = bus.Read(GetAddress(PBR, PC++)); ushort i = (ushort)((DP + o) & 0xFFFF); ushort l = bus.Read(GetAddress(DBR, i)); ushort h = bus.Read(GetAddress(DBR, (ushort)(i + 1))); ushort b = bus.Read(GetAddress(DBR, (ushort)(i + 2))); uint f = (uint)((b << 16) | (h << 8) | l); byte v = bus.Read(f); A ^= v; SetZeroAndNegativeFlags(A); break; } // EOR [DP],Y
            case 0x5A: Push(Y); break; // PHY
            case 0x5C: { ushort l = bus.Read(GetAddress(PBR, PC++)); ushort h = bus.Read(GetAddress(PBR, PC++)); byte b = bus.Read(GetAddress(PBR, PC++)); PC = (ushort)(l | (h << 8)); PBR = b; break; } // JML AL
            case 0x5E: { ushort b = (ushort)(bus.Read(GetAddress(PBR, PC++)) | (bus.Read(GetAddress(PBR, PC++)) << 8)); ushort f = (ushort)(b + X); byte v = bus.Read(GetAddress(DBR, f)); P = (v & 1) == 1 ? (P | StatusFlags.Carry) : (P & ~StatusFlags.Carry); v >>= 1; bus.Write(GetAddress(DBR, f), v); P = v == 0 ? (P | StatusFlags.Zero) : (P & ~StatusFlags.Zero); P &= ~StatusFlags.Negative; break; } // LSR A,X
            case 0x5F: { ushort l = bus.Read(GetAddress(PBR, PC++)); ushort h = bus.Read(GetAddress(PBR, PC++)); byte b = bus.Read(GetAddress(PBR, PC++)); uint ba = (uint)((b << 16) | (h << 8) | l); uint f = ba + X; byte v = bus.Read(f); A ^= v; SetZeroAndNegativeFlags(A); break; } // EOR AL,X
            case 0x60: { byte l = Pop(); byte h = Pop(); ushort r = (ushort)(l | (h << 8)); PC = (ushort)(r + 1); break; } // RTS
            case 0x64: { byte o = bus.Read(GetAddress(PBR, PC++)); ushort a = (ushort)((DP + o) & 0xFFFF); bus.Write(GetAddress(DBR, a), 0); break; } // STZ DP
            case 0x68: A = Pop(); SetZeroAndNegativeFlags(A); break; // PLA
            case 0x69: { byte v = bus.Read(GetAddress(PBR, PC++)); int c = P.HasFlag(StatusFlags.Carry) ? 1 : 0; int s = A + v + c; P = (s > 255) ? (P | StatusFlags.Carry) : (P & ~StatusFlags.Carry); P = (((A ^ s) & (v ^ s) & 0x80) != 0) ? (P | StatusFlags.Overflow) : (P & ~StatusFlags.Overflow); A = (byte)s; SetZeroAndNegativeFlags(A); break; } // ADC #
            case 0x6A: { bool oc = P.HasFlag(StatusFlags.Carry); P = (A & 0x80) != 0 ? (P | StatusFlags.Carry) : (P & ~StatusFlags.Carry); A <<= 1; if (oc) A |= 1; SetZeroAndNegativeFlags(A); break; } // ROL A
            case 0x6D: { ushort a = (ushort)(bus.Read(GetAddress(PBR, PC++)) | (bus.Read(GetAddress(PBR, PC++)) << 8)); byte v = bus.Read(GetAddress(DBR, a)); int c = P.HasFlag(StatusFlags.Carry) ? 1 : 0; int s = A + v + c; P = (s > 255) ? (P | StatusFlags.Carry) : (P & ~StatusFlags.Carry); P = (((A ^ s) & (v ^ s) & 0x80) != 0) ? (P | StatusFlags.Overflow) : (P & ~StatusFlags.Overflow); A = (byte)s; SetZeroAndNegativeFlags(A); break; } // ADC A
            case 0x70: { sbyte o = (sbyte)bus.Read(GetAddress(PBR, PC++)); if (P.HasFlag(StatusFlags.Overflow)) PC = (ushort)(PC + o); break; } // BVS
            case 0x77: { byte o = bus.Read(GetAddress(PBR, PC++)); ushort i = (ushort)((DP + o) & 0xFFFF); ushort l = bus.Read(GetAddress(DBR, i)); ushort h = bus.Read(GetAddress(DBR, (ushort)(i + 1))); ushort b = bus.Read(GetAddress(DBR, (ushort)(i + 2))); uint f = (uint)((b << 16) | (h << 8) | l); byte v = bus.Read(f); int c = P.HasFlag(StatusFlags.Carry) ? 1 : 0; int s = A + v + c; P = (s > 255) ? (P | StatusFlags.Carry) : (P & ~StatusFlags.Carry); P = (((A ^ s) & (v ^ s) & 0x80) != 0) ? (P | StatusFlags.Overflow) : (P & ~StatusFlags.Overflow); A = (byte)s; SetZeroAndNegativeFlags(A); break; } // ADC [DP],Y
            case 0x7F: { ushort l = bus.Read(GetAddress(PBR, PC++)); ushort h = bus.Read(GetAddress(PBR, PC++)); byte b = bus.Read(GetAddress(PBR, PC++)); uint ba = (uint)((b << 16) | (h << 8) | l); uint f = ba + X; byte v = bus.Read(f); int c = P.HasFlag(StatusFlags.Carry) ? 1 : 0; int s = A + v + c; P = (s > 255) ? (P | StatusFlags.Carry) : (P & ~StatusFlags.Carry); P = (((A ^ s) & (v ^ s) & 0x80) != 0) ? (P | StatusFlags.Overflow) : (P & ~StatusFlags.Overflow); A = (byte)s; SetZeroAndNegativeFlags(A); break; } // ADC AL,X
            case 0x80: { sbyte o = (sbyte)bus.Read(GetAddress(PBR, PC++)); PC = (ushort)(PC + o); break; } // BRA
            case 0x81: { byte o = bus.Read(GetAddress(PBR, PC++)); ushort i = (ushort)((DP + o + X) & 0xFFFF); ushort f = (ushort)(bus.Read(GetAddress(DBR, i)) | (bus.Read(GetAddress(DBR, (ushort)(i + 1))) << 8)); bus.Write(GetAddress(DBR, f), A); break; } // STA (DP,X),I
            case 0x82: { short o = (short)(bus.Read(GetAddress(PBR, PC++)) | (bus.Read(GetAddress(PBR, PC++)) << 8)); PC = (ushort)(PC + o); break; } // BRL
            case 0x86: { byte o = bus.Read(GetAddress(PBR, PC++)); ushort a = (ushort)((DP + o) & 0xFFFF); bus.Write(GetAddress(DBR, a), X); break; } // STX DP
            case 0x8B: Push(DBR); break; // PHB
            case 0x8C: { ushort a = (ushort)(bus.Read(GetAddress(PBR, PC++)) | (bus.Read(GetAddress(PBR, PC++)) << 8)); bus.Write(GetAddress(DBR, a), Y); break; } // STY A
            case 0x8D: { ushort a = (ushort)(bus.Read(GetAddress(PBR, PC++)) | (bus.Read(GetAddress(PBR, PC++)) << 8)); bus.Write(GetAddress(DBR, a), A); break; } // STA A
            case 0x8E: { ushort a = (ushort)(bus.Read(GetAddress(PBR, PC++)) | (bus.Read(GetAddress(PBR, PC++)) << 8)); bus.Write(GetAddress(DBR, a), X); break; } // STX A
            case 0x8F: { ushort l = bus.Read(GetAddress(PBR, PC++)); ushort h = bus.Read(GetAddress(PBR, PC++)); byte b = bus.Read(GetAddress(PBR, PC++)); uint f = (uint)((b << 16) | (h << 8) | l); bus.Write(f, A); break; } // STA AL
            case 0x90: { sbyte o = (sbyte)bus.Read(GetAddress(PBR, PC++)); if (!P.HasFlag(StatusFlags.Carry)) PC = (ushort)(PC + o); break; } // BCC
            case 0x97: { byte o = bus.Read(GetAddress(PBR, PC++)); ushort i = (ushort)((DP + o) & 0xFFFF); ushort l = bus.Read(GetAddress(DBR, i)); ushort h = bus.Read(GetAddress(DBR, (ushort)(i + 1))); ushort b = bus.Read(GetAddress(DBR, (ushort)(i + 2))); uint ba = (uint)((b << 16) | (h << 8) | l); uint f = ba + Y; bus.Write(f, A); break; } // STA [DP],Y
            case 0x99: { ushort b = (ushort)(bus.Read(GetAddress(PBR, PC++)) | (bus.Read(GetAddress(PBR, PC++)) << 8)); ushort f = (ushort)(b + Y); bus.Write(GetAddress(DBR, f), A); break; } // STA A,Y
            case 0x9B: Y = X; SetZeroAndNegativeFlags(Y); break; // TXY
            case 0x9C: { ushort a = (ushort)(bus.Read(GetAddress(PBR, PC++)) | (bus.Read(GetAddress(PBR, PC++)) << 8)); bus.Write(GetAddress(DBR, a), 0); break; } // STZ A
            case 0x9D: { ushort b = (ushort)(bus.Read(GetAddress(PBR, PC++)) | (bus.Read(GetAddress(PBR, PC++)) << 8)); ushort f = (ushort)(b + X); bus.Write(GetAddress(DBR, f), A); break; } // STA A,X
            case 0x9E: { ushort b = (ushort)(bus.Read(GetAddress(PBR, PC++)) | (bus.Read(GetAddress(PBR, PC++)) << 8)); ushort f = (ushort)(b + X); bus.Write(GetAddress(DBR, f), 0); break; } // STZ A,X
            case 0xA0: Y = bus.Read(GetAddress(PBR, PC++)); SetZeroAndNegativeFlags(Y); break; // LDY #
            case 0xA5: { byte o = bus.Read(GetAddress(PBR, PC++)); ushort a = (ushort)((DP + o) & 0xFFFF); A = bus.Read(GetAddress(DBR, a)); SetZeroAndNegativeFlags(A); break; } // LDA DP
            case 0xA7: { byte o = bus.Read(GetAddress(PBR, PC++)); ushort i = (ushort)((DP + o) & 0xFFFF); ushort l = bus.Read(GetAddress(DBR, i)); ushort h = bus.Read(GetAddress(DBR, (ushort)(i + 1))); ushort b = bus.Read(GetAddress(DBR, (ushort)(i + 2))); uint f = (uint)((b << 16) | (h << 8) | l); A = bus.Read(f); SetZeroAndNegativeFlags(A); break; } // LDA [DP]
            case 0xAA: X = A; SetZeroAndNegativeFlags(X); break; // TAX
            case 0xAB: DBR = Pop(); SetZeroAndNegativeFlags(DBR); break; // PLB
            case 0xAC: { ushort a = (ushort)(bus.Read(GetAddress(PBR, PC++)) | (bus.Read(GetAddress(PBR, PC++)) << 8)); Y = bus.Read(GetAddress(DBR, a)); SetZeroAndNegativeFlags(Y); break; } // LDY A
            case 0xAE: { ushort a = (ushort)(bus.Read(GetAddress(PBR, PC++)) | (bus.Read(GetAddress(PBR, PC++)) << 8)); X = bus.Read(GetAddress(DBR, a)); SetZeroAndNegativeFlags(X); break; } // LDX A
            case 0xB2: { byte o = bus.Read(GetAddress(PBR, PC++)); ushort i = (ushort)((DP + o) & 0xFFFF); ushort f = (ushort)(bus.Read(GetAddress(DBR, i)) | (bus.Read(GetAddress(DBR, (ushort)(i + 1))) << 8)); A = bus.Read(GetAddress(DBR, f)); SetZeroAndNegativeFlags(A); break; } // LDA (DP)
            case 0xB7: { byte o = bus.Read(GetAddress(PBR, PC++)); ushort i = (ushort)((DP + o) & 0xFFFF); ushort l = bus.Read(GetAddress(DBR, i)); ushort h = bus.Read(GetAddress(DBR, (ushort)(i + 1))); ushort b = bus.Read(GetAddress(DBR, (ushort)(i + 2))); uint ba = (uint)((b << 16) | (h << 8) | l); uint f = ba + Y; A = bus.Read(f); SetZeroAndNegativeFlags(A); break; } // LDA [DP],Y
            case 0xBD: { ushort b = (ushort)(bus.Read(GetAddress(PBR, PC++)) | (bus.Read(GetAddress(PBR, PC++)) << 8)); ushort f = (ushort)(b + X); A = bus.Read(GetAddress(DBR, f)); SetZeroAndNegativeFlags(A); break; } // LDA A,X
            case 0xBE: { ushort b = (ushort)(bus.Read(GetAddress(PBR, PC++)) | (bus.Read(GetAddress(PBR, PC++)) << 8)); ushort f = (ushort)(b + Y); X = bus.Read(GetAddress(DBR, f)); SetZeroAndNegativeFlags(X); break; } // LDX A,Y
            case 0xBF: { ushort l = bus.Read(GetAddress(PBR, PC++)); ushort h = bus.Read(GetAddress(PBR, PC++)); byte b = bus.Read(GetAddress(PBR, PC++)); uint ba = (uint)((b << 16) | (h << 8) | l); uint f = ba + X; A = bus.Read(f); SetZeroAndNegativeFlags(A); break; } // LDA AL,X
            case 0xC0: { byte v = bus.Read(GetAddress(PBR, PC++)); byte r = (byte)(Y - v); SetZeroAndNegativeFlags(r); P = (Y >= v) ? (P | StatusFlags.Carry) : (P & ~StatusFlags.Carry); break; } // CPY #
            case 0xC2: P &= ~(StatusFlags)bus.Read(GetAddress(PBR, PC++)); break; // REP
            case 0xC8: Y++; SetZeroAndNegativeFlags(Y); break; // INY
            case 0xC9: { byte v = bus.Read(GetAddress(PBR, PC++)); byte r = (byte)(A - v); SetZeroAndNegativeFlags(r); P = (A >= v) ? (P | StatusFlags.Carry) : (P & ~StatusFlags.Carry); break; } // CMP #
            case 0xCD: { ushort a = (ushort)(bus.Read(GetAddress(PBR, PC++)) | (bus.Read(GetAddress(PBR, PC++)) << 8)); byte v = bus.Read(GetAddress(DBR, a)); byte r = (byte)(A - v); SetZeroAndNegativeFlags(r); P = (A >= v) ? (P | StatusFlags.Carry) : (P & ~StatusFlags.Carry); break; } // CMP A
            case 0xCE: { ushort a = (ushort)(bus.Read(GetAddress(PBR, PC++)) | (bus.Read(GetAddress(PBR, PC++)) << 8)); byte v = bus.Read(GetAddress(DBR, a)); v--; bus.Write(GetAddress(DBR, a), v); SetZeroAndNegativeFlags(v); break; } // DEC A
            case 0xD3: { byte o = bus.Read(GetAddress(PBR, PC++)); ushort i = (ushort)(SP + o); ushort b = (ushort)(bus.Read(GetAddress(DBR, i)) | (bus.Read(GetAddress(DBR, (ushort)(i + 1))) << 8)); ushort f = (ushort)(b + Y); byte v = bus.Read(GetAddress(DBR, f)); A |= v; SetZeroAndNegativeFlags(A); break; } // ORA (S,Y)
            case 0xD4: { byte o = bus.Read(GetAddress(PBR, PC++)); ushort i = (ushort)((DP + o) & 0xFFFF); ushort e = (ushort)(bus.Read(GetAddress(DBR, i)) | (bus.Read(GetAddress(DBR, (ushort)(i + 1))) << 8)); Push((byte)(e >> 8)); Push((byte)(e & 0xFF)); break; } // PEI
            case 0xD5: { byte o = bus.Read(GetAddress(PBR, PC++)); ushort a = (ushort)((DP + o + X) & 0xFFFF); byte v = bus.Read(GetAddress(DBR, a)); byte r = (byte)(A - v); SetZeroAndNegativeFlags(r); P = (A >= v) ? (P | StatusFlags.Carry) : (P & ~StatusFlags.Carry); break; } // CMP DP,X
            case 0xD6: { byte o = bus.Read(GetAddress(PBR, PC++)); ushort f = (ushort)((DP + o + X) & 0xFFFF); byte v = bus.Read(GetAddress(DBR, f)); v--; bus.Write(GetAddress(DBR, f), v); SetZeroAndNegativeFlags(v); break; } // DEC DP,X
            case 0xDA: Push(X); break; // PHX
            case 0xDC: { ushort i = (ushort)(bus.Read(GetAddress(PBR, PC++)) | (bus.Read(GetAddress(PBR, PC++)) << 8)); ushort l = bus.Read(GetAddress(DBR, i)); ushort h = bus.Read(GetAddress(DBR, (ushort)(i + 1))); byte b = bus.Read(GetAddress(DBR, (ushort)(i + 2))); PC = (ushort)(l | (h << 8)); PBR = b; break; } // JMP [AL]
            case 0xDE: { ushort b = (ushort)(bus.Read(GetAddress(PBR, PC++)) | (bus.Read(GetAddress(PBR, PC++)) << 8)); ushort f = (ushort)(b + X); byte v = bus.Read(GetAddress(DBR, f)); v--; bus.Write(GetAddress(DBR, f), v); SetZeroAndNegativeFlags(v); break; } // DEC A,X
            case 0xDF: { ushort l = bus.Read(GetAddress(PBR, PC++)); ushort h = bus.Read(GetAddress(PBR, PC++)); byte b = bus.Read(GetAddress(PBR, PC++)); uint ba = (uint)((b << 16) | (h << 8) | l); uint f = ba + X; byte v = bus.Read(f); byte r = (byte)(A - v); SetZeroAndNegativeFlags(r); P = (A >= v) ? (P | StatusFlags.Carry) : (P & ~StatusFlags.Carry); break; } // CMP AL,X
            case 0xE1: { byte o = bus.Read(GetAddress(PBR, PC++)); ushort i = (ushort)((DP + o + X) & 0xFFFF); ushort f = (ushort)(bus.Read(GetAddress(DBR, i)) | (bus.Read(GetAddress(DBR, (ushort)(i + 1))) << 8)); byte v = bus.Read(GetAddress(DBR, f)); int c = P.HasFlag(StatusFlags.Carry) ? 1 : 0; int d = A - v - (1 - c); P = (d >= 0) ? (P | StatusFlags.Carry) : (P & ~StatusFlags.Carry); P = (((A ^ d) & (~v ^ d) & 0x80) != 0) ? (P | StatusFlags.Overflow) : (P & ~StatusFlags.Overflow); A = (byte)d; SetZeroAndNegativeFlags(A); break; } // SBC (DP,X),I
            case 0xE2: P |= (StatusFlags)bus.Read(GetAddress(PBR, PC++)); break; // SEP
            case 0xE4: { byte a = bus.Read(GetAddress(PBR, PC++)); byte v = bus.Read(GetAddress(DBR, a)); byte r = (byte)(X - v); SetZeroAndNegativeFlags(r); P = (X >= v) ? (P | StatusFlags.Carry) : (P & ~StatusFlags.Carry); break; } // CPX DP
            case 0xEA: break; // NOP
            case 0xED: { ushort a = (ushort)(bus.Read(GetAddress(PBR, PC++)) | (bus.Read(GetAddress(PBR, PC++)) << 8)); byte v = bus.Read(GetAddress(DBR, a)); int c = P.HasFlag(StatusFlags.Carry) ? 1 : 0; int d = A - v - (1 - c); P = (d >= 0) ? (P | StatusFlags.Carry) : (P & ~StatusFlags.Carry); P = (((A ^ d) & (~v ^ d) & 0x80) != 0) ? (P | StatusFlags.Overflow) : (P & ~StatusFlags.Overflow); A = (byte)d; SetZeroAndNegativeFlags(A); break; } // SBC A
            case 0xEF: { ushort l = bus.Read(GetAddress(PBR, PC++)); ushort h = bus.Read(GetAddress(PBR, PC++)); byte b = bus.Read(GetAddress(PBR, PC++)); uint f = (uint)((b << 16) | (h << 8) | l); byte v = bus.Read(f); int c = P.HasFlag(StatusFlags.Carry) ? 1 : 0; int d = A - v - (1 - c); P = (d >= 0) ? (P | StatusFlags.Carry) : (P & ~StatusFlags.Carry); P = (((A ^ d) & (~v ^ d) & 0x80) != 0) ? (P | StatusFlags.Overflow) : (P & ~StatusFlags.Overflow); A = (byte)d; SetZeroAndNegativeFlags(A); break; } // SBC AL
            case 0xF0: { sbyte o = (sbyte)bus.Read(GetAddress(PBR, PC++)); if (P.HasFlag(StatusFlags.Zero)) PC = (ushort)(PC + o); break; } // BEQ
            case 0xF1: { byte o = bus.Read(GetAddress(PBR, PC++)); ushort i = (ushort)((DP + o) & 0xFFFF); ushort f = (ushort)(bus.Read(GetAddress(DBR, i)) | (bus.Read(GetAddress(DBR, (ushort)(i + 1))) << 8)); byte v = bus.Read(GetAddress(DBR, f)); int c = P.HasFlag(StatusFlags.Carry) ? 1 : 0; int s = A + v + c; P = (s > 255) ? (P | StatusFlags.Carry) : (P & ~StatusFlags.Carry); P = (((A ^ s) & (v ^ s) & 0x80) != 0) ? (P | StatusFlags.Overflow) : (P & ~StatusFlags.Overflow); A = (byte)s; SetZeroAndNegativeFlags(A); break; } // ADC (DP),Y
            case 0xF3: { byte o = bus.Read(GetAddress(PBR, PC++)); ushort f = (ushort)(SP + o); byte v = bus.Read(GetAddress(DBR, f)); int c = P.HasFlag(StatusFlags.Carry) ? 1 : 0; int d = A - v - (1 - c); P = (d >= 0) ? (P | StatusFlags.Carry) : (P & ~StatusFlags.Carry); P = (((A ^ d) & (~v ^ d) & 0x80) != 0) ? (P | StatusFlags.Overflow) : (P & ~StatusFlags.Overflow); A = (byte)d; SetZeroAndNegativeFlags(A); break; } // SBC S
            case 0xF4: { ushort v = (ushort)(bus.Read(GetAddress(PBR, PC++)) | (bus.Read(GetAddress(PBR, PC++)) << 8)); Push((byte)(v >> 8)); Push((byte)(v & 0xFF)); break; } // PEA
            case 0xF5: { byte o = bus.Read(GetAddress(PBR, PC++)); ushort a = (ushort)((DP + o + X) & 0xFFFF); byte v = bus.Read(GetAddress(DBR, a)); int c = P.HasFlag(StatusFlags.Carry) ? 1 : 0; int d = A - v - (1 - c); P = (d >= 0) ? (P | StatusFlags.Carry) : (P & ~StatusFlags.Carry); P = (((A ^ d) & (~v ^ d) & 0x80) != 0) ? (P | StatusFlags.Overflow) : (P & ~StatusFlags.Overflow); A = (byte)d; SetZeroAndNegativeFlags(A); break; } // SBC DP,X
            case 0xF8: P |= StatusFlags.DecimalMode; break; // SED
            case 0xF9: { ushort b = (ushort)(bus.Read(GetAddress(PBR, PC++)) | (bus.Read(GetAddress(PBR, PC++)) << 8)); ushort f = (ushort)(b + Y); byte v = bus.Read(GetAddress(DBR, f)); int c = P.HasFlag(StatusFlags.Carry) ? 1 : 0; int d = A - v - (1 - c); P = (d >= 0) ? (P | StatusFlags.Carry) : (P & ~StatusFlags.Overflow); P = (((A ^ d) & (~v ^ d) & 0x80) != 0) ? (P | StatusFlags.Overflow) : (P & ~StatusFlags.Overflow); A = (byte)d; SetZeroAndNegativeFlags(A); break; } // SBC A,Y
            case 0xFB: { bool oc = P.HasFlag(StatusFlags.Carry); if (EmulationMode) P |= StatusFlags.Carry; else P &= ~StatusFlags.Carry; EmulationMode = oc; if (EmulationMode) { X &= 0xFF; Y &= 0xFF; SP = (ushort)(0x0100 | (SP & 0xFF)); } break; } // XCE
            case 0xFC: { ushort i = (ushort)(bus.Read(GetAddress(PBR, PC++)) | (bus.Read(GetAddress(PBR, PC++)) << 8)); ushort f = (ushort)(bus.Read(GetAddress(DBR, i)) | (bus.Read(GetAddress(DBR, (ushort)(i + 1))) << 8)); ushort r = (ushort)(PC - 1); Push((byte)(r >> 8)); Push((byte)(r & 0xFF)); PC = f; break; } // JSR (A,X)
            case 0xFD: { ushort b = (ushort)(bus.Read(GetAddress(PBR, PC++)) | (bus.Read(GetAddress(PBR, PC++)) << 8)); ushort f = (ushort)(b + X); byte v = bus.Read(GetAddress(DBR, f)); int c = P.HasFlag(StatusFlags.Carry) ? 1 : 0; int d = A - v - (1 - c); P = (d >= 0) ? (P | StatusFlags.Carry) : (P & ~StatusFlags.Carry); P = (((A ^ d) & (~v ^ d) & 0x80) != 0) ? (P | StatusFlags.Overflow) : (P & ~StatusFlags.Overflow); A = (byte)d; SetZeroAndNegativeFlags(A); break; } // SBC A,X
            case 0xFF: { ushort l = bus.Read(GetAddress(PBR, PC++)); ushort h = bus.Read(GetAddress(PBR, PC++)); byte b = bus.Read(GetAddress(PBR, PC++)); uint ba = (uint)((b << 16) | (h << 8) | l); uint f = ba + X; byte v = bus.Read(f); int c = P.HasFlag(StatusFlags.Carry) ? 1 : 0; int d = A - v - (1 - c); P = (d >= 0) ? (P | StatusFlags.Carry) : (P & ~StatusFlags.Carry); P = (((A ^ d) & (~v ^ d) & 0x80) != 0) ? (P | StatusFlags.Overflow) : (P & ~StatusFlags.Overflow); A = (byte)d; SetZeroAndNegativeFlags(A); break; } // SBC AL,X
            case 0x06: // ASL Direct Page
                {
                    byte dpOffset = bus.Read(GetAddress(PBR, PC++));
                    ushort address = (ushort)((DP + dpOffset) & 0xFFFF);

                    byte value = bus.Read(GetAddress(DBR, address));
                    P = (value & 0x80) != 0 ? (P | StatusFlags.Carry) : (P & ~StatusFlags.Carry);
                    value <<= 1;
                    bus.Write(GetAddress(DBR, address), value);

                    SetZeroAndNegativeFlags(value);
                    break;
                }
            case 0x5B: // TCD - Transfer C to DP
                {
                    DP = C;
                    SetZeroAndNegativeFlags16(DP);
                    break;
                }
            case 0x66: // ROR Direct Page
                {
                    byte dpOffset = bus.Read(GetAddress(PBR, PC++));
                    ushort address = (ushort)((DP + dpOffset) & 0xFFFF);

                    byte value = bus.Read(GetAddress(DBR, address));
                    bool oldCarry = P.HasFlag(StatusFlags.Carry);

                    P = (value & 1) == 1 ? (P | StatusFlags.Carry) : (P & ~StatusFlags.Carry);

                    value >>= 1;
                    if (oldCarry)
                    {
                        value |= 0x80;
                    }

                    bus.Write(GetAddress(DBR, address), value);
                    SetZeroAndNegativeFlags(value);
                    break;
                }
                case 0xBC: // LDY Absolute,X
                {
                    ushort baseAddr = (ushort)(bus.Read(GetAddress(PBR, PC++)) | (bus.Read(GetAddress(PBR, PC++)) << 8));
                    ushort finalOffset = (ushort)(baseAddr + X);
                    Y = bus.Read(GetAddress(DBR, finalOffset));
                    SetZeroAndNegativeFlags(Y);
                    break;
                }
                case 0x36: // ROL Direct Page,X
                {
                    byte dpOffset = bus.Read(GetAddress(PBR, PC++));
                    ushort address = (ushort)((DP + dpOffset + X) & 0xFFFF);

                    byte value = bus.Read(GetAddress(DBR, address));
                    bool oldCarry = P.HasFlag(StatusFlags.Carry);

                    P = (value & 0x80) != 0 ? (P | StatusFlags.Carry) : (P & ~StatusFlags.Carry);

                    value <<= 1;
                    if (oldCarry) value |= 1;

                    bus.Write(GetAddress(DBR, address), value);
                    SetZeroAndNegativeFlags(value);
                    break;
                }
                case 0x46: // LSR Direct Page
                {
                    byte dpOffset = bus.Read(GetAddress(PBR, PC++));
                    ushort address = (ushort)((DP + dpOffset) & 0xFFFF);

                    byte value = bus.Read(GetAddress(DBR, address));

                    P = (value & 1) == 1 ? (P | StatusFlags.Carry) : (P & ~StatusFlags.Carry);

                    value >>= 1;

                    bus.Write(GetAddress(DBR, address), value);

                    P = value == 0 ? (P | StatusFlags.Zero) : (P & ~StatusFlags.Zero);
                    P &= ~StatusFlags.Negative; // El bit 7 siempre es 0, así que N siempre es 0.
                    break;
                }
                case 0x0C: // TSB Absolute
                {
                    ushort address = (ushort)(bus.Read(GetAddress(PBR, PC++)) | (bus.Read(GetAddress(PBR, PC++)) << 8));

                    byte value = bus.Read(GetAddress(DBR, address));

                    // "Test": Actualiza el flag Zero basado en A & valor.
                    P = (A & value) == 0 ? (P | StatusFlags.Zero) : (P & ~StatusFlags.Zero);

                    // "Set": Realiza un OR y escribe el resultado en memoria.
                    value |= A;
                    bus.Write(GetAddress(DBR, address), value);
                    break;
                }
                case 0x1D: // ORA Absolute,X
                {
                    ushort baseAddr = (ushort)(bus.Read(GetAddress(PBR, PC++)) | (bus.Read(GetAddress(PBR, PC++)) << 8));
                    ushort finalOffset = (ushort)(baseAddr + X);

                    byte value = bus.Read(GetAddress(DBR, finalOffset));
                    A |= value;

                    SetZeroAndNegativeFlags(A);
                    break;
                }
                case 0x78: // SEI - Set Interrupt Disable
                {
                    P |= StatusFlags.InterruptDisable;
                    break;
                }
                case 0xA9: // LDA Immediate
                {
                    A = bus.Read(GetAddress(PBR, PC++));
                    SetZeroAndNegativeFlags(A);
                    break;
                }
                case 0x2B: // PLA - Pull Accumulator
                {
                    A = Pop();
                    SetZeroAndNegativeFlags(A);
                    break;
                }
                case 0x3C: // TRB Absolute
                {
                    ushort address = (ushort)(bus.Read(GetAddress(PBR, PC++)) | (bus.Read(GetAddress(PBR, PC++)) << 8));
                    byte value = bus.Read(GetAddress(DBR, address));

                    P = (A & value) == 0 ? (P | StatusFlags.Zero) : (P & ~StatusFlags.Zero);

                    value &= (byte)~A;
                    bus.Write(GetAddress(DBR, address), value);
                    break;
                }
                case 0xC6: // DEC Direct Page
                {
                    byte dpOffset = bus.Read(GetAddress(PBR, PC++));
                    ushort address = (ushort)((DP + dpOffset) & 0xFFFF);

                    byte value = bus.Read(GetAddress(DBR, address));
                    value--;
                    bus.Write(GetAddress(DBR, address), value);

                    SetZeroAndNegativeFlags(value);
                    break;
                }
                case 0xCC: // CPY Absolute
                {
                    ushort address = (ushort)(bus.Read(GetAddress(PBR, PC++)) | (bus.Read(GetAddress(PBR, PC++)) << 8));
                    byte value = bus.Read(GetAddress(DBR, address));
                    byte result = (byte)(Y - value);

                    SetZeroAndNegativeFlags(result);
                    P = (Y >= value) ? (P | StatusFlags.Carry) : (P & ~StatusFlags.Carry);
                    break;
                }
                case 0xFE: // INC Absolute,X
                {
                    ushort baseAddr = (ushort)(bus.Read(GetAddress(PBR, PC++)) | (bus.Read(GetAddress(PBR, PC++)) << 8));
                    ushort finalAddr = (ushort)(baseAddr + X);

                    byte value = bus.Read(GetAddress(DBR, finalAddr));
                    value++;
                    bus.Write(GetAddress(DBR, finalAddr), value);

                    SetZeroAndNegativeFlags(value);
                    break;
                }
                case 0x7C: // JMP (Absolute,X) Indirect
                {
                    ushort baseAddr = (ushort)(bus.Read(GetAddress(PBR, PC++)) | (bus.Read(GetAddress(PBR, PC++)) << 8));
                    ushort indirectAddr = (ushort)(baseAddr + X);

                    // Lee la dirección final de 16-bit
                    PC = (ushort)(bus.Read(GetAddress(PBR, indirectAddr)) | (bus.Read(GetAddress(PBR, (ushort)(indirectAddr + 1))) << 8));
                    break;
                }
                case 0x72: { byte o=bus.Read(GetAddress(PBR,PC++)); ushort i=(ushort)((DP+o)&0xFFFF); ushort f=(ushort)(bus.Read(GetAddress(DBR,i))|(bus.Read(GetAddress(DBR,(ushort)(i+1)))<<8)); byte v=bus.Read(GetAddress(DBR,f)); int c=P.HasFlag(StatusFlags.Carry)?1:0; int s=A+v+c; P=(s>255)?(P|StatusFlags.Carry):(P&~StatusFlags.Carry); P=(((A^s)&(v^s)&0x80)!=0)?(P|StatusFlags.Overflow):(P&~StatusFlags.Overflow); A=(byte)s; SetZeroAndNegativeFlags(A); break; } // ADC (DP),I
                case 0x24: { byte o=bus.Read(GetAddress(PBR,PC++)); ushort a=(ushort)((DP+o)&0xFFFF); byte v=bus.Read(GetAddress(DBR,a)); P=(A&v)==0?(P|StatusFlags.Zero):(P&~StatusFlags.Zero); P=(v&0x80)!=0?(P|StatusFlags.Negative):(P&~StatusFlags.Negative); P=(v&0x40)!=0?(P|StatusFlags.Overflow):(P&~StatusFlags.Overflow); break; } // BIT DP
                case 0x25: { byte o=bus.Read(GetAddress(PBR,PC++)); ushort a=(ushort)((DP+o)&0xFFFF); byte v=bus.Read(GetAddress(DBR,a)); A&=v; SetZeroAndNegativeFlags(A); break; } // AND DP
                case 0x26: { byte o=bus.Read(GetAddress(PBR,PC++)); ushort a=(ushort)((DP+o)&0xFFFF); byte v=bus.Read(GetAddress(DBR,a)); bool oc=P.HasFlag(StatusFlags.Carry); P=(v&0x80)!=0?(P|StatusFlags.Carry):(P&~StatusFlags.Carry); v<<=1; if(oc)v|=1; bus.Write(GetAddress(DBR,a),v); SetZeroAndNegativeFlags(v); break; } // ROL DP
                case 0x87: { byte o=bus.Read(GetAddress(PBR,PC++)); ushort i=(ushort)((DP+o)&0xFFFF); ushort l=bus.Read(GetAddress(DBR,i)); ushort h=bus.Read(GetAddress(DBR,(ushort)(i+1))); byte b=bus.Read(GetAddress(DBR,(ushort)(i+2))); uint f=(uint)((b<<16)|(h<<8)|l); bus.Write(f,A); break; } // STA [DP]
                case 0xA2: { X=bus.Read(GetAddress(PBR,PC++)); SetZeroAndNegativeFlags(X); break; } // LDX #
            #endregion
            default:
                Console.WriteLine($"❌ Opcode Desconocido: ${opcode:X2} en la dirección ${GetAddress(PBR, (ushort)(PC - 1)):X6}");
                // Para detener el bucle, podríamos necesitar un mecanismo para cerrar la ventana
                break;
        }
    }
}