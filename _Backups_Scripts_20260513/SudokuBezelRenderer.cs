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

    /// <summary>
    /// 指定されたオブジェクトに現在のテーマに基づいた枠線を適用します。
    /// </summary>
    public static void ApplyBezel(GameObject target, SudokuData.SudokuTheme theme, float width, float height) {
        if (target == null) return;
        
        // テーマに bezelSprite が存在しないため、色ベースの描画オーバーロードを呼び出します。
        // theme.highlightColor と theme.shadowColor を使用してメカニカルな立体感を出します。
        float thickness = theme.bevelWidth > 0 ? theme.bevelWidth : BEZEL_THICKNESS;
        ApplyBezel(target, theme.highlightColor, theme.shadowColor, width, height, thickness);
    }

    /// <summary>
    /// 色と太さを直接指定して枠線を適用します。
    /// 既存の _Bezel_ オブジェクトを再利用し、実行時の破壊・生成を行いません。
    /// </summary>
    public static void ApplyBezel(GameObject go, Color light, Color dark, float w, float h, float thickness) {
        if (go == null) return;

        // 枠線ホルダー（子オブジェクト）を取得（なければ作成するが、既存のものを優先）
        Transform holderT = go.transform.Find("_Bezel_");
        if (holderT == null) {
            GameObject holder = new GameObject("_Bezel_", typeof(RectTransform), typeof(CanvasGroup));
            holderT = holder.transform;
            holderT.SetParent(go.transform, false);
        }

        // 常に最前面（LastSibling）に持ってくることで、背景に隠れるのを防ぐ
        holderT.SetAsLastSibling();

        // 表示状態を確保
        CanvasGroup cg = holderT.GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = 1f;

        // 枠線パーツの更新
        UpdateBevelPart(holderT.gameObject, "_B_T", light, new Vector2(0.5f, 1f), new Vector2(w, thickness));
        UpdateBevelPart(holderT.gameObject, "_B_B", dark,  new Vector2(0.5f, 0f), new Vector2(w, thickness));
        UpdateBevelPart(holderT.gameObject, "_B_L", light, new Vector2(0f, 0.5f), new Vector2(thickness, h));
        UpdateBevelPart(holderT.gameObject, "_B_R", dark,  new Vector2(1f, 0.5f), new Vector2(thickness, h));
    }

    private static void UpdateBevelPart(GameObject holder, string name, Color color, Vector2 anchor, Vector2 size) {
        Transform t = holder.transform.Find(name);
        if (t == null) {
            GameObject part = new GameObject(name, typeof(RectTransform), typeof(Image));
            t = part.transform;
            t.SetParent(holder.transform, false);
        }

        var rt = t.GetComponent<RectTransform>();
        if (rt != null) {
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
        }
        
        var img = t.GetComponent<Image>();
        if (img != null) {
            img.color = color;
            img.raycastTarget = false; // 枠線がクリックを邪魔しないように
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
