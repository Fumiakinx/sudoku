using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.U2D.Sprites;

public class LifeRouletteV30Generator : EditorWindow
{
    [MenuItem("Tools/Generate Life Roulette V30")]
    public static void Generate()
    {
        string v9Path = "Assets/Textures/life_roulette_v9.png";
        string dstPath = "Assets/Textures/life_roulette_v30.png";
        
        Texture2D v9Tex = AssetDatabase.LoadAssetAtPath<Texture2D>(v9Path);
        if (v9Tex == null) return;

        string v9FullPath = Path.Combine(Application.dataPath, v9Path.Replace("Assets/", ""));
        byte[] v9Bytes = File.ReadAllBytes(v9FullPath);
        Texture2D readableV9 = new Texture2D(2, 2);
        readableV9.LoadImage(v9Bytes);
        
        // --- 修正点: ユーザー様が「ズレていない」とおっしゃっていた V29 時点の座標に完全に復元 ---
        int[] exactCenters = new int[] { 68, 167, 265, 364, 462, 561, 660, 758, 857, 956 };

        string[] jinseiHex = new string[] {
            "B8D13F", "388DCC", "F6CD4C", "E12328", "DD3355", "8C2F7A", "2C2D6D", "2A5BA1", "05A0C6", "379549"
        };
        Color[] themeColors = new Color[10];
        for (int i = 0; i < 10; i++) ColorUtility.TryParseHtmlString("#" + jinseiHex[i], out themeColors[i]);

        int cellW = 110;
        int cellH = 110;
        int totalW = cellW * 10;
        Texture2D tex = new Texture2D(totalW, cellH, TextureFormat.RGBA32, false);
        
        for (int y = 0; y < cellH; y++)
            for (int x = 0; x < totalW; x++)
                tex.SetPixel(x, y, Color.white);

        for (int i = 0; i < 10; i++)
        {
            int cxSource = exactCenters[i];
            int cxTarget = i * cellW + cellW / 2;
            int cyTarget = cellH / 2;

            int size = 94;
            int r = 20;
            for (int ry = cyTarget - size/2; ry <= cyTarget + size/2; ry++) {
                for (int rx = cxTarget - size/2; rx <= cxTarget + size/2; rx++) {
                    float dx = Mathf.Abs(rx - cxTarget) - (size/2f - r);
                    float dy = Mathf.Abs(ry - cyTarget) - (size/2f - r);
                    if (Mathf.Sqrt(Mathf.Max(dx, 0) * Mathf.Max(dx, 0) + Mathf.Max(dy, 0) * Mathf.Max(dy, 0)) <= r)
                        tex.SetPixel(rx, ry, themeColors[i]);
                }
            }

            // --- 修正点: 抽出範囲を制限することで、四隅の白い三角だけを狙い撃ちで消去（塗りつぶし） ---
            int extractRange = 25; 
            for (int dy = -extractRange; dy < extractRange; dy++)
            {
                for (int dx = -extractRange; dx < extractRange; dx++)
                {
                    int sx = cxSource + dx;
                    int sy = readableV9.height / 2 + dy;
                    if (sx < 0 || sx >= readableV9.width || sy < 0 || sy >= readableV9.height) continue;

                    Color p = readableV9.GetPixel(sx, sy);
                    if (p.r > 0.92f && p.g > 0.92f && p.b > 0.92f)
                    {
                        tex.SetPixel(cxTarget + dx, cyTarget + dy, Color.white);
                    }
                }
            }
        }
        tex.Apply();

        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(dstPath, bytes);
        AssetDatabase.ImportAsset(dstPath);

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var dataProvider = factory.GetSpriteEditorDataProviderFromObject(AssetImporter.GetAtPath(dstPath));
        dataProvider.InitSpriteEditorDataProvider();

        var spriteRects = new List<SpriteRect>();
        for (int i = 0; i < 10; i++)
        {
            spriteRects.Add(new SpriteRect
            {
                name = "life_roulette_v30_" + i,
                rect = new Rect(i * cellW, 0, cellW, cellH),
                alignment = SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f)
            });
        }
        dataProvider.SetSpriteRects(spriteRects.ToArray());
        dataProvider.Apply();

        TextureImporter importer = dataProvider.targetObject as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.filterMode = FilterMode.Bilinear;
            importer.isReadable = true;
            importer.SaveAndReimport();
        }

        var data = AssetDatabase.LoadAssetAtPath<SudokuData>("Assets/Data/SudokuData.asset");
        if (data != null)
        {
            for (int i = 0; i < data.themes.Length; i++)
            {
                if (data.themes[i].themeName.ToLower().Contains("roulette"))
                {
                    var allAssets = AssetDatabase.LoadAllAssetsAtPath(dstPath);
                    var spriteList = new List<Sprite>();
                    foreach (var obj in allAssets) if (obj is Sprite s) spriteList.Add(s);
                    spriteList.Sort((a, b) => string.Compare(a.name, b.name));
                    data.themes[i].sprites = spriteList.ToArray();
                    data.themes[i].topSprites = spriteList.ToArray();
                    EditorUtility.SetDirty(data);
                    AssetDatabase.SaveAssets();
                    break;
                }
            }
        }
    }
}
