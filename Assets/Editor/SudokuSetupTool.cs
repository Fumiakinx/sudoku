#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class SudokuSetupTool
{
    [MenuItem("Sudoku/Apply Theme Fixes")]
    public static void ApplyThemeFixes()
    {
        // 1. SudokuData.asset の変更
        var data = AssetDatabase.LoadAssetAtPath<SudokuData>("Assets/Data/SudokuData.asset");
        if (data == null)
        {
            Debug.LogError("SudokuData.asset not found!");
            return;
        }

        if (data.themes != null && data.themes.Length > 1)
        {
            var t1 = data.themes[1];
            t1.themeName = "FlipFlap";
            data.themes[1] = t1;
            Debug.Log("Theme [1] name set to FlipFlap");
        }

        if (data.themes != null && data.themes.Length > 5)
        {
            var t5 = data.themes[5];
            t5.displayType = SudokuData.ThemeDisplayType.Roulette;
            data.themes[5] = t5;
            Debug.Log("Theme [5] displayType set to Roulette");
        }

        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();

        // 2. GameUI_Stable.prefab の変更
        string prefabPath = "Assets/Prefabs/GameUI_Stable.prefab";
        var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        if (prefabRoot == null)
        {
            Debug.LogError("GameUI_Stable.prefab not found!");
            return;
        }

        var displays = prefabRoot.GetComponentsInChildren<SudokuDigitDisplay>(true);
        int timerCount = 0;
        foreach (var d in displays)
        {
            if (d.transform.parent != null && d.transform.parent.name == "TimerBG")
            {
                var so = new SerializedObject(d);
                var prop = so.FindProperty("isTimerDigit");
                if (prop != null)
                {
                    prop.boolValue = true;
                    so.ApplyModifiedProperties();
                    timerCount++;
                }
            }
        }

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);

        Debug.Log($"Setup completed! Timer digits updated: {timerCount}");
    }
}
#endif
