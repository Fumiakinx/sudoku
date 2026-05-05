using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class FlipFlapSlicer {
//    [MenuItem("Sudoku/Slice Flip Flap Sheet")]
    public static void Run() {
        string path = "Assets/Sprites/Numbers/FlipFlapSheet.png";
        TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) {
            Debug.LogError("FlipFlapSheet.png not found at " + path);
            return;
        }

        ti.isReadable = true;
        ti.spriteImportMode = SpriteImportMode.Multiple;
        
        List<SpriteMetaData> metas = new List<SpriteMetaData>();
        // The sheet is 3x4. 
        // We'll assume the generated image is large enough. 
        // Usually, generate_image returns 1024x1024 or similar.
        // Let's detect the actual texture size.
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex == null) return;
        
        int cols = 3;
        int rows = 4;
        int cellW = 110;
        int cellH = 110;

        for (int y = 0; y < rows; y++) {
            for (int x = 0; x < cols; x++) {
                int digitIndex = y * cols + x;
                if (digitIndex > 10) break; // We only need 0-9 and Blank

                SpriteMetaData meta = new SpriteMetaData();
                meta.rect = new Rect(x * cellW, (rows - 1 - y) * cellH, cellW, cellH);
                
                if (digitIndex < 10) meta.name = "Flip_" + digitIndex;
                else meta.name = "Flip_Blank";

                meta.alignment = (int)SpriteAlignment.Center;
                metas.Add(meta);
            }
        }
        
#pragma warning disable 0618
        ti.spritesheet = metas.ToArray();
#pragma warning restore 0618
        EditorUtility.SetDirty(ti);
        ti.SaveAndReimport();
        AssetDatabase.WriteImportSettingsIfDirty(path);
        Debug.Log("Sliced Flip Flap Sheet successfully! " + metas.Count + " sprites created.");
    }
}
