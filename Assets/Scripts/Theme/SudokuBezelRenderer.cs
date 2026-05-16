using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// UIパネルやボタンにメカニカルな枠線（ベゼル）を描画する機能を提供します。
/// 静的UI（あらかじめヒエラルキーに配置された構造）を優先的に使用します。
/// </summary>
public class SudokuBezelRenderer : MonoBehaviour
{
    public const float BEZEL_THICKNESS = 6f;
    public static void ApplyBezel(GameObject target, SudokuData.SudokuTheme theme) {
        if (target == null) return;
        float thickness = theme.bevelWidth > 0 ? theme.bevelWidth : BEZEL_THICKNESS;
        ApplyBezel(target, theme.highlightColor, theme.shadowColor, thickness);
    }

    public static void ApplyBezel(GameObject go, Color light, Color dark, float thickness) {
        if (go == null) return;

        Transform holderT = go.transform.Find("_Bezel_");
        if (holderT == null) return;

        CanvasGroup cg = holderT.GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = 1f;

        // 枠線パーツの色更新のみを行う（サイズはアンカーに任せる）
        UpdateBevelPart(holderT.gameObject, "_B_T", light);
        UpdateBevelPart(holderT.gameObject, "_B_B", dark);
        UpdateBevelPart(holderT.gameObject, "_B_L", light);
        UpdateBevelPart(holderT.gameObject, "_B_R", dark);
    }

    private static void UpdateBevelPart(GameObject holder, string name, Color color) {
        Transform t = holder.transform.Find(name);
        if (t == null) {
            Transform bezelHolder = holder.transform.Find("_Bezel_");
            if (bezelHolder != null) t = bezelHolder.Find(name);
        }

        if (t == null) {
            // Debug.Log($"[Bezel-Trace] Part '{name}' not found in {holder.name}");
            return;
        }

        var img = t.GetComponent<Image>();
        if (img != null) {
            img.color = color;
            img.raycastTarget = false;
        }
    }

    public static void CleanUpBezel(GameObject go) {
        if (go == null) return;
        Transform holder = go.transform.Find("_Bezel_");
        if (holder != null) {
            CanvasGroup cg = holder.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 0f;
        }
    }
}
