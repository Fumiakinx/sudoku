using UnityEngine;
using TMPro;
using System.Collections;
using System;

public class SudokuFeedbackOverlay : MonoBehaviour {
    public static SudokuFeedbackOverlay Instance;

    [Header("UI References")]
    public TextMeshProUGUI bigCircle;
    public TextMeshProUGUI bigCross;
    public GameObject overlayRoot;
    public float displayDuration = 1.0f;

    private void Awake() {
        Instance = this;
        
        // 参照がNULLの場合、名前ベースで自動修復を試みる
        if (overlayRoot == null) {
            var trans = transform.Find("AnimationContainer");
            if (trans == null) trans = transform.Find("ResultOverlay"); // 旧名対応
            if (trans != null) overlayRoot = trans.gameObject;
        }
        
        if (bigCircle == null && overlayRoot != null) {
            bigCircle = overlayRoot.GetComponentInChildren<TextMeshProUGUI>(true);
            // 名前に "Circle" を含むものを優先
            foreach (var t in overlayRoot.GetComponentsInChildren<TextMeshProUGUI>(true)) {
                if (t.name.Contains("Circle")) { bigCircle = t; break; }
            }
        }
        
        if (bigCross == null && overlayRoot != null) {
            // 名前に "Cross" または "X" を含むものを優先
            foreach (var t in overlayRoot.GetComponentsInChildren<TextMeshProUGUI>(true)) {
                if (t.name.Contains("Cross") || t.name.Contains("X")) { bigCross = t; break; }
            }
        }

        if (overlayRoot) overlayRoot.SetActive(false);
    }

    public void ShowCorrect(Action onComplete = null) {
        if (bigCircle == null) return;
        if (bigCross != null) bigCross.gameObject.SetActive(false);
        StopAllCoroutines();
        StartCoroutine(ShowRoutine(bigCircle, true, onComplete));
    }

    public void ShowMistake(Action onComplete = null) {
        if (bigCross == null) return;
        if (bigCircle != null) bigCircle.gameObject.SetActive(false);
        StopAllCoroutines();
        StartCoroutine(ShowRoutine(bigCross, false, onComplete));
    }

    private IEnumerator ShowRoutine(TextMeshProUGUI textElement, bool isCorrect, Action onComplete) {
        if (textElement == null || overlayRoot == null) {
            onComplete?.Invoke();
            yield break;
        }

        // テーマから最新の色を取得し、背景色とのコントラスト・色差を自動調整
        if (SudokuThemeManager.Instance != null) {
            var theme = SudokuThemeManager.Instance.CurrentTheme;
            Color baseColor = isCorrect ? theme.correctMarkColor : theme.errorMarkColor;
            textElement.color = GetVisibleMarkColor(baseColor, theme.backgroundColor, isCorrect);
        }
        
        overlayRoot.SetActive(true);
        textElement.gameObject.SetActive(true);
        
        // レイアウトを静的に即時確定させる
        Canvas.ForceUpdateCanvases();
        
        yield return new WaitForSeconds(displayDuration);
        
        textElement.gameObject.SetActive(false);
        overlayRoot.SetActive(false);
        
        onComplete?.Invoke();
    }

    /// <summary>
    /// 背景色に対して、対象の色（丸・バツ）が十分に視認できるかチェックし、
    /// 見えにくい場合は背景色とコントラストの高い色（白、黄色、暗い赤/緑など）に調整して返します。
    /// </summary>
    public static Color GetVisibleMarkColor(Color baseMarkColor, Color backgroundColor, bool isCorrect) {
        // 1. 輝度の算出 (ITU-R BT.709 に基づく人間が知覚する明るさ)
        float lBg = 0.2126f * backgroundColor.r + 0.7152f * backgroundColor.g + 0.0722f * backgroundColor.b;
        float lMark = 0.2126f * baseMarkColor.r + 0.7152f * baseMarkColor.g + 0.0722f * baseMarkColor.b;
        
        // コントラスト比の算出
        float l1 = lBg + 0.05f;
        float l2 = lMark + 0.05f;
        float contrast = l1 > l2 ? l1 / l2 : l2 / l1;

        // 2. RGB空間での単純な色距離の算出 (色相の同化を防ぐため)
        float rDiff = backgroundColor.r - baseMarkColor.r;
        float gDiff = backgroundColor.g - baseMarkColor.g;
        float bDiff = backgroundColor.b - baseMarkColor.b;
        float colorDist = Mathf.Sqrt(rDiff * rDiff + gDiff * gDiff + bDiff * bDiff);

        // コントラスト比が 2.5 未満、または色空間での距離が 0.35 未満の場合に「見えにくい」と判定
        if (contrast < 2.5f || colorDist < 0.35f) {
            // 背景が暗い場合は明るい色、明るい場合は暗い色を選択
            if (lBg < 0.5f) {
                // 暗い背景（黒、深緑など）：正解なら白、間違いなら明るい黄色
                return isCorrect ? Color.white : Color.yellow;
            } else {
                // 明るい背景（白、薄い灰色など）：正解なら濃い緑、間違いなら濃い赤
                return isCorrect ? new Color(0.0f, 0.4f, 0.0f, 1f) : new Color(0.5f, 0.0f, 0.0f, 1f);
            }
        }

        return baseMarkColor;
    }
}
