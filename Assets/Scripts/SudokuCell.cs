using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SudokuCell : MonoBehaviour {
    public int Row { get; private set; }
    public int Col { get; private set; }
    public int Value { get; set; }
    public bool IsFixed { get; private set; }
    public bool IsError { get; set; }
    
    private Image bg;
    private Image digitImage;
    private TMPro.TextMeshProUGUI crossText;
    private RectTransform rectTransform;
    private NixieDigit nixieCache;
    private Led7SegDigit ledCache;

    public void Init(int r, int c, int val, bool fixedVal, SudokuBoard b) {
        Row = r;
        Col = c;
        Value = val;
        IsFixed = fixedVal;
        IsError = false;
        bg = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        RefreshUI();
    }

    public void SetValue(int val, bool immediate = true, bool fixedVal = false) {
        Value = val;
        IsFixed = fixedVal;
        RefreshUI(immediate);
    }

    public void SetHighlight(bool isSelected, bool isSameDigit, bool isError, bool isRelated) {
        if (bg == null) bg = GetComponent<Image>();
        if (bg == null) return;

        var styler = SudokuUIStyler.Instance;
        if (styler == null) return;
        var theme = styler.CurrentTheme;

        // 背景色の決定（本体の色を維持しつつ、ハイライトはオーバーレイで重ねる）
        if (isError) {
            bg.color = theme.errorColor; 
        } else if (isSameDigit) {
            bg.color = theme.sameDigitColor;
        } else {
            bg.color = theme.cellColorNormal; // 本体は常にテーマの基本色
        }

        // 背景ハイライト（オーバーレイ方式：力技禁止・共通化維持）
        styler.ApplyCellBackgroundHighlight(gameObject, isSelected, isRelated);

        styler.ApplyBezel(gameObject, theme, isSelected, styler.bevelThickness);
        styler.ApplySelectionOutline(gameObject, isSelected);
        styler.ApplyRelatedHighlight(gameObject, isRelated && !isSelected);
    }


    public void RefreshUI(bool immediate = true) {
        var styler = SudokuUIStyler.Instance;
        if (styler == null) return;

        var theme = styler.CurrentTheme;
        if (string.IsNullOrEmpty(theme.themeName)) return;

        if (bg == null) bg = GetComponent<Image>();
        if (bg != null) bg.color = theme.cellColorNormal;

        // 唯一のDigitImageを確保（他は削除）
        Transform digitTransform = null;
        var children = new System.Collections.Generic.List<Transform>();
        foreach (Transform t in transform) children.Add(t);
        
        foreach (var t in children) {
            if (t.name == "DigitImage") {
                if (digitTransform == null) digitTransform = t;
                else DestroyImmediate(t.gameObject);
            }
        }

        if (digitTransform == null) {
            var go = new GameObject("DigitImage", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            digitTransform = go.transform;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.one * 5; rt.offsetMax = -Vector2.one * 5;
        }

        styler.ApplyDigitVisual(digitTransform.gameObject, Value, theme, immediate, true);

        // バツ印の表示制御
        UpdateCrossMark();

        styler.ApplyBezel(gameObject, theme, false, styler.bevelThickness);
        styler.ApplySelectionOutline(gameObject, false);
        styler.ApplyRelatedHighlight(gameObject, false);
    }

    private void UpdateCrossMark() {
        if (crossText == null) {
            var t = transform.Find("CrossMark");
            if (t != null) {
                crossText = t.GetComponent<TMPro.TextMeshProUGUI>();
            } else {
                var go = new GameObject("CrossMark", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
                go.transform.SetParent(transform, false);
                crossText = go.GetComponent<TMPro.TextMeshProUGUI>();
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                
                crossText.text = "×";
                crossText.alignment = TMPro.TextAlignmentOptions.Center;
                crossText.enableAutoSizing = true;
                crossText.fontSizeMin = 10;
                crossText.fontSizeMax = 100;
                crossText.margin = Vector4.zero;
                crossText.color = new Color(1, 0, 0, 0.8f); 
                crossText.fontStyle = TMPro.FontStyles.Bold;
                crossText.raycastTarget = false;
            }
        }
        
        if (crossText != null) {
            // テーマに基づいてエラー色を調整（基本は赤だが、視認性のためにStylerから取得検討）
            Color errorColor = new Color(1, 0, 0, 0.9f); // デフォルト
            if (SudokuUIStyler.Instance != null) {
                // Stylerのハイライトカラーなどを参考に、より目立つ赤を設定可能
                // ここでは一旦視認性を高めた赤に固定し、必要に応じてStyler側で管理する
                errorColor.a = 0.9f; 
            }
            crossText.color = errorColor; 
            crossText.gameObject.SetActive(IsError && Value > 0);
            if (crossText.gameObject.activeSelf) {
                crossText.transform.SetAsLastSibling();
            }
        }
    }
}
