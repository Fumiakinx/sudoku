using System;
using System.Drawing;
using System.IO;

public class DetectBounds {
    public static void Main() {
        string path = @"Assets\Textures\Life\life_roulette_v9.png";
        if (!File.Exists(path)) {
            Console.WriteLine("Not found: " + path);
            return;
        }

        using (Bitmap bmp = new Bitmap(path)) {
            int w = bmp.Width;
            int h = bmp.Height;
            int minY = h, maxY = 0;
            int minX = w, maxX = 0;

            // 白背景 (RGB=255, 255, 255) 以外のピクセルを検出
            for (int y = 0; y < h; y++) {
                for (int x = 0; x < w; x++) {
                    Color c = bmp.GetPixel(x, y);
                    if (c.R < 250 || c.G < 250 || c.B < 250) {
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                    }
                }
            }

            Console.WriteLine("Bounds of colorful circles:");
            Console.WriteLine("X: " + minX + " to " + maxX);
            Console.WriteLine("Y: " + minY + " to " + maxY);
            Console.WriteLine("Circle Height: " + (maxY - minY));
            Console.WriteLine("Image Size: " + w + "x" + h);
            
            // 各セルの色を調べる
            int numCells = 10;
            float cellW = w / (float)numCells;
            Console.WriteLine("\nColor of each circle (center):");
            for (int i = 0; i < numCells; i++) {
                int cx = (int)(i * cellW + cellW / 2);
                int cy = (minY + maxY) / 2;
                Color c = bmp.GetPixel(cx, cy);
                Console.WriteLine("Circle " + i + " center color: R=" + c.R + ", G=" + c.G + ", B=" + c.B);
            }
        }
    }
}
