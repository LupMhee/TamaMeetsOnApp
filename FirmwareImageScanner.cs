using System;
using System.Collections.Generic;

namespace TamagotchiMeetsOnEditor
{
    public static class FirmwareImageScanner
    {
        public static List<FirmwareImage> ScanForImages(byte[] firmware)
        {
            List<FirmwareImage> images = new List<FirmwareImage>();

            for (int offset = 0; offset < firmware.Length - 10; offset++)
            {
                int width = firmware[offset + 0];
                int height = firmware[offset + 1];
                int paletteSize = firmware[offset + 2];

                if (firmware.Length - offset > 10 &&
                    width > 0 && width <= 128 &&
                    height > 0 && height <= 128 &&
                    paletteSize > 0 &&
                    firmware[offset + 3] == 0 &&
                    firmware[offset + 4] == 1 &&
                    firmware[offset + 5] == 255)
                {
                    try
                    {
                        int headerSize = 6 + paletteSize * 2;
                        int pixelPerByte = paletteSize > 16 ? 1 : 2;
                        int imageDataSize = (int)Math.Ceiling((width * height) / (double)pixelPerByte);
                        int totalSize = headerSize + imageDataSize;

                        if (offset + totalSize <= firmware.Length)
                        {
                            byte[] imageBytes = new byte[totalSize];
                            Array.Copy(firmware, offset, imageBytes, 0, totalSize);

                            byte[] paletteData = new byte[paletteSize * 2];
                            Array.Copy(firmware, offset + 6, paletteData, 0, paletteSize * 2);

                            byte[] pixelData = new byte[imageDataSize];
                            Array.Copy(firmware, offset + headerSize, pixelData, 0, imageDataSize);

                            FirmwareImage image = new FirmwareImage(offset, imageBytes, width, height, paletteSize, paletteData, pixelData);
                            images.Add(image);
                            offset += totalSize - 1;
                        }
                    }
                    catch
                    {
                    }
                }
            }

            return images;
        }
    }
}
