using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// UIパネルやボタンにメカニカルな枠線（ベゼル）を描画する機能を提供します。
/// 静的UI（あらかじめヒエラルキーに配置された構造）を優先的に使用します。
/// </summary>
public class SudokuBezelRenderer : MonoBehaviour
{
    public const float BEZEL_THICKNESS = 2f;
    public static void ApplyBezel(GameObject target, SudokuData.SudokuTheme theme, bool outset = false) {
        if (target == null) return;
        // outset 指定時は強制的に 4px、それ以外はテーマ設定に従う
        float thickness = outset ? 4f : (theme.bevelWidth > 0 ? theme.bevelWidth : BEZEL_THICKNESS);
        ApplyBezel(target, theme.highlightColor, theme.shadowColor, thickness, outset);
    }

    public static void ApplyBezel(GameObject go, Color light, Color dark, float thickness, bool outset = false) {
        if (go == null) return;

        Transform holderT = go.transform.Find("_Bezel_");
        if (holderT == null) return;

        CanvasGroup cg = holderT.GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = 1f;

        // 枠線の太さと位置を動的に更新
        UpdateBevelPart(holderT.gameObject, "_B_T", light, thickness, outset);
        UpdateBevelPart(holderT.gameObject, "_B_B", dark, thickness, outset);
        UpdateBevelPart(holderT.gameObject, "_B_L", light, thickness, outset);
        UpdateBevelPart(holderT.gameObject, "_B_R", dark, thickness, outset);
    }

    private static void UpdateBevelPart(GameObject holder, string name, Color color, float thickness, bool outset) {
        Transform t = holder.transform.Find(name);
        if (t == null) {
            Transform bezelHolder = holder.transform.Find("_Bezel_");
            if (bezelHolder != null) t = bezelHolder.Find(name);
        }

        if (t == null) return;

        var rt = t.GetComponent<RectTransform>();
        if (rt != null) {
            rt.pivot = new Vector2(0.5f, 0.5f);
            
            if (outset) {
                // 外側配置: 座標を外にずらし、角を覆うために長さも厚み分伸ばす
                if (name == "_B_T") { // Top
                    rt.anchorMin = new Vector2(0, 1);
                    rt.anchorMax = new Vector2(1, 1);
                    rt.sizeDelta = new Vector2(thickness * 2f, thickness);
                    rt.anchoredPosition = new Vector2(0, thickness / 2f);
                } else if (name == "_B_B") { // Bottom
                    rt.anchorMin = new Vector2(0, 0);
                    rt.anchorMax = new Vector2(1, 0);
                    rt.sizeDelta = new Vector2(thickness * 2f, thickness);
                    rt.anchoredPosition = new Vector2(0, -thickness / 2f);
                } else if (name == "_B_L") { // Left
                    rt.anchorMin = new Vector2(0, 0);
                    rt.anchorMax = new Vector2(0, 1);
                    rt.sizeDelta = new Vector2(thickness, thickness * 2f);
                    rt.anchoredPosition = new Vector2(-thickness / 2f, 0);
                } else if (name == "_B_R") { // Right
                    rt.anchorMin = new Vector2(1, 0);
                    rt.anchorMax = new Vector2(1, 1);
                    rt.sizeDelta = new Vector2(thickness, thickness * 2f);
                    rt.anchoredPosition = new Vector2(thickness / 2f, 0);
                }
            } else {
                // 内側配置 (現状維持)
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
