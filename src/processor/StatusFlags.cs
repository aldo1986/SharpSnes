[Flags]
public enum StatusFlags : byte
{
    Carry             = 1 << 0, // C
    Zero              = 1 << 1, // Z
    InterruptDisable  = 1 << 2, // I
    DecimalMode       = 1 << 3, // D
    Break             = 1 << 4, // B (Flag de índice de 8-bit en modo nativo)
    Unused            = 1 << 5, // m (Flag de Acumulador de 8-bit)
    Overflow          = 1 << 6, // V
    Negative          = 1 << 7, // N
}