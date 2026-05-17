using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UIパネルやボタンにメカニカルな枠線（ベゼル）を描画する機能提供します。
/// 静的UI（あらかじめヒエラルキーに配置された構造）を優先的に使用します。
/// ベゼルは常に最前面のインナーベゼル（内側）として描画されます。
/// </summary>
public class SudokuBezelRenderer : MonoBehaviour
{
    public const float BEZEL_THICKNESS = 2f;

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

        // 枠線の太さと位置を動的に更新（常にインセット描画）
        UpdateBevelPart(holderT.gameObject, "_B_T", light, thickness);
        UpdateBevelPart(holderT.gameObject, "_B_B", dark, thickness);
        UpdateBevelPart(holderT.gameObject, "_B_L", light, thickness);
        UpdateBevelPart(holderT.gameObject, "_B_R", dark, thickness);
    }

    private static void UpdateBevelPart(GameObject holder, string name, Color color, float thickness) {
        Transform t = holder.transform.Find(name);
        if (t == null) {
            Transform bezelHolder = holder.transform.Find("_Bezel_");
            if (bezelHolder != null) t = bezelHolder.Find(name);
        }

        if (t == null) return;

        var rt = t.GetComponent<RectTransform>();
        if (rt != null) {
            rt.pivot = new Vector2(0.5f, 0.5f);
            
            // 内側配置 (常にインセット)
            if (name == "_B_T") { // Top
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(1, 1);
                rt.sizeDelta = new Vector2(0, thickness);
                rt.anchoredPosition = new Vector2(0, -thickness / 2f);
            } else if (name == "_B_B") { // Bottom
                rt.anchorMin = new Vector2(0, 0);
                rt.anchorMax = new Vector2(1, 0);
                rt.sizeDelta = new Vector2(0, thickness);
                rt.anchoredPosition = new Vector2(0, thickness / 2f);
            } else if (name == "_B_L") { // Left
                rt.anchorMin = new Vector2(0, 0);
                rt.anchorMax = new Vector2(0, 1);
                rt.sizeDelta = new Vector2(thickness, 0);
                rt.anchoredPosition = new Vector2(thickness / 2f, 0);
            } else if (name == "_B_R") { // Right
                rt.anchorMin = new Vector2(1, 0);
                rt.anchorMax = new Vector2(1, 1);
                rt.sizeDelta = new Vector2(thickness, 0);
                rt.anchoredPosition = new Vector2(-thickness / 2f, 0);
            }
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
