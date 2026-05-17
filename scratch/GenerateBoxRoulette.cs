using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

public class GenerateBoxRoulette {
    public static void Main() {
        string destPath = @"Assets\Textures\Life\Life_roulette_Box.png";
        
        // 保存先のディレクトリを確保
        string dir = Path.GetDirectoryName(destPath);
        if (!Directory.Exists(dir)) {
            Directory.CreateDirectory(dir);
        }

        Console.WriteLine("Generating premium procedural box-shaped roulette sprites...");

        int cellW = 110;
        int cellH = 110;
        int numCols = 5;
        int numRows = 2;

        int destW = cellW * numCols;  // 550px
        int destH = cellH * numRows;  // 220px

        // 人生ゲームのルーレットを元にした、0(10番)から9までの完璧なカラーパレット
        Color[] baseColors = new Color[] {
            Color.FromArgb(142, 196, 31),  // 0 (黄緑 - 元の10番)
            Color.FromArgb(255, 222, 0),   // 1 (黄色)
            Color.FromArgb(253, 180, 0),   // 2 (黄橙)
            Color.FromArgb(245, 112, 0),   // 3 (オレンジ)
            Color.FromArgb(228, 10, 26),    // 4 (赤)
            Color.FromArgb(231, 26, 128),   // 5 (ピンク / マゼンタ)
            Color.FromArgb(136, 68, 152),   // 6 (紫)
            Color.FromArgb(10, 113, 187),   // 7 (紺)
            Color.FromArgb(0, 172, 236),    // 8 (水色)
            Color.FromArgb(0, 148, 64)      // 9 (緑)
        };

        using (Bitmap destBmp = new Bitmap(destW, destH, PixelFormat.Format32bppArgb)) {
            using (Graphics g = Graphics.FromImage(destBmp)) {
                // 最高品質の描画セッティング（アンチエイリアシングを有効化）
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                // 背景は白
                g.Clear(Color.White);

                for (int i = 0; i < 10; i++) {
                    int col = i % 5;
                    int row = i / 5;

                    // セルの左上座標
                    int cellX = col * cellW;
                    int cellY = row * cellH;

                    // 角丸四角形の配置領域（92x92px にして上品な余白を確保）
                    float boxSize = 92f;
                    float boxX = cellX + (cellW - boxSize) / 2.0f;
                    float boxY = cellY + (cellH - boxSize) / 2.0f;
                    RectangleF rect = new RectangleF(boxX, boxY, boxSize, boxSize);

                    Color mainColor = baseColors[i];

                    // わずかに立体感を出すため、グラデーションカラー（上部を少し明るく、下部を少し暗く）を設計
                    Color colorLight = BlendColor(mainColor, Color.White, 0.15f);
                    Color colorDark = BlendColor(mainColor, Color.Black, 0.10f);

                    // 角丸パスの生成
                    using (GraphicsPath path = new GraphicsPath()) {
                        float radius = 20f; // 角丸の半径
                        AddRoundedRectangle(path, rect, radius);

                        // グラデーションで角丸四角形を塗りつぶし
                        using (LinearGradientBrush gradBrush = new LinearGradientBrush(
                            new PointF(boxX, boxY), 
                            new PointF(boxX, boxY + boxSize), 
                            colorLight, 
                            colorDark)) {
                            g.FillPath(gradBrush, path);
                        }

                        // 半透明の薄い黒で、高級感のあるシャープな輪郭線を描く
                        using (Pen outlinePen = new Pen(Color.FromArgb(60, 0, 0, 0), 1.5f)) {
                            g.DrawPath(outlinePen, path);
                        }
                    }

                    // 数字フォントの選定
                    string fontName = "Impact";
                    using (Font testFont = new Font(fontName, 10)) {
                        if (testFont.Name != fontName) {
                            fontName = "Arial Black"; // フォールバック
                        }
                    }

                    // 文字サイズ 54pt で極太の数字を描画
                    float fontSize = 54f;
                    using (Font font = new Font(fontName, fontSize, FontStyle.Bold)) {
                        string numStr = i.ToString();

                        // 文字パスを作成し、太い黒枠と白塗りつぶしを行う（人生ゲーム文字スタイル）
                        using (GraphicsPath textPath = new GraphicsPath()) {
                            // 縦横中央揃えのStringFormat
                            using (StringFormat sf = new StringFormat()) {
                                sf.Alignment = StringAlignment.Center;
                                sf.LineAlignment = StringAlignment.Center;

                                // 文字をパスに追加 (中央座標を基準とする)
                                textPath.AddString(
                                    numStr, 
                                    font.FontFamily, 
                                    (int)font.Style, 
                                    g.DpiY * fontSize / 72.0f, 
                                    new RectangleF(cellX, cellY + 3.0f, cellW, cellH), // 微調整で少し下に
                                    sf
                                );
                            }

                            // 1. 太い黒いフチを描く (太さ 7.5px)
                            using (Pen strokePen = new Pen(Color.FromArgb(255, 30, 30, 30), 7.5f)) {
                                strokePen.LineJoin = LineJoin.Round;
                                g.DrawPath(strokePen, textPath);
                            }

                            // 2. 内側を純白で塗りつぶす
                            using (Brush fillBrush = new SolidBrush(Color.White)) {
                                g.FillPath(fillBrush, textPath);
                            }
                        }
                    }
                }
            }

            destBmp.Save(destPath, ImageFormat.Png);
            Console.WriteLine("Successfully created premium box roulette spritesheet at: " + destPath);
        }
    }

    // 角丸四角形パスを生成するヘルパーメソッド
    private static void AddRoundedRectangle(GraphicsPath path, RectangleF bounds, float radius) {
        float diameter = radius * 2.0f;
        if (diameter > bounds.Width) diameter = bounds.Width;
        if (diameter > bounds.Height) diameter = bounds.Height;

        RectangleF arc = new RectangleF(bounds.Location, new SizeF(diameter, diameter));

        // 左上
        path.AddArc(arc, 180, 90);
        // 右上
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);
        // 右下
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        // 左下
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);

        path.CloseFigure();
    }

    // 色をブレンドするヘルパーメソッド (グラデーションカラー生成用)
    private static Color BlendColor(Color color1, Color color2, float ratio) {
        int r = (int)(color1.R * (1.0f - ratio) + color2.R * ratio);
        int g = (int)(color1.G * (1.0f - ratio) + color2.G * ratio);
        int b = (int)(color1.B * (1.0f - ratio) + color2.B * ratio);
        return Color.FromArgb(255, Math.Max(0, Math.Min(255, r)), Math.Max(0, Math.Min(255, g)), Math.Max(0, Math.Min(255, b)));
    }
}
