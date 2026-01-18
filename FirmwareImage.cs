using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

namespace TamagotchiMeetsOnEditor
{
    public class FirmwareImage
    {
        public int Offset { get; }
        public int Size { get; }
        public int Width { get; }
        public int Height { get; }
        public int ColorsCount { get; }
        private byte[] imageData;
        private Color[] palette;
        
        public byte[] ImageData => imageData;
        public Color[] Palette => palette;
        
        public void UpdateImageData(byte[] newImageData)
        {
            if (newImageData.Length == imageData.Length)
            {
                imageData = newImageData;
            }
            else if (newImageData.Length < imageData.Length)
            {
                byte[] padded = new byte[imageData.Length];
                Array.Copy(newImageData, 0, padded, 0, newImageData.Length);
                imageData = padded;
            }
            else
            {
                byte[] trimmed = new byte[imageData.Length];
                Array.Copy(newImageData, 0, trimmed, 0, imageData.Length);
                imageData = trimmed;
            }
            
            if (imageData.Length >= 6 + ColorsCount * 2)
            {
                byte[] paletteData = new byte[ColorsCount * 2];
                Array.Copy(imageData, 6, paletteData, 0, ColorsCount * 2);
                palette = DecodePalette(paletteData);
            }
        }

        public FirmwareImage(int offset, byte[] data, int width, int height, int colorsCount, byte[] paletteData, byte[] pixelData)
        {
            Offset = offset;
            Size = data.Length;
            Width = width;
            Height = height;
            ColorsCount = colorsCount;
            imageData = data;
            palette = DecodePalette(paletteData);
        }

        public string DisplayText => $"0x{Offset:X8} - {Width}×{Height} ({ColorsCount} colors)";

        private Color[] DecodePalette(byte[] paletteData)
        {
            System.Collections.Generic.List<Color> colors = new System.Collections.Generic.List<Color>();
            for (int i = 0; i < paletteData.Length; i += 2)
            {
                ushort color16 = (ushort)((paletteData[i] << 8) | paletteData[i + 1]);
                
                int blue = (int)Math.Round((((color16 & 0xF800) >> 11) / 31.0) * 255);
                int green = (int)Math.Round((((color16 & 0x07E0) >> 5) / 63.0) * 255);
                int red = (int)Math.Round(((color16 & 0x001F) / 31.0) * 255);
                colors.Add(Color.FromArgb(255, red, green, blue));
            }
            return colors.ToArray();
        }

        public Bitmap GetBitmap()
        {
            bool halfBytePixel = ColorsCount <= 16;
            Bitmap bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);

            if (halfBytePixel)
            {
                int pixelIndex = 0;
                int dataOffset = 6 + ColorsCount * 2;
                int maxPixels = Width * Height;
                
                for (int i = 0; i < maxPixels / 2 && dataOffset + i < imageData.Length; i++)
                {
                    byte byteData = imageData[dataOffset + i];
                    int pixel1Index = byteData & 0x0F;
                    int pixel2Index = (byteData >> 4) & 0x0F;
                    
                    if (pixelIndex < maxPixels)
                    {
                        int x1 = pixelIndex % Width;
                        int y1 = pixelIndex / Width;
                        if (pixel1Index < palette.Length)
                        {
                            bitmap.SetPixel(x1, y1, palette[pixel1Index]);
                        }
                    }
                    pixelIndex++;
                    
                    if (pixelIndex < maxPixels)
                    {
                        int x2 = pixelIndex % Width;
                        int y2 = pixelIndex / Width;
                        if (pixel2Index < palette.Length)
                        {
                            bitmap.SetPixel(x2, y2, palette[pixel2Index]);
                        }
                    }
                    pixelIndex++;
                }
            }
            else
            {
                int dataOffset = 6 + ColorsCount * 2;
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        int index = y * Width + x;
                        if (dataOffset + index < imageData.Length)
                        {
                            int colorIndex = imageData[dataOffset + index];
                            if (colorIndex < palette.Length)
                            {
                                bitmap.SetPixel(x, y, palette[colorIndex]);
                            }
                        }
                    }
                }
            }

            return bitmap;
        }

        public int GetChromaKeyIndex()
        {
            bool halfBytePixel = ColorsCount <= 16;
            int chromaKeyIndex = -1;
            
            if (palette.Length > 0)
            {
                for (int i = 0; i < palette.Length; i++)
                {
                    Color c = palette[i];
                    if (c.G > c.R && c.G > c.B && c.G > 150)
                    {
                        chromaKeyIndex = i;
                        break;
                    }
                }
                
                if (chromaKeyIndex < 0 && palette.Length > 0)
                {
                    Color c0 = palette[0];
                    if (c0.G > c0.R && c0.G > c0.B)
                    {
                        chromaKeyIndex = 0;
                    }
                }
                
                if (chromaKeyIndex < 0)
                {
                    bool index0Used = false;
                    int dataOffset = 6 + ColorsCount * 2;
                    
                    if (halfBytePixel)
                    {
                        int maxPixels = Width * Height;
                        for (int i = 0; i < maxPixels / 2 && dataOffset + i < imageData.Length; i++)
                        {
                            byte byteData = imageData[dataOffset + i];
                            int pixel1Index = byteData & 0x0F;
                            int pixel2Index = (byteData >> 4) & 0x0F;
                            
                            if (pixel1Index == 0 || pixel2Index == 0)
                            {
                                index0Used = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < Width * Height && dataOffset + i < imageData.Length; i++)
                        {
                            int colorIndex = imageData[dataOffset + i];
                            if (colorIndex == 0)
                            {
                                index0Used = true;
                                break;
                            }
                        }
                    }
                    
                    if (index0Used)
                    {
                        chromaKeyIndex = 0;
                    }
                }
            }
            
            return chromaKeyIndex;
        }
    }
}
