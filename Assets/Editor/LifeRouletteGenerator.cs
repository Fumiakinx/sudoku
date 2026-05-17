using UnityEngine;
using UnityEditor;
using System.IO;

public class LifeRouletteGenerator : EditorWindow
{
    private const string SourcePath = "Assets/Textures/Life/life_roulette_v9.png";
    private const string DestPath = "Assets/Textures/Life/LifeRouletteSprites_New.png";

    [MenuItem("Tools/Generate Life Roulette Texture")]
    public static void GenerateTextureMenu()
    {
        GenerateTexture(true);
    }

    public static void GenerateTexture(bool showDialog = false)
    {
        // 1. ソーステクスチャのロードとReadable設定
        TextureImporter sourceImporter = AssetImporter.GetAtPath(SourcePath) as TextureImporter;
        if (sourceImporter == null)
        {
            if (showDialog) EditorUtility.DisplayDialog("Error", "ソーステクスチャ (life_roulette_v9.png) が見つかりません。", "OK");
            else Debug.LogError("ソーステクスチャ (life_roulette_v9.png) が見つかりません。");
            return;
        }

        bool wasReadable = sourceImporter.isReadable;
        TextureImporterCompression wasCompression = sourceImporter.textureCompression;
        TextureImporterType wasType = sourceImporter.textureType;

        if (!wasReadable || wasCompression != TextureImporterCompression.Uncompressed || wasType != TextureImporterType.Default)
        {
            sourceImporter.isReadable = true;
            sourceImporter.textureCompression = TextureImporterCompression.Uncompressed;
            sourceImporter.textureType = TextureImporterType.Default; // 一時的に通常のテクスチャとして読み込む
            sourceImporter.SaveAndReimport();
        }

        Texture2D srcTex = AssetDatabase.LoadAssetAtPath<Texture2D>(SourcePath);
        if (srcTex == null)
        {
            if (showDialog) EditorUtility.DisplayDialog("Error", "ソーステクスチャのロードに失敗しました。", "OK");
            else Debug.LogError("ソーステクスチャのロードに失敗しました。");
            return;
        }

        // 2. 新しいテクスチャの作成 (1100 x 110)
        int cellW = 110;
        int cellH = 110;
        int numCells = 10;
        Texture2D destTex = new Texture2D(cellW * numCells, cellH, TextureFormat.RGBA32, false);

        // 背景を完全に透明にクリア
        Color[] clearColors = new Color[destTex.width * destTex.height];
        for (int i = 0; i < clearColors.Length; i++) clearColors[i] = Color.clear;
        destTex.SetPixels(clearColors);

        // 3. 人生ゲームルーレットの配色定義 (0〜9)
        // 0:黄緑(10の色), 1:黄, 2:橙, 3:赤, 4:桃, 5:濃桃, 6:紫, 7:中青, 8:水色, 9:紺
        Color[] themeColors = new Color[10]
        {
            new Color(0.627f, 0.824f, 0.208f, 1f), // 0: 黄緑 (Hex: #A0D235)
            new Color(1f, 0.902f, 0f, 1f),       // 1: 黄色 (Hex: #FFE600)
            new Color(1f, 0.596f, 0f, 1f),       // 2: 橙色 (Hex: #FF9800)
            new Color(0.898f, 0.224f, 0.208f, 1f), // 3: 赤色 (Hex: #E53935)
            new Color(1f, 0.251f, 0.506f, 1f),   // 4: ピンク (Hex: #FF4081)
            new Color(0.847f, 0.106f, 0.376f, 1f), // 5: マゼンタ/濃桃 (Hex: #D81B60)
            new Color(0.557f, 0.141f, 0.667f, 1f), // 6: 紫色 (Hex: #8E24AA)
            new Color(0.082f, 0.396f, 0.753f, 1f), // 7: 青色 (Hex: #1565C0)
            new Color(0f, 0.69f, 1f, 1f),        // 8: 水色 (Hex: #00B0FF)
            new Color(0.102f, 0.137f, 0.494f, 1f)  // 9: 紺色 (Hex: #1A237E)
        };

        // 4. 各セルの描画処理
        for (int cellIdx = 0; cellIdx < numCells; cellIdx++)
        {
            Color bgColor = themeColors[cellIdx];
            int destStartX = cellIdx * cellW;

            // --- A. 角丸四角形の描画 ---
            float roundSize = 98f; // マージン6pxを引いた角丸四角形のサイズ (110 - 12 = 98)
            float radius = 22f;    // 角丸の半径
            float halfS = roundSize / 2f;

            for (int y = 0; y < cellH; y++)
            {
                for (int x = 0; x < cellW; x++)
                {
                    float dx = Mathf.Abs(x - 55f);
                    float dy = Mathf.Abs(y - 55f);

                    float alpha = 0f;
                    if (dx <= halfS && dy <= halfS)
                    {
                        float cx = halfS - radius;
                        float cy = halfS - radius;

                        if (dx > cx && dy > cy)
                        {
                            float dist = Mathf.Sqrt((dx - cx) * (dx - cx) + (dy - cy) * (dy - cy));
                            if (dist <= radius)
                            {
                                alpha = 1f;
                            }
                            else if (dist - radius < 1f)
                            {
                                alpha = 1f - (dist - radius); // アンチエイリアス
                            }
                        }
                        else
                        {
                            alpha = 1f;
                        }
                    }

                    if (alpha > 0f)
                    {
                        Color pixelColor = bgColor;
                        pixelColor.a = alpha;
                        destTex.SetPixel(destStartX + x, y, pixelColor);
                    }
                }
            }

            // --- B. 元画像から白文字を抽出してセンタリング・縮小（余白拡大）配置 ---
            // 元画像スプライト設定: x = cellIdx * 102.4, y = 400, w = 102.4, h = 224
            float srcXStart = cellIdx * 102.4f;
            float srcYStart = 400f;
            int srcW = 103; // 切り捨て考慮
            int srcH = 224;

            // 文字のバウンディングボックス検出
            // 余白が狭く境界線が白っぽいため、外側16ピクセル(境界ノイズ領域)を完全に除外して純白文字だけをスキャンする
            int safePadding = 16;
            int minX = srcW, maxX = 0;
            int minY = srcH, maxY = 0;
            bool foundChar = false;

            for (int sy = safePadding; sy < srcH - safePadding; sy++)
            {
                for (int sx = safePadding; sx < srcW - safePadding; sx++)
                {
                    Color sc = srcTex.GetPixel((int)srcXStart + sx, (int)srcYStart + sy);
                    // 白さを検出 (カラフルな背景円を完全に排除するためRGB全チャンネルが0.9以上のものだけ)
                    if (sc.r > 0.9f && sc.g > 0.9f && sc.b > 0.9f && sc.a > 0.5f)
                    {
                        if (sx < minX) minX = sx;
                        if (sx > maxX) maxX = sx;
                        if (sy < minY) minY = sy;
                        if (sy > maxY) maxY = sy;
                        foundChar = true;
                    }
                }
            }

            if (foundChar)
            {
                // 文字の中心を算出
                float charCentX = (minX + maxX) / 2f;
                float charCentY = (minY + maxY) / 2f;

                // 縮小スケール係数 (上品な余白を持たせるために文字サイズを 80% に縮小)
                float charScale = 0.80f;

                // 新しいセルに文字をバイリニア補間縮小しながらブレンド描画
                for (int y = 0; y < cellH; y++)
                {
                    for (int x = 0; x < cellW; x++)
                    {
                        // セル中心 (55, 55) からの相対座標をスケールで割って、元画像上の相対座標を逆算
                        float rx = (x - 55f) / charScale;
                        float ry = (y - 55f) / charScale;

                        // 元画像でのサンプリング位置 (文字中心からの相対座標)
                        float srcX = charCentX + rx;
                        float srcY = charCentY + ry;

                        int sx = Mathf.RoundToInt(srcX);
                        int sy = Mathf.RoundToInt(srcY);

                        // 境界チェック
                        if (sx >= safePadding && sx < srcW - safePadding && sy >= safePadding && sy < srcH - safePadding)
                        {
                            Color sc = srcTex.GetPixel((int)srcXStart + sx, (int)srcYStart + sy);
                            
                            // 文字と判定された場合 (R, G, B すべてが0.85以上の白に近いピクセル)
                            if (sc.r > 0.85f && sc.g > 0.85f && sc.b > 0.85f)
                            {
                                float charAlpha = sc.a;
                                Color current = destTex.GetPixel(destStartX + x, y);
                                
                                // アルファブレンド (白文字を上書き合成)
                                Color blended = Color.Lerp(current, Color.white, charAlpha);
                                blended.a = Mathf.Max(current.a, charAlpha); // アルファチャンネルの維持
                                destTex.SetPixel(destStartX + x, y, blended);
                            }
                        }
                    }
                }
            }
        }

        destTex.Apply();

        // 5. 新しいテクスチャのPNG保存
        byte[] bytes = destTex.EncodeToPNG();
        string fullPath = Path.Combine(Directory.GetCurrentDirectory(), DestPath);
        File.WriteAllBytes(fullPath, bytes);
        AssetDatabase.ImportAsset(DestPath);

        // 6. 元テクスチャのインポーター設定を復元
        if (!wasReadable || wasCompression != TextureImporterCompression.Uncompressed || wasType != TextureImporterType.Default)
        {
            sourceImporter.isReadable = wasReadable;
            sourceImporter.textureCompression = wasCompression;
            sourceImporter.textureType = wasType;
            sourceImporter.SaveAndReimport();
        }

        // 7. 生成された新規テクスチャのスプライトシート自動設定
        TextureImporter destImporter = AssetImporter.GetAtPath(DestPath) as TextureImporter;
        if (destImporter != null)
        {
            destImporter.textureType = TextureImporterType.Sprite;
            destImporter.spriteImportMode = SpriteImportMode.Multiple;
            destImporter.spritePixelsPerUnit = 100;
            destImporter.filterMode = FilterMode.Bilinear;
            destImporter.mipmapEnabled = false;
            destImporter.alphaIsTransparency = true;

            // 10個のスライススプライト定義を作成
            SpriteMetaData[] sheet = new SpriteMetaData[numCells];
            for (int i = 0; i < numCells; i++)
            {
                SpriteMetaData meta = new SpriteMetaData();
                meta.name = $"life_roulette_v31_{i}";
                meta.rect = new Rect(i * cellW, 0, cellW, cellH);
                meta.alignment = 0; // Center
                meta.pivot = new Vector2(0.5f, 0.5f);
                sheet[i] = meta;
            }

            destImporter.spritesheet = sheet;
            destImporter.SaveAndReimport();
        }

        if (showDialog)
        {
            EditorUtility.DisplayDialog("Success", "新しいルーレットテクスチャを生成し、スプライトシートの設定を自動完了しました！\n\n保存先: " + DestPath, "OK");
        }
        else
        {
            Debug.Log("Successfully generated roulette texture at " + DestPath);
        }
    }
}
