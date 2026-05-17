using System;
using System.Drawing;
using System.IO;

public class DetectBounds2 {
    public static void Main() {
        string path = @"Assets\Textures\Life\life_roulette_v9.png";
        if (!File.Exists(path)) {
            Console.WriteLine("Not found: " + path);
            return;
        }

        using (Bitmap bmp = new Bitmap(path)) {
            int w = bmp.Width;
            int h = bmp.Height;
            int numCells = 10;
            float cellW = w / (float)numCells;

            Console.WriteLine("Image Size: " + w + "x" + h);
            
            // 各セルごとに白以外のピクセルのバウンディングボックスを検出し、その中心の色をダンプする
            for (int i = 0; i < numCells; i++) {
                int startX = (int)(i * cellW);
                int endX = (int)((i + 1) * cellW);
                
                int minX = w, maxX = 0;
                int minY = h, maxY = 0;
                
                // 白ではない(R, G, Bすべてが240以上ではない)ピクセルを検出
                for (int y = 0; y < h; y++) {
                    for (int x = startX; x < endX; x++) {
                        Color c = bmp.GetPixel(x, y);
                        if (c.R < 240 || c.G < 240 || c.B < 240) {
                            if (x < minX) minX = x;
                            if (x > maxX) maxX = x;
                            if (y < minY) minY = y;
                            if (y > maxY) maxY = y;
                        }
                    }
                }
                
                if (minX <= maxX && minY <= maxY) {
                    int cx = (minX + maxX) / 2;
                    int cy = (minY + maxY) / 2;
                    Color c = bmp.GetPixel(cx, cy);
                    Console.WriteLine("Cell " + i + ": X=" + minX + " to " + maxX + " (width=" + (maxX - minX + 1) + "), Y=" + minY + " to " + maxY + " (height=" + (maxY - minY + 1) + "), Center=(" + cx + ", " + cy + "), Color=R=" + c.R + ", G=" + c.G + ", B=" + c.B);
                } else {
                    Console.WriteLine("Cell " + i + ": No non-white pixel found!");
                }
            }
        }
    }
}
