// Program.cs
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("🚀 Ejecutando Paso 7: Memorias y Registros de la PPU...");

        var bus = new Bus();
        var cpu = new CPU(bus);

        // --- Programa de prueba ---
        int codePtr = 0x8000;
        
        // 1. Establecer brillo de pantalla a 10
        // LDA #$0A -> STA $2100
        bus.Write((ushort)codePtr++, 0xA9); bus.Write((ushort)codePtr++, 0x0A); // LDA #$0A (valor 10)
        bus.Write((ushort)codePtr++, 0x8D); bus.Write((ushort)codePtr++, 0x00); bus.Write((ushort)codePtr++, 0x21); // STA $2100
        
        // 2. Establecer la dirección de VRAM a $1234
        // LDA #$34 -> STA $2116 (byte bajo)
        bus.Write((ushort)codePtr++, 0xA9); bus.Write((ushort)codePtr++, 0x34);
        bus.Write((ushort)codePtr++, 0x8D); bus.Write((ushort)codePtr++, 0x16); bus.Write((ushort)codePtr++, 0x21);
        // LDA #$12 -> STA $2117 (byte alto)
        bus.Write((ushort)codePtr++, 0xA9); bus.Write((ushort)codePtr++, 0x12);
        bus.Write((ushort)codePtr++, 0x8D); bus.Write((ushort)codePtr++, 0x17); bus.Write((ushort)codePtr++, 0x21);

        // 3. Escribir el valor 0xAB en VRAM a través del puerto $2118
        // LDA #$AB -> STA $2118
        bus.Write((ushort)codePtr++, 0xA9); bus.Write((ushort)codePtr++, 0xAB);
        bus.Write((ushort)codePtr++, 0x8D); bus.Write((ushort)codePtr++, 0x18); bus.Write((ushort)codePtr++, 0x21);
        
        // 4. Leer el valor de vuelta desde el puerto $2139 para verificar
        // (Primero hay que volver a establecer la dirección a $1234)
        // LDA #$34 -> STA $2116
        bus.Write((ushort)codePtr++, 0xA9); bus.Write((ushort)codePtr++, 0x34);
        bus.Write((ushort)codePtr++, 0x8D); bus.Write((ushort)codePtr++, 0x16); bus.Write((ushort)codePtr++, 0x21);
        // LDA #$12 -> STA $2117
        bus.Write((ushort)codePtr++, 0xA9); bus.Write((ushort)codePtr++, 0x12);
        bus.Write((ushort)codePtr++, 0x8D); bus.Write((ushort)codePtr++, 0x17); bus.Write((ushort)codePtr++, 0x21);
        // LDA $2139 (Lee desde VRAM y lo pone en A)
        bus.Write((ushort)codePtr++, 0xAD); bus.Write((ushort)codePtr++, 0x39); bus.Write((ushort)codePtr++, 0x21);
        
        bus.Write((ushort)codePtr++, 0x00); // BRK

        cpu.PC = 0x8000;
        cpu.Run();

        Console.WriteLine("Ejecución finalizada.");
        Console.WriteLine("--- Estado Final del CPU ---");
        // El valor 0xAB en hexadecimal es 171 en decimal.
        Console.WriteLine($"Registro A (valor leído de VRAM): {cpu.A} (Esperado: 171)");

        if (cpu.A == 171)
        {
            Console.WriteLine("✅ ¡Éxito! El CPU escribió y leyó datos de la VRAM de la PPU.");
        }
        else
        {
            Console.WriteLine("❌ Error en el resultado.");
        }
    }
}