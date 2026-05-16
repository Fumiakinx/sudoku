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

        // テーマから最新の色を取得
        if (SudokuThemeManager.Instance != null) {
            var theme = SudokuThemeManager.Instance.CurrentTheme;
            textElement.color = isCorrect ? theme.correctMarkColor : theme.errorMarkColor;
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
}
