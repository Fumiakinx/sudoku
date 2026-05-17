using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

public class Generate2x5Roulette {
    public static void Main() {
        string srcPath = @"Assets\Textures\Life\life_roulette_v9.png";
        string destPath = @"Assets\Textures\Life\life_roulette_2x5.png";
        string zoomPath = @"Assets\Textures\Life\verification_zoom.png";

        if (!File.Exists(srcPath)) {
            Console.WriteLine("Source file not found: " + srcPath);
            return;
        }

        Console.WriteLine("Processing " + srcPath + " into ultra-clean 2x5 grid...");

        using (Bitmap srcBmp = new Bitmap(srcPath)) {
            int srcW = srcBmp.Width;
            int srcH = srcBmp.Height;

            // 1セルあたり幅 110px、高さ 110px
            int cellW = 110;
            int cellH = 110;
            int numCols = 5;
            int numRows = 2;

            int destW = cellW * numCols;  // 550px
            int destH = cellH * numRows;  // 220px

            using (Bitmap destBmp = new Bitmap(destW, destH, PixelFormat.Format32bppArgb)) {
                // 背景を白で初期化
                using (Graphics g = Graphics.FromImage(destBmp)) {
                    g.Clear(Color.White);
                }

                float srcCellW = srcW / 10.0f;

                for (int i = 0; i < 10; i++) {
                    // 元画像からの切り出し中心
                    float srcCx = i * srcCellW + srcCellW / 2.0f;
                    float srcCy = 511.5f;

                    // 新しい画像内での配置先の列と行 (上段: 0〜4, 下段: 5〜9)
                    int col = i % 5;
                    int row = i / 5; // 0: 上段, 1: 下段

                    // 新しい画像内での配置先の中心
                    float destCx = col * cellW + cellW / 2.0f;
                    float destCy = row * cellH + cellH / 2.0f;

                    // 切り出すマルのサイズ
                    int cutSize = 106;

                    for (int dy = -cutSize / 2; dy <= cutSize / 2; dy++) {
                        for (int dx = -cutSize / 2; dx <= cutSize / 2; dx++) {
                            int sx = (int)Math.Round(srcCx + dx);
                            int sy = (int)Math.Round(srcCy + dy);

                            if (sx < 0 || sx >= srcW || sy < 0 || sy >= srcH)
                                continue;

                            int tx = (int)Math.Round(destCx + dx);
                            int ty = (int)Math.Round(destCy + dy);

                            if (tx < 0 || tx >= destW || ty < 0 || ty >= destH)
                                continue;

                            // 円の中心からの距離
                            float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                            Color srcColor = srcBmp.GetPixel(sx, sy);

                            // 円のフチのアンチエイリアス処理（半径47.0px〜49.5pxの間で白へ滑らかにブレンド）
                            float innerRadius = 47.0f;
                            float outerRadius = 49.5f;

                            if (dist <= innerRadius) {
                                // 円の内側（文字も含む）：そのままコピー
                                destBmp.SetPixel(tx, ty, srcColor);
                            } else if (dist >= outerRadius) {
                                // 円の外側（隣の円の映り込みノイズ）：完全に白
                                destBmp.SetPixel(tx, ty, Color.White);
                            } else {
                                // 円のフチのアンチエイリアス境界線：元の色と白を滑らかにブレンド（ジャギー防止）
                                float t = (dist - innerRadius) / (outerRadius - innerRadius);
                                int r = (int)(srcColor.R * (1.0f - t) + 255.0f * t);
                                int g = (int)(srcColor.G * (1.0f - t) + 255.0f * t);
                                int b = (int)(srcColor.B * (1.0f - t) + 255.0f * t);
                                destBmp.SetPixel(tx, ty, Color.FromArgb(255, r, g, b));
                            }
                        }
                    }
                }

                // 保存
                destBmp.Save(destPath, ImageFormat.Png);
                Console.WriteLine("Saved ultra-clean 2x5 grid roulette to: " + destPath);

                // --- 拡大確認用画像の生成 ---
                // ノイズが目立っていた 0番 と 1番 のセル境界部分（X=100〜120付近、Y=20〜80付近）を5倍に拡大した検証画像を保存します。
                int zoomSrcX = 80;
                int zoomSrcY = 20;
                int zoomW = 60;
                int zoomH = 60;
                int scale = 5;

                using (Bitmap zoomBmp = new Bitmap(zoomW * scale, zoomH * scale, PixelFormat.Format32bppArgb)) {
                    using (Graphics gZoom = Graphics.FromImage(zoomBmp)) {
                        // 補間モードを NearestNeighbor にしてピクセルをくっきり拡大
                        gZoom.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                        gZoom.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                        
                        gZoom.DrawImage(destBmp, 
                            new Rectangle(0, 0, zoomW * scale, zoomH * scale), 
                            new Rectangle(zoomSrcX, zoomSrcY, zoomW, zoomH), 
                            GraphicsUnit.Pixel);
                    }
                    zoomBmp.Save(zoomPath, ImageFormat.Png);
                    Console.WriteLine("Saved zoomed verification image to: " + zoomPath);
                }
            }
        }
    }
}
