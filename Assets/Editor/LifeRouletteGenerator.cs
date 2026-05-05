using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using UnityEditor.U2D.Sprites;

public class LifeRouletteGenerator : EditorWindow
{
    [MenuItem("Tools/Generate Life Roulette Texture")]
    public static void Generate()
    {
        string dstPath = "Assets/Textures/life_roulette_v28_procedural.png";
        int cellSize = 128;
        int cols = 4;
        int rows = 3;
        int w = cellSize * cols;
        int h = cellSize * rows;

        Color[] slotColors = new Color[] {
            new Color(1.0f, 0.35f, 0.35f), // 1: Red
            new Color(1.0f, 0.65f, 0.1f), // 2: Orange
            new Color(1.0f, 0.95f, 0.2f), // 3: Yellow
            new Color(0.35f, 0.85f, 0.35f), // 4: Green
            new Color(0.2f, 0.8f, 1.0f), // 5: Light Blue
            new Color(0.35f, 0.45f, 1.0f), // 6: Blue
            new Color(0.75f, 0.35f, 1.0f), // 7: Purple
            new Color(1.0f, 0.45f, 0.85f), // 8: Pink
            new Color(0.1f, 0.85f, 0.85f), // 9: Teal
            new Color(0.2f, 0.65f, 0.2f), // 10 (X): Dark Green
            new Color(0.7f, 0.7f, 0.75f), // 11 (0): Grey
        };

        int[] digits = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 0 };

        Texture2D result = new Texture2D(w, h, TextureFormat.ARGB32, false);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                result.SetPixel(x, y, Color.white);

        for (int i = 0; i < 11; i++)
        {
            int row = i / cols;
            int col = i % cols;
            float centerX = col * cellSize + cellSize / 2f;
            float centerY = h - (row * cellSize + cellSize / 2f);
            float radius = (cellSize / 2f) * 0.9f;
            Color c = slotColors[i];

            for (int py = row * cellSize; py < (row + 1) * cellSize; py++)
            {
                for (int px = col * cellSize; px < (col + 1) * cellSize; px++)
                {
                    float dx = px - centerX;
                    float dy = (h - 1 - py) - (centerY - 1);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist < radius)
                    {
                        float alpha = Mathf.Clamp01(radius - dist);
                        Color bg = result.GetPixel(px, h - 1 - py);
                        result.SetPixel(px, h - 1 - py, Color.Lerp(bg, c, alpha));
                    }
                }
            }
            DrawDigitFinal(result, digits[i], col, row, cellSize, h);
        }

        result.Apply();
        byte[] bytes = result.EncodeToPNG();
        File.WriteAllBytes(dstPath, bytes);
        AssetDatabase.ImportAsset(dstPath);

        // --- ISpriteEditorDataProvider を使用したスライス設定 (修正版) ---
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var dataProvider = factory.GetSpriteEditorDataProviderFromObject(AssetImporter.GetAtPath(dstPath));
        dataProvider.InitSpriteEditorDataProvider();

        var spriteRects = new List<SpriteRect>();
        for (int i = 0; i < 11; i++)
        {
            int r = i / cols;
            int c = i % cols;
            spriteRects.Add(new SpriteRect
            {
                name = "life_roulette_v28_procedural_" + i,
                rect = new Rect(c * cellSize, (rows - 1 - r) * cellSize, cellSize, cellSize),
                alignment = SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f)
            });
        }

        // 直接 SetSpriteRects を呼び出す
        dataProvider.SetSpriteRects(spriteRects.ToArray());
        dataProvider.Apply();

        TextureImporter importer = dataProvider.targetObject as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.isReadable = true;
            importer.SaveAndReimport();
        }

        var data = AssetDatabase.LoadAssetAtPath<SudokuData>("Assets/Data/SudokuData.asset");
        if (data != null)
        {
            for (int i = 0; i < data.themes.Length; i++)
            {
                if (data.themes[i].themeName.Contains("Life Roulette"))
                {
                    var sprites = AssetDatabase.LoadAllAssetsAtPath(dstPath);
                    var spriteList = new List<Sprite>();
                    foreach (var obj in sprites) if (obj is Sprite s) spriteList.Add(s);
                    spriteList.Sort((a, b) => string.Compare(a.name, b.name));
                    data.themes[i].sprites = spriteList.ToArray();
                    EditorUtility.SetDirty(data);
                    AssetDatabase.SaveAssets();
                    break;
                }
            }
        }
        Debug.Log("Life Roulette texture REGENERATED with corrected modern API.");
    }

    private static void DrawDigitFinal(Texture2D tex, int digit, int col, int row, int cellSize, int texH)
    {
        int startY = texH - 1 - (row * cellSize + cellSize); 
        int startX = col * cellSize;
        int thick = 16; 
        int margin = 32;
        int w = cellSize - margin * 2;
        int h = cellSize - margin * 2;
        int x = startX + margin;
        int y = startY + margin;
        Color white = Color.white;

        System.Action<int, int, int, int> fill = (fx, fy, fw, fh) => {
            for (int py = fy; py < fy + fh; py++)
                for (int px = fx; px < fx + fw; px++)
                    if (px >= 0 && px < tex.width && py >= 0 && py < tex.height)
                        tex.SetPixel(px, py, white);
        };

        switch (digit)
        {
            case 1: fill(x + w / 2 - thick / 2, y, thick, h); break;
            case 2:
                fill(x, y + h - thick, w, thick);
                fill(x + w - thick, y + h / 2, thick, h / 2);
                fill(x, y + h / 2 - thick / 2, w, thick);
                fill(x, y, thick, h / 2);
                fill(x, y, w, thick);
                break;
            case 3:
                fill(x, y + h - thick, w, thick);
                fill(x + w - thick, y, thick, h);
                fill(x, y + h / 2 - thick / 2, w, thick);
                fill(x, y, w, thick);
                break;
            case 4:
                fill(x, y + h / 2, thick, h / 2);
                fill(x + w - thick, y, thick, h);
                fill(x, y + h / 2 - thick / 2, w, thick);
                break;
            case 5:
                fill(x, y + h - thick, w, thick);
                fill(x, y + h / 2, thick, h / 2);
                fill(x, y + h / 2 - thick / 2, w, thick);
                fill(x + w - thick, y, thick, h / 2);
                fill(x, y, w, thick);
                break;
            case 6:
                fill(x, y, thick, h);
                fill(x, y + h / 2 - thick / 2, w, thick);
                fill(x, y + h - thick, w, thick);
                fill(x + w - thick, y, thick, h / 2);
                fill(x, y, w, thick);
                break;
            case 7:
                fill(x, y + h - thick, w, thick);
                fill(x + w - thick, y, thick, h);
                break;
            case 8:
                fill(x, y, thick, h); fill(x + w - thick, y, thick, h);
                fill(x, y, w, thick); fill(x, y + h - thick, w, thick);
                fill(x, y + h / 2 - thick / 2, w, thick);
                break;
            case 9:
                fill(x + w - thick, y, thick, h);
                fill(x, y + h / 2, thick, h / 2);
                fill(x, y + h - thick, w, thick);
                fill(x, y + h / 2 - thick / 2, w, thick);
                fill(x, y, w, thick);
                break;
            case 10: // X
                for (int i = 0; i < h; i++) {
                    fill(x + i, y + i, thick, thick);
                    fill(x + (w - thick) - i, y + i, thick, thick);
                }
                break;
            case 0:
                fill(x, y, w, thick); fill(x, y + h - thick, w, thick);
                fill(x, y, thick, h); fill(x + w - thick, y, thick, h);
                break;
        }
    }
}
