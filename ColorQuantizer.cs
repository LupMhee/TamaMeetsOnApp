using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace TamagotchiMeetsOnEditor
{
    public class ColorQuantizer
    {
        public class QuantizedResult
        {
            public List<Color> Palette { get; set; } = new List<Color>();
            public byte[] IndexedImage { get; set; } = Array.Empty<byte>();
        }

        public QuantizedResult Quantize(Bitmap bitmap, int maxColors)
        {
            var colors = new List<Color>();
            
            var colorFrequency = new Dictionary<Color, int>();
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    Color pixel = bitmap.GetPixel(x, y);
                    Color key = pixel;
                    if (colorFrequency.Count > maxColors * 10)
                    {
                        key = QuantizeColor(pixel);
                    }
                    if (!colorFrequency.ContainsKey(key))
                    {
                        colorFrequency[key] = 0;
                    }
                    colorFrequency[key]++;
                }
            }

            var topColors = colorFrequency
                .OrderByDescending(kvp => kvp.Value)
                .Take(maxColors)
                .Select(kvp => kvp.Key)
                .ToList();

            while (topColors.Count < maxColors && topColors.Count < colorFrequency.Count)
            {
                var remaining = colorFrequency.Keys
                    .Where(c => !topColors.Contains(c))
                    .OrderByDescending(c => colorFrequency[c])
                    .FirstOrDefault();
                if (remaining != default(Color))
                {
                    topColors.Add(remaining);
                }
                else
                {
                    break;
                }
            }

            var usedColors = new HashSet<Color>();
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    Color pixel = bitmap.GetPixel(x, y);
                    usedColors.Add(pixel);
                }
            }

            var missingColors = usedColors.Where(c => !topColors.Contains(c)).ToList();
            
            if (missingColors.Count > 0 && topColors.Count >= maxColors)
            {
                var sortedPalette = topColors
                    .Select(c => new { Color = c, Frequency = colorFrequency.ContainsKey(c) ? colorFrequency[c] : 0 })
                    .OrderBy(x => x.Frequency)
                    .ToList();

                int replaceIndex = 0;
                foreach (var missingColor in missingColors.Take(Math.Min(missingColors.Count, maxColors / 4)))
                {
                    if (replaceIndex < sortedPalette.Count)
                    {
                        int indexToReplace = topColors.IndexOf(sortedPalette[replaceIndex].Color);
                        if (indexToReplace >= 0)
                        {
                            topColors[indexToReplace] = missingColor;
                            replaceIndex++;
                        }
                    }
                }
            }
            else if (missingColors.Count > 0 && topColors.Count < maxColors)
            {
                foreach (var missingColor in missingColors.Take(maxColors - topColors.Count))
                {
                    topColors.Add(missingColor);
                }
            }

            var palette = topColors.ToList();

            var indexedImage = new byte[bitmap.Width * bitmap.Height];
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    Color pixel = bitmap.GetPixel(x, y);
                    
                    int bestIndex = 0;
                    double bestDistance = double.MaxValue;
                    for (int i = 0; i < palette.Count; i++)
                    {
                        double distance = PerceptualColorDistance(pixel, palette[i]);
                        if (distance < bestDistance)
                        {
                            bestDistance = distance;
                            bestIndex = i;
                        }
                    }
                    
                    indexedImage[y * bitmap.Width + x] = (byte)bestIndex;
                }
            }

            return new QuantizedResult
            {
                Palette = palette,
                IndexedImage = indexedImage
            };
        }

        private Color QuantizeColor(Color color)
        {
            int r = (color.R / 16) * 16;
            int g = (color.G / 16) * 16;
            int b = (color.B / 16) * 16;
            return Color.FromArgb(255, r, g, b);
        }

        private double ColorDistance(Color c1, Color c2)
        {
            int dr = c1.R - c2.R;
            int dg = c1.G - c2.G;
            int db = c1.B - c2.B;
            return Math.Sqrt(dr * dr + dg * dg + db * db);
        }

        private double PerceptualColorDistance(Color c1, Color c2)
        {
            int dr = c1.R - c2.R;
            int dg = c1.G - c2.G;
            int db = c1.B - c2.B;
            return Math.Sqrt(2 * dr * dr + 4 * dg * dg + 3 * db * db);
        }
    }
}
