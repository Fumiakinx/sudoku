using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

public class GenerateSpacedRoulette {
    public static void Main() {
        string srcPath = @"Assets\Textures\Life\life_roulette_v9.png";
        string destPath = @"Assets\Textures\Life\life_roulette_spaced.png";

        if (!File.Exists(srcPath)) {
            Console.WriteLine("Source file not found: " + srcPath);
            return;
        }

        Console.WriteLine("Processing " + srcPath + "...");

        using (Bitmap srcBmp = new Bitmap(srcPath)) {
            int srcW = srcBmp.Width;
            int srcH = srcBmp.Height;

            // 新しいキャンバスの設定
            // 1セルあたり幅 150px、高さ 150px
            int destCellW = 150;
            int destCellH = 150;
            int numCells = 10;

            int destW = destCellW * numCells;
            int destH = destCellH;

            using (Bitmap destBmp = new Bitmap(destW, destH, PixelFormat.Format32bppArgb)) {
                // 背景を完全透明で初期化
                using (Graphics g = Graphics.FromImage(destBmp)) {
                    g.Clear(Color.Transparent);
                }

                float cellW = srcW / (float)numCells;

                // 各セルを切り出して新しい画像の中央に配置
                for (int i = 0; i < numCells; i++) {
                    // 元画像内の切り出し中心 (Y=511.5)
                    float srcCx = i * cellW + cellW / 2.0f;
                    float srcCy = 511.5f;

                    // 新しい画像内での配置中心
                    float destCx = i * destCellW + destCellW / 2.0f;
                    float destCy = destCellH / 2.0f;

                    // 元のマルの大きさ（半径約48.5pxまでを高品質アンチエイリアスマスクで切り出し）
                    int scanSize = 110; // 110x110の領域を処理

                    for (int dy = -scanSize / 2; dy <= scanSize / 2; dy++) {
                        for (int dx = -scanSize / 2; dx <= scanSize / 2; dx++) {
                            // 元画像のピクセル座標
                            int sx = (int)Math.Round(srcCx + dx);
                            int sy = (int)Math.Round(srcCy + dy);

                            if (sx < 0 || sx >= srcW || sy < 0 || sy >= srcH)
                                continue;

                            // 円の中心からの距離
                            float dist = (float)Math.Sqrt(dx * dx + dy * dy);

                            // 新しい画像のピクセル座標
                            int tx = (int)Math.Round(destCx + dx);
                            int ty = (int)Math.Round(destCy + dy);

                            if (tx < 0 || tx >= destW || ty < 0 || ty >= destH)
                                continue;

                            Color srcColor = srcBmp.GetPixel(sx, sy);

                            // 円のフチのアンチエイリアス処理（半径47.5px〜49.5pxの間でフェードアウト）
                            float innerRadius = 47.0f;
                            float outerRadius = 49.5f;

                            if (dist <= innerRadius) {
                                // 円の内側（文字も含む）：そのままコピー
                                destBmp.SetPixel(tx, ty, srcColor);
                            } else if (dist >= outerRadius) {
                                // 円の外側：完全透明
                                destBmp.SetPixel(tx, ty, Color.Transparent);
                            } else {
                                // 円のフチ：アルファ値でフェードアウトして滑らかな境界にする
                                float t = (dist - innerRadius) / (outerRadius - innerRadius);
                                int alpha = (int)((1.0f - t) * srcColor.A);
                                destBmp.SetPixel(tx, ty, Color.FromArgb(alpha, srcColor.R, srcColor.G, srcColor.B));
                            }
                        }
                    }
                }

                destBmp.Save(destPath, ImageFormat.Png);
                Console.WriteLine("Saved spaced roulette to: " + destPath);
            }
        }
    }
}
