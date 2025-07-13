[Flags]
public enum StatusFlags : byte
{
     // El orden y valor son importantes y corresponden al hardware real
    Carry             = 1 << 0, // C: 1 si la última operación generó un acarreo
    Zero              = 1 << 1, // Z: 1 si el resultado de una operación es cero
    InterruptDisable  = 1 << 2, // I: 1 para deshabilitar interrupciones
    DecimalMode       = 1 << 3, // D: 1 para activar el modo decimal (no usado en la SNES)
    Break             = 1 << 4, // B: 1 cuando ocurre una interrupción por BRK
    Unused            = 1 << 5, // Este bit no se usa
    Overflow          = 1 << 6, // V: 1 si la operación causó un desborde (overflow)
    Negative          = 1 << 7  // N: 1 si el bit más alto del resultado es 1 (es negativo)
}  