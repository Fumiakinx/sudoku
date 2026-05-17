using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.IO;

public class GenerateRoulette {
    public static void Main() {
        string srcPath = @"Assets\Textures\Life\life_roulette_v9.png";
        string destPath = @"Assets\Textures\Life\LifeRouletteSprites_New.png";

        if (!File.Exists(srcPath)) {
            Console.WriteLine("Source file not found: " + srcPath);
            return;
        }

        Console.WriteLine("Loading source image: " + srcPath);
        using (Bitmap srcTex = new Bitmap(srcPath)) {
            int numCells = 10;
            int cellW = 110;
            int cellH = 110;
            
            // 10個のセルを横に並べたスプライトシート (1100 x 110)
            using (Bitmap destTex = new Bitmap(cellW * numCells, cellH, PixelFormat.Format32bppArgb)) {
                using (Graphics g = Graphics.FromImage(destTex)) {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.InterpolationMode = InterpolationMode.HighQualityBilinear;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.Clear(Color.Transparent);

                    // 人生ゲームカラーパレットの定義
                    Color[] themeColors = new Color[] {
                        Color.FromArgb(255, 144, 219, 93),   // 0: 黄緑 (10の色)
                        Color.FromArgb(255, 255, 230, 0),    // 1: 黄
                        Color.FromArgb(255, 255, 137, 0),    // 2: 橙
                        Color.FromArgb(255, 230, 40, 40),     // 3: 赤
                        Color.FromArgb(255, 255, 120, 170),   // 4: 桃
                        Color.FromArgb(255, 235, 100, 145),   // 5: 濃桃
                        Color.FromArgb(255, 130, 30, 170),    // 6: 紫
                        Color.FromArgb(255, 130, 170, 230),   // 7: 淡青/ライトブルー
                        Color.FromArgb(255, 0, 180, 255),     // 8: 水色
                        Color.FromArgb(255, 45, 65, 140)      // 9: 紺
                    };

                    for (int cellIdx = 0; cellIdx < numCells; cellIdx++) {
                        int destStartX = cellIdx * cellW;
                        Color bgColor = themeColors[cellIdx];

                        // --- 1. 角丸四角形背景とベゼル(白枠)の描画 ---
                        int pad = 2; // 周囲の余白
                        float drawX = destStartX + pad;
                        float drawY = pad;
                        float drawW = cellW - pad * 2;
                        float drawH = cellH - pad * 2;
                        float radius = 24f; // 角丸の半径

                        using (GraphicsPath path = GetRoundedRectPath(drawX, drawY, drawW, drawH, radius)) {
                            // 中身を塗りつぶし
                            using (Brush brush = new SolidBrush(bgColor)) {
                                g.FillPath(brush, path);
                            }

                            // 白枠 (Bezel) を描画 (太さ 3.5px)
                            using (Pen pen = new Pen(Color.White, 3.5f)) {
                                pen.Alignment = PenAlignment.Inset;
                                g.DrawPath(pen, path);
                            }
                        }

                        // --- 2. 元画像でのセルの座標定義 ---
                        float srcXStart = cellIdx * 102.4f;
                        float srcYStart = 400f; // 円の並びがあるY座標の開始位置
                        int srcW = 103;
                        int srcH = 224;

                        // --- 3. まず「背景のカラフルな円」の領域をスキャンして中心を特定する ---
                        int minCircleX = srcW, maxCircleX = 0;
                        int minCircleY = srcH, maxCircleY = 0;
                        bool foundCircle = false;

                        for (int sy = 5; sy < srcH - 5; sy++) {
                            for (int sx = 5; sx < srcW - 5; sx++) {
                                Color sc = srcTex.GetPixel((int)srcXStart + sx, (int)srcYStart + sy);
                                // 白背景(RGB=255,255,255)以外のカラフルな色を検出
                                if (sc.R < 240 || sc.G < 240 || sc.B < 240) {
                                    if (sx < minCircleX) minCircleX = sx;
                                    if (sx > maxCircleX) maxCircleX = sx;
                                    if (sy < minCircleY) minCircleY = sy;
                                    if (sy > maxCircleY) maxCircleY = sy;
                                    foundCircle = true;
                                }
                            }
                        }

                        // 円が見つからない場合のフォールバック（セルの中心を仮定）
                        float circleCentX = srcW / 2f;
                        float circleCentY = srcH / 2f;
                        if (foundCircle) {
                            circleCentX = (minCircleX + maxCircleX) / 2f;
                            circleCentY = (minCircleY + maxCircleY) / 2f;
                        }

                        // --- 4. 円の中心から「半径 24px の内側」だけで純白文字を精密スキャンする ---
                        int minCharX = srcW, maxCharX = 0;
                        int minCharY = srcH, maxCharY = 0;
                        bool foundChar = false;
                        float scanRadius = 24f;

                        int startScanX = (int)Math.Max(0, circleCentX - scanRadius);
                        int endScanX = (int)Math.Min(srcW - 1, circleCentX + scanRadius);
                        int startScanY = (int)Math.Max(0, circleCentY - scanRadius);
                        int endScanY = (int)Math.Min(srcH - 1, circleCentY + scanRadius);

                        for (int sy = startScanY; sy <= endScanY; sy++) {
                            for (int sx = startScanX; sx <= endScanX; sx++) {
                                Color sc = srcTex.GetPixel((int)srcXStart + sx, (int)srcYStart + sy);
                                // 円の内側にある文字（完全な白 RGB=255）を検出
                                if (sc.R >= 253 && sc.G >= 253 && sc.B >= 253) {
                                    if (sx < minCharX) minCharX = sx;
                                    if (sx > maxCharX) maxCharX = sx;
                                    if (sy < minCharY) minCharY = sy;
                                    if (sy > maxCharY) maxCharY = sy;
                                    foundChar = true;
                                }
                            }
                        }

                        // --- 5. 文字を新しいセルの中央に縮小描画する ---
                        if (foundChar) {
                            float charCentX = (minCharX + maxCharX) / 2f;
                            float charCentY = (minCharY + maxCharY) / 2f;

                            // 縮小スケール係数 (上品な余白を確保するため 80% 縮小)
                            float charScale = 0.80f;

                            for (int y = 0; y < cellH; y++) {
                                for (int x = 0; x < cellW; x++) {
                                    // 中心 (55, 55) からの相対座標を逆算
                                    float rx = (x - 55f) / charScale;
                                    float ry = (y - 55f) / charScale;

                                    float srcX = charCentX + rx;
                                    float srcY = charCentY + ry;

                                    int sx = (int)Math.Round(srcX);
                                    int sy = (int)Math.Round(srcY);

                                    // 円の中心から半径 24px の安全な内側だけをコピー対象とする
                                    if (sx >= startScanX && sx <= endScanX && sy >= startScanY && sy <= endScanY) {
                                        Color sc = srcTex.GetPixel((int)srcXStart + sx, (int)srcYStart + sy);
                                        
                                        // 文字ピクセル（白）のみをブレンド描画
                                        if (sc.R >= 250 && sc.G >= 250 && sc.B >= 250) {
                                            float charAlpha = sc.A / 255f;
                                            Color current = destTex.GetPixel(destStartX + x, y);
                                            
                                            int blendedA = (int)Math.Max(current.A, sc.A);
                                            int blendedR = (int)(current.R * (1f - charAlpha) + 255 * charAlpha);
                                            int blendedG = (int)(current.G * (1f - charAlpha) + 255 * charAlpha);
                                            int blendedB = (int)(current.B * (1f - charAlpha) + 255 * charAlpha);

                                            blendedA = Math.Min(255, Math.Max(0, blendedA));
                                            blendedR = Math.Min(255, Math.Max(0, blendedR));
                                            blendedG = Math.Min(255, Math.Max(0, blendedG));
                                            blendedB = Math.Min(255, Math.Max(0, blendedB));

                                            destTex.SetPixel(destStartX + x, y, Color.FromArgb(blendedA, blendedR, blendedG, blendedB));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // 保存先フォルダの確認・作成
                string destDir = Path.GetDirectoryName(destPath);
                if (!Directory.Exists(destDir)) {
                    Directory.CreateDirectory(destDir);
                }

                // PNGとして保存
                destTex.Save(destPath, ImageFormat.Png);
                Console.WriteLine("Successfully saved ultra-premium clean roulette texture to: " + destPath);
            }
        }
    }

    private static GraphicsPath GetRoundedRectPath(float x, float y, float width, float height, float radius) {
        GraphicsPath path = new GraphicsPath();
        float diameter = radius * 2;
        path.AddArc(x, y, diameter, diameter, 180, 90);
        path.AddArc(x + width - diameter, y, diameter, diameter, 270, 90);
        path.AddArc(x + width - diameter, y + height - diameter, diameter, diameter, 0, 90);
        path.AddArc(x, y + height - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
