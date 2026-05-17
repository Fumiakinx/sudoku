using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// スドクの各セルを制御するクラス。
/// 静的UIの原則に基づき、コンポーネントの紐付けやイベント登録をインスペクターで行います。
/// </summary>
public class SudokuCell : MonoBehaviour 
{
    [Header("Static References")]
    public Image bg;
    public Button button;
    public SudokuDigitDisplay display;
    public RectTransform rectTransform;

    [Header("Mark References")]
    public TextMeshProUGUI crossText;

    public int Row;
    public int Col;
    public int Value;
    public bool IsFixed;
    public bool IsPreFilled;
    public bool IsError;

    public System.Action<int> OnValueChanged;



    /// <summary>
    /// セルがクリックされた時のイベント。インスペクターの Button から静的に呼び出されます。
    /// </summary>
    public void OnCellClicked() {
        if (SudokuGameStandalone.Instance != null) {
            SudokuGameStandalone.Instance.OnCellSelected(this);
        }
    }


    private void OnEnable() {
        SudokuThemeManager.OnThemeChanged += HandleThemeChanged;
    }


    private void OnDisable() {
        SudokuThemeManager.OnThemeChanged -= HandleThemeChanged;
    }


    private void HandleThemeChanged(SudokuData.SudokuTheme theme, bool isInitial) {
        RefreshUI(true);
    }

    public void Init(int r, int c, int val, bool isFixed) {
        this.Row = r;
        this.Col = c;
        this.Value = val;
        this.IsFixed = isFixed;
        this.IsPreFilled = isFixed;
        RefreshUI(true);
    }

    private bool _isSelected;
    private bool _isSameDigit;
    private bool _isRelated;

    public void SetHighlight(bool isSelected, bool isSameDigit, bool isError, bool isRelated) {
        _isSelected = isSelected;
        _isSameDigit = isSameDigit;
        this.IsError = isError;
        _isRelated = isRelated;
        ApplyHighlightVisuals();
    }

    private void ApplyHighlightVisuals() {
        if (bg == null) return;
        var theme = SudokuThemeManager.Instance != null ? SudokuThemeManager.Instance.CurrentTheme : default;
        
        float normalThickness = 2f;    // 通常時は 2px
        float highlightThickness = 4f; // 選択・関連時は 4px
        
        Color targetBgColor = theme.cellColorNormal;
        float targetImageAlpha = 1.0f; // 画像の透明度（通常時は透かさない）
        
        // テーマごとにハイライト時の「透かし具合」を調整
        // Mechanicalはしっかり透かす(0.6)、Nixie/LEDは数字をはっきり残すため少しだけ透かす(0.85)
        float highlightAlpha = (theme.displayType == SudokuData.ThemeDisplayType.Mechanical) ? 0.6f : 0.85f;
        
        // 優先順位: エラー > 選択中 > 同じ数字 > 関連セル
        if (IsError) {
            targetBgColor = theme.errorColor;
            // エラー時は内側に強調（全辺を明るいエラー色で統一）
            SudokuBezelRenderer.ApplyBezel(gameObject, theme.errorMarkColor, theme.errorMarkColor, highlightThickness, false);
        } else if (_isSelected) {
            // 【選択中】背面を明るい黄色で塗りつぶす
            targetBgColor = Color.yellow; 
            targetImageAlpha = highlightAlpha; // 画像を少し透明にして背面の黄色を透かす
            // ベベルは明暗をなくし、全辺を明るい黄色にする
            SudokuBezelRenderer.ApplyBezel(gameObject, Color.yellow, Color.yellow, 4f, true);
        } else if (_isSameDigit) {
            targetBgColor = theme.sameDigitColor;
            // 同じ数字: 内側に強調
            SudokuBezelRenderer.ApplyBezel(gameObject, theme.textColor, theme.textColor * 0.5f, normalThickness, false);
        } else if (_isRelated) {
            // 【関連セル】明るいパステル調の補色を算出
            float h, s, v;
            Color.RGBToHSV(theme.highlightColor, out h, out s, out v);
            h = (h + 0.5f) % 1f; // 色相を180度回転
            if (s < 0.1f) h = 0.5f; // 無彩色ならシアン
            Color standout = Color.HSVToRGB(h, 0.6f, 1f);
            
            // 背面をその色で塗りつぶす
            targetBgColor = standout; 
            targetImageAlpha = highlightAlpha; // 画像を少し透明にして背面の光を透かす
            
            // ベベルにも同じ補色を適用（明暗をなくし全辺明るく）
            SudokuBezelRenderer.ApplyBezel(gameObject, standout, standout, 4f, true);
        } else {
            // 通常時: 内側に 2px (通常設定)
            SudokuBezelRenderer.ApplyBezel(gameObject, theme, false);
        }
        
        bg.color = targetBgColor;

        if (display != null) {
            display.SetBackgroundColor(targetBgColor);
            display.SetImageAlpha(targetImageAlpha);
        }
        UpdateMarks();
    }

    public void RefreshUI(bool immediate = true) {
        var theme = SudokuThemeManager.Instance != null ? SudokuThemeManager.Instance.CurrentTheme : default;
        
        // 強制リセットを廃止し、現在のハイライト状態を再適用
        ApplyHighlightVisuals();

        if (display != null) {
            display.HideZero = true;
            // 盤面では表示の安定性とパフォーマンスを優先し、常に即時表示（アニメなし）とする
            display.SetDigit(Value, theme, true);
        }
        UpdateMarks();
    }

    public void SetValue(int val, bool immediate = true, bool fixedVal = false) {
        Value = val;
        IsFixed = fixedVal;
        RefreshUI(immediate);
        if (OnValueChanged != null) OnValueChanged.Invoke(Value);
    }

    private void UpdateMarks() {
        if (crossText != null) {
            bool shouldShow = IsError && Value > 0;
            var cg = crossText.GetComponent<CanvasGroup>();
            if (cg != null) {
                cg.alpha = shouldShow ? 1f : 0f;
            } else {
                crossText.color = new Color(crossText.color.r, crossText.color.g, crossText.color.b, shouldShow ? 1f : 0f);
            }
        }
    }


    private void Awake() {
        if (button == null) button = GetComponent<Button>();
        
        if (button != null) {
            button.enabled = true;
            button.interactable = true;
        }
    }




    

}
