using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

[InitializeOnLoad]
public static class SudokuInteractionSniper
{
    private static bool hasFound = false;
    static SudokuInteractionSniper()
    {
        EditorApplication.update += Monitor;
    }

    private static void Monitor()
    {
        if (!EditorApplication.isPlaying) return;

        GameObject cellObj = GameObject.Find("Cell_0_0");
        if (cellObj == null) {
            // Debug.Log("[SNIPER] Cell_0_0 not found yet.");
            return;
        }

        Button btn = cellObj.GetComponent<Button>();
        SudokuCell script = cellObj.GetComponent<SudokuCell>();
        
        // 最初の1回だけ見つけたことを報告
        if (!hasFound) {
            Debug.Log($"[SNIPER] Found Cell_0_0! Button: {btn != null}, Script: {script != null}");
            hasFound = true;
        }

        if (btn != null) {
            if (!btn.enabled) Debug.LogError($"[SNIPER] Cell_0_0 Button DISABLED! Frame: {Time.frameCount}");
            if (!btn.interactable) Debug.LogError($"[SNIPER] Cell_0_0 Button NOT INTERACTABLE! Frame: {Time.frameCount}");
        }

        Image img = cellObj.GetComponent<Image>();
        if (img != null && !img.raycastTarget) {
            Debug.LogError($"[SNIPER] Cell_0_0 Image RaycastTarget is FALSE! Frame: {Time.frameCount}");
        }

        if (script != null && !script.enabled)
        {
            Debug.LogError($"[SNIPER] Cell_0_0 SudokuCell Script was DISABLED at runtime! Frame: {Time.frameCount}");
        }
    }
}
