// PPU.cs
public class PPU
{
    private readonly Bus bus;

    // Memorias internas
    private readonly byte[] vram = new byte[64 * 1024];
    private readonly byte[] oam = new byte[544];
    private readonly byte[] cgram = new byte[512];
    private byte oamAddress = 0;
    private bool isSecondCgramWrite = false; // Para rastrear si estamos esperando el segundo byte de un color
    private byte cgramLowByte = 0;           // Para almacenar temporalmente el primer byte

    // Registros internos de la PPU
    private byte screenBrightness = 0;
    private ushort vramAddress = 0;
    private byte cgramAddress = 0;
    private ushort _horizontalScroll = 0;
    private ushort _verticalScroll = 0;

    public PPU(Bus bus)
    {
        this.bus = bus;
    }

    public byte Read(ushort address)
    {
        // ... (El método Read no cambia) ...
        switch (address)
        {
            case 0x213F: return 0x80;
            case 0x2139:
                byte data = vram[vramAddress];
                vramAddress++;
                return data;
            default: return 0;
        }
    }

    public void Write(ushort address, byte data)
    {
        switch (address)
        {
            case 0x2100: // Brillo
                this.screenBrightness = (byte)(data & 0x0F);
                break;

            // Registros de Scroll (sin cambios)
            case 0x210D: _horizontalScroll = (ushort)((_horizontalScroll & 0xFF00) | data); break;
            case 0x210E: _verticalScroll = (ushort)((_verticalScroll & 0xFF00) | data); break;
            case 0x210F: _horizontalScroll = (ushort)((_horizontalScroll & 0x00FF) | (data << 8)); break;
            case 0x2110: _verticalScroll = (ushort)((_verticalScroll & 0x00FF) | (data << 8)); break;

            // Registros de VRAM (sin cambios)
            case 0x2116: vramAddress = (ushort)((vramAddress & 0xFF00) | data); break;
            case 0x2117: vramAddress = (ushort)((vramAddress & 0x00FF) | (data << 8)); break;
            case 0x2118: vram[vramAddress++] = data; break;

            // Registros de OAM (sin cambios)
            case 0x2102: oamAddress = data; break;
            case 0x2103: break;
            case 0x2104: oam[oamAddress++] = data; break;

            // --- LÓGICA DE CGRAM CORREGIDA ---
            case 0x2121: // CGRAM Address
                this.cgramAddress = data;
                this.isSecondCgramWrite = false; // Cada vez que se establece una dirección, se reinicia el ciclo de escritura
                break;
            case 0x2122: // CGRAM Data Write
                if (!isSecondCgramWrite)
                {
                    // Este es el primer byte (el bajo)
                    cgramLowByte = data;
                    isSecondCgramWrite = true;
                }
                else
                {
                    // Este es el segundo byte (el alto). Ahora podemos escribir el color completo.
                    int byteAddress = cgramAddress * 2;
                    cgram[byteAddress] = cgramLowByte;
                    cgram[byteAddress + 1] = data; // 'data' es el byte alto

                    cgramAddress++; // Incrementar la dirección para la siguiente escritura de color
                    isSecondCgramWrite = false;
                }
                break;
        }
    }
public void RenderFrame(byte[] pixelBuffer)
    {
        // 1. Limpiar el buffer de la pantalla al inicio de cada fotograma
        Array.Clear(pixelBuffer, 0, pixelBuffer.Length);

        // --- 2. DIBUJAR EL FONDO ---
        const int TILEMAP_BASE = 0x1000;
        const int TILESET_BASE = 0x0000;

        for (int screenY = 0; screenY < 224; screenY++)
        {
            for (int screenX = 0; screenX < 256; screenX++)
            {
                // Aplicar el desplazamiento de scroll
                int backgroundX = (screenX + _horizontalScroll) % 512;
                int backgroundY = (screenY + _verticalScroll) % 512;

                // Calcular en qué tile del mapa estamos
                int tilemapX = backgroundX / 8;
                int tilemapY = backgroundY / 8;

                // Obtener la información de ese tile del mapa
                int tilemapEntryAddress = TILEMAP_BASE + (tilemapY * 32 + tilemapX) * 2;
                ushort tileInfo = (ushort)(vram[tilemapEntryAddress] | (vram[tilemapEntryAddress + 1] << 8));

                int tileNumber = tileInfo & 0x3FF;
                if (tileNumber == 0) continue;

                // Extraer el número de paleta (0-7) de los bits 10-12 del tileInfo
                int bgPaletteNum = (tileInfo >> 10) & 0x07;

                // Calcular la dirección del gráfico del tile
                int tileAddress = TILESET_BASE + (tileNumber * 16);

                // Encontrar el píxel exacto dentro del tile de 8x8
                int innerX = backgroundX % 8;
                int innerY = backgroundY % 8;

                // Decodificar el color del píxel a partir de los planos de bits
                byte plano0 = vram[tileAddress + innerY * 2];
                byte plano1 = vram[tileAddress + innerY * 2 + 1];
                int bit0 = (plano0 >> (7 - innerX)) & 1;
                int bit1 = (plano1 >> (7 - innerX)) & 1;
                int paletteIndex = (bit1 << 1) | bit0;
                if (paletteIndex == 0) continue;

                // Calcular el índice de color final usando la paleta del mapa
                int finalColorIndexBG = (bgPaletteNum * 16) + paletteIndex;

                // Convertir el color de SNES (BGR555) a PC (RGBA) y escribirlo
                ushort snesColor = (ushort)(cgram[finalColorIndexBG * 2] | (cgram[finalColorIndexBG * 2 + 1] << 8));
                byte r = (byte)((snesColor & 0x1F) * 8);
                byte g = (byte)(((snesColor >> 5) & 0x1F) * 8);
                byte b = (byte)(((snesColor >> 10) & 0x1F) * 8);

                int bufferIndex = (screenY * 256 + screenX) * 4;
                pixelBuffer[bufferIndex] = r;
                pixelBuffer[bufferIndex + 1] = g;
                pixelBuffer[bufferIndex + 2] = b;
                pixelBuffer[bufferIndex + 3] = 255;
            }
        }

        // --- 3. DIBUJAR LOS SPRITES (encima del fondo) ---
        for (int i = 0; i < 128; i++)
        {
            int oamIndex = i * 4;
            byte xPos = oam[oamIndex];
            byte yPos = oam[oamIndex + 1];
            byte tileNumber = oam[oamIndex + 2];
            byte attributes = oam[oamIndex + 3];

            if (yPos >= 224) continue;

            int oamPaletteNum = attributes & 0x07;
            int tileAddress = TILESET_BASE + (tileNumber * 16);

            for (int y = 0; y < 8; y++)
            {
                byte plano0 = vram[tileAddress + y * 2];
                byte plano1 = vram[tileAddress + y * 2 + 1];

                for (int x = 0; x < 8; x++)
                {
                    int paletteIndex = ((plano1 >> (7 - x)) & 1) << 1 | ((plano0 >> (7 - x)) & 1);
                    if (paletteIndex == 0) continue;

                    int screenX = xPos + x;
                    int screenY = yPos + y;
                    if (screenX >= 256 || screenY >= 224) continue;

                    int finalColorIndex = 128 + (oamPaletteNum * 16) + paletteIndex;

                    ushort snesColor = (ushort)(cgram[finalColorIndex * 2] | (cgram[finalColorIndex * 2 + 1] << 8));
                    byte r = (byte)((snesColor & 0x1F) * 8);
                    byte g = (byte)(((snesColor >> 5) & 0x1F) * 8);
                    byte b = (byte)(((snesColor >> 10) & 0x1F) * 8);

                    int bufferIndex = (screenY * 256 + screenX) * 4;
                    pixelBuffer[bufferIndex] = r;
                    pixelBuffer[bufferIndex + 1] = g;
                    pixelBuffer[bufferIndex + 2] = b;
                    pixelBuffer[bufferIndex + 3] = 255;
                }
            }
        }
    }
}