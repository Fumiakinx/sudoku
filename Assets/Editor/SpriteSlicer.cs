using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class SpriteSlicer {
//    [MenuItem("Sudoku/Slice FlipFlap Sheet")]
    public static void SliceFlipFlap() {
        string path = "Assets/Sprites/Numbers/FlipFlapSheet.png";
        TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) {
            Debug.LogError("FlipFlapSheet not found at " + path);
            return;
        }

        ti.isReadable = true;
        ti.spriteImportMode = SpriteImportMode.Multiple;
        
        // Use the modern API if possible, but for simplicity let's stick to what works
        List<SpriteMetaData> metas = new List<SpriteMetaData>();
        int width = 512;
        int height = 512;
        int cols = 5;
        int rows = 2;

        for (int r = 0; r < rows; r++) {
            for (int c = 0; c < cols; c++) {
                int index = r * cols + c;
                if (index >= 10) break;

                SpriteMetaData meta = new SpriteMetaData();
                // Texture coordinates start from bottom-left
                meta.rect = new Rect(c * width, (rows - 1 - r) * height, width, height);
                meta.name = "FlipFlap_" + index;
                meta.alignment = (int)SpriteAlignment.Center;
                metas.Add(meta);
            }
        }
        
#pragma warning disable 0618
        ti.spritesheet = metas.ToArray();
#pragma warning restore 0618
        ti.SaveAndReimport();
        AssetDatabase.Refresh();
        Debug.Log("Sliced FlipFlap Sheet successfully!");
    }
}
