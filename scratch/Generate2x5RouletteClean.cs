using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

public class Generate2x5RouletteClean {
    public static void Main() {
        string srcPath = @"Assets\Textures\Life\life_roulette_v9.png";
        string destPath = @"Assets\Textures\Life\life_roulette_2x5.png";
        string zoomVerifyPath = @"scratch\roulette_verify_zoom.png";

        if (!File.Exists(srcPath)) {
            Console.WriteLine("Source file not found: " + srcPath);
            return;
        }

        Console.WriteLine("Processing " + srcPath + " with Intelligent Color-Distance Bleed Filter...");

        // 実測された各円の正確な中心X座標テーブル
        float[] realCentersX = new float[] {
            61.5f,   // Cell 0
            152.5f,  // Cell 1
            255.0f,  // Cell 2
            357.5f,  // Cell 3
            458.5f,  // Cell 4
            561.0f,  // Cell 5
            664.5f,  // Cell 6
            767.0f,  // Cell 7
            869.5f,  // Cell 8
            961.0f   // Cell 9
        };

        using (Bitmap srcBmp = new Bitmap(srcPath)) {
            int srcW = srcBmp.Width;
            int srcH = srcBmp.Height;

            int cellW = 110;
            int cellH = 110;
            int numCols = 5;
            int numRows = 2;

            int destW = cellW * numCols;  // 550px
            int destH = cellH * numRows;  // 220px

            // 各円の代表色（文字を避けた円上部の背景色）を自動取得
            Color[] cellBgColors = new Color[10];
            for (int i = 0; i < 10; i++) {
                int cx = (int)realCentersX[i];
                int cy = 511 - 25; // 中心から上に25pxずらした「確実に文字がない背景領域」
                cellBgColors[i] = srcBmp.GetPixel(cx, cy);
                Console.WriteLine("Cell " + i + " background representative color auto-detected: R=" + cellBgColors[i].R + ", G=" + cellBgColors[i].G + ", B=" + cellBgColors[i].B);
            }

            using (Bitmap destBmp = new Bitmap(destW, destH, PixelFormat.Format32bppArgb)) {
                // 背景を白で完全に初期化
                using (Graphics g = Graphics.FromImage(destBmp)) {
                    g.Clear(Color.White);
                }

                for (int i = 0; i < 10; i++) {
                    float srcCx = realCentersX[i];
                    float srcCy = 511.5f;

                    int col = i % 5;
                    int row = i / 5; // 0: 上段, 1: 下段

                    float destCx = col * cellW + cellW / 2.0f;
                    float destCy = row * cellH + cellH / 2.0f;

                    Color myBgColor = cellBgColors[i];

                    // 切り出し処理範囲を少し広めに取る (106x106)
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

                            // 円の正確な中心からの距離
                            float dist = (float)Math.Sqrt(dx * dx + dy * dy);

                            Color srcColor = srcBmp.GetPixel(sx, sy);

                            // =======================================================
                            // 【超知能的・隣接円ノイズ除去色フィルタ】
                            // 円の端側（中心から距離40px以上、かつ左右幅32px以上）において、
                            // サンプリングした色が「自分自身の背景円の代表色」と大きく異なる場合、
                            // それは隣の円からのはみ出しノイズと判定し、完璧に白（Color.White）に置き換えます。
                            // =======================================================
                            bool isBleedNoise = false;
                            if (dist >= 38.0f && Math.Abs(dx) >= 32.0f) {
                                // 色距離を算出 (RGB Euclidean Distance)
                                float colorDist = (float)Math.Sqrt(
                                    (srcColor.R - myBgColor.R) * (srcColor.R - myBgColor.R) +
                                    (srcColor.G - myBgColor.G) * (srcColor.G - myBgColor.G) +
                                    (srcColor.B - myBgColor.B) * (srcColor.B - myBgColor.B)
                                );

                                // 自分自身の背景色とも、白（背景）とも異なる色であれば、隣のセルの色（ノイズ）であると断定！
                                // 白背景との距離も考慮（白いエッジや文字を消さないようにするため）
                                float distToWhite = (float)Math.Sqrt(
                                    (srcColor.R - 255) * (srcColor.R - 255) +
                                    (srcColor.G - 255) * (srcColor.G - 255) +
                                    (srcColor.B - 255) * (srcColor.B - 255)
                                );

                                if (colorDist > 75.0f && distToWhite > 50.0f) {
                                    isBleedNoise = true;
                                }
                            }

                            // アンチエイリアス白フェードマスク設計
                            float innerRadius = 46.0f;
                            float outerRadius = 48.0f;

                            if (isBleedNoise) {
                                // 隣接円のはみ出しノイズ：完璧に白で消去！
                                destBmp.SetPixel(tx, ty, Color.White);
                            } else if (dist <= innerRadius) {
                                // 円の内部：元のピクセルをそのままコピー（文字を含む）
                                destBmp.SetPixel(tx, ty, srcColor);
                            } else if (dist >= outerRadius) {
                                // 円の外部：ノイズを完璧に遮断し、100%真っ白にする
                                destBmp.SetPixel(tx, ty, Color.White);
                            } else {
                                // 境界部分：ジャギーを防ぐため、元の色と白背景をブレンド
                                float t = (dist - innerRadius) / (outerRadius - innerRadius);
                                int r = (int)(srcColor.R + (255 - srcColor.R) * t);
                                int g = (int)(srcColor.G + (255 - srcColor.G) * t);
                                int b = (int)(srcColor.B + (255 - srcColor.B) * t);
                                destBmp.SetPixel(tx, ty, Color.FromArgb(255, r, g, b));
                            }
                        }
                    }
                }

                // 作成した画像を保存
                destBmp.Save(destPath, ImageFormat.Png);
                Console.WriteLine("Saved perfectly centered and cleaned 2x5 grid roulette to: " + destPath);

                // ==========================================
                // 【セルフチェック用の10倍拡大プレビュー生成】
                // セル 0 (X: 0〜110) と セル 1 (X: 110〜220) の境界線付近 (X: 90〜130, Y: 20〜70) を
                // 10倍に拡大した検証用画像を生成します。
                // ==========================================
                int cropX = 90;
                int cropY = 20;
                int cropW = 40;
                int cropH = 50;
                int scale = 10;

                using (Bitmap zoomBmp = new Bitmap(cropW * scale, cropH * scale, PixelFormat.Format32bppArgb)) {
                    using (Graphics gZoom = Graphics.FromImage(zoomBmp)) {
                        gZoom.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                        gZoom.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                        
                        gZoom.DrawImage(destBmp, 
                            new Rectangle(0, 0, zoomBmp.Width, zoomBmp.Height), 
                            new Rectangle(cropX, cropY, cropW, cropH), 
                            GraphicsUnit.Pixel);
                    }
                    zoomBmp.Save(zoomVerifyPath, ImageFormat.Png);
                    Console.WriteLine("Generated 10x Zoom verification image at: " + zoomVerifyPath);
                }
            }
        }
    }
}
