// Program.cs
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("🚀 Ejecutando Paso 5 (Corregido): Implementación del Bus de Memoria...");

        // 1. Crear el Bus y el CPU
        var bus = new Bus();
        var cpu = new CPU(bus);

        // 2. Escribir el CÓDIGO del programa directamente en la memoria ROM del bus
        int codePtr = 0x8000;
        bus.Write((ushort)codePtr++, 0xA9); bus.Write((ushort)codePtr++, 0x00); // LDA #$00
        bus.Write((ushort)codePtr++, 0xA2); bus.Write((ushort)codePtr++, 0x00); // LDX #$00
        // Etiqueta "LOOP"
        bus.Write((ushort)codePtr++, 0x7D); bus.Write((ushort)codePtr++, 0x00); bus.Write((ushort)codePtr++, 0x01); // ADC $0100,X
        bus.Write((ushort)codePtr++, 0xE8);                                   // INX
        bus.Write((ushort)codePtr++, 0xE0); bus.Write((ushort)codePtr++, 0x05); // CPX #$05
        bus.Write((ushort)codePtr++, 0xD0); bus.Write((ushort)codePtr++, 0xF8); // BNE LOOP
        bus.Write((ushort)codePtr++, 0x00);                                   // BRK

        // 3. Escribir los DATOS directamente en la WRAM del bus
        bus.Write(0x0100, 10);
        bus.Write(0x0101, 20);
        bus.Write(0x0102, 30);
        bus.Write(0x0103, 40);
        bus.Write(0x0104, 5);

        Console.WriteLine("Código y datos cargados correctamente en el Bus.");

        // 4. Configurar y ejecutar
        cpu.PC = 0x8000;
        cpu.Run();

        Console.WriteLine("Ejecución finalizada.");
        Console.WriteLine("--- Estado Final del CPU ---");
        Console.WriteLine($"Registro A (Suma): {cpu.A} (Esperado: 105)");
        Console.WriteLine($"Registro X (Contador): {cpu.X} (Esperado: 5)");

        if (cpu.A == 105 && cpu.X == 5)
        {
            Console.WriteLine("✅ ¡La refactorización al Bus de Memoria fue un éxito!");
        }
        else
        {
            Console.WriteLine("❌ Error en los resultados.");
        }
    }
}