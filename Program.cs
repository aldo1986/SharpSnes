// Program.cs
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("🚀 Ejecutando Paso 6: Conexión del CPU a la PPU...");

        var bus = new Bus();
        var cpu = new CPU(bus);

        // Programa de prueba:
        // 0xAD 0x3F 0x21  -> LDA $213F  (Carga en A el valor desde el registro de estado de la PPU)
        // 0x00            -> BRK        (Detiene la ejecución)
        int codePtr = 0x8000;
        bus.Write((ushort)codePtr++, 0xAD); // Opcode LDA Absolute
        bus.Write((ushort)codePtr++, 0x3F); // Dirección baja ($213F)
        bus.Write((ushort)codePtr++, 0x21); // Dirección alta ($213F)
        bus.Write((ushort)codePtr++, 0x00); // Opcode BRK

        Console.WriteLine("ROM de prueba para leer registro PPU cargada.");

        cpu.PC = 0x8000;
        cpu.Run();

        Console.WriteLine("Ejecución finalizada.");
        Console.WriteLine("--- Estado Final del CPU ---");
        // El valor 0x80 en hexadecimal es 128 en decimal.
        Console.WriteLine($"Registro A: {cpu.A} (Esperado: 128)");

        if (cpu.A == 128)
        {
            Console.WriteLine("✅ ¡Éxito! El CPU ha leído correctamente un registro de la PPU a través del Bus.");
        }
        else
        {
            Console.WriteLine("❌ Error en el resultado.");
        }
    }
}