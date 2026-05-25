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
        
        float selectThickness = 10f;  // 選択・エラーセルは超極太の 10px！
        float relatedThickness = 8f;  // 関連セル（十字ライン）は極太の 8px！
        
        Color baseCellColor = theme.useOriginalSpriteColor ? theme.originalSpriteBgColor : theme.cellColorNormal;
        
        // 輝度（Luminance）の算出 (ITU-R BT.709 規格に基づき人間が感じる明るさを 0.0〜1.0 で自動判定)
        float luminance = 0.2126f * baseCellColor.r + 0.7152f * baseCellColor.g + 0.0722f * baseCellColor.b;
        
        // 通常の背景色を常に維持（ベタ塗り・塗りつぶしを完全廃止！）
        Color targetBgColor = baseCellColor;
        float targetImageAlpha = 1.0f; 
        Color imageMultiplierColor = Color.white;
        
        // 優先順位: エラー > 選択中 > 同じ数字 > 関連セル
        if (IsError) {
            // エラー時は極太 10px の警告色ベゼルを適用（背景色に合わせてコントラスト自動調整）
            Color adjustedErrorColor = SudokuFeedbackOverlay.GetVisibleMarkColor(theme.errorMarkColor, baseCellColor, false);
            SudokuBezelRenderer.ApplyBezel(gameObject, adjustedErrorColor, adjustedErrorColor, selectThickness);
        } else if (_isSelected) {
            // 【選択中：最前面・極太 10px インテリジェントゴールドベゼル】
            // 黒背景（luminance=0）の時：明るく輝くネオンゴールド
            // 白背景（luminance=1）の時：白地に溶けず美しく引き締まるクラシックゴールド
            Color darkGold = new Color(0.75f, 0.55f, 0.15f, 1f); 
            Color lightGold = new Color(0.95f, 0.85f, 0.55f, 1f); 
            Color finalGold = Color.Lerp(lightGold, darkGold, luminance);

            SudokuBezelRenderer.ApplyBezel(gameObject, finalGold, finalGold, selectThickness);
        } else if (_isSameDigit) {
            // 同じ数字：太さ 4px で文字色をインナーベゼルとして適用
            SudokuBezelRenderer.ApplyBezel(gameObject, theme.textColor, theme.textColor * 0.5f, 4f);
        } else if (_isRelated) {
            // 【関連セル：最前面・極太 8px インテリジェントブルーベゼル】
            // 黒背景（luminance=0）の時：鮮やかなネオンブルー
            // 白背景（luminance=1）の時：白地に溶けず上品に引き締まる深みのあるロイヤルネイビーブルー
            Color deepNavy = new Color(0.12f, 0.35f, 0.55f, 1f); 
            Color neonBlue = new Color(0.48f, 0.85f, 0.95f, 1f); 
            Color finalBlue = Color.Lerp(neonBlue, deepNavy, luminance);

            SudokuBezelRenderer.ApplyBezel(gameObject, finalBlue, finalBlue, relatedThickness);
        } else {
            // 通常時: 通常の枠線を描画 (2px)
            SudokuBezelRenderer.ApplyBezel(gameObject, theme);
        }
        
        bg.color = targetBgColor;

        if (display != null) {
            display.SetBackgroundColor(targetBgColor);
            display.SetImageAlpha(targetImageAlpha);
            display.SetImageColor(theme.useOriginalSpriteColor ? imageMultiplierColor : theme.textColor);
        }
        UpdateMarks();
    }

    public void RefreshUI(bool immediate = true) {
        var theme = SudokuThemeManager.Instance != null ? SudokuThemeManager.Instance.CurrentTheme : default;
        
        // 強制リセットを廃止し、現在のハイライト状態を再適用
        ApplyHighlightVisuals();

        if (display != null) {
            display.HideZero = true;
            // 指定された immediate パラメータを渡してアニメーションを制御します
            display.SetDigit(Value, theme, immediate);
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

            if (shouldShow) {
                var theme = SudokuThemeManager.Instance != null ? SudokuThemeManager.Instance.CurrentTheme : default;
                // セルの背景色を取得
                Color cellBgColor = bg != null ? bg.color : Color.black;
                // コントラスト調整を適用したバツ印の色を設定
                crossText.color = SudokuFeedbackOverlay.GetVisibleMarkColor(theme.errorMarkColor, cellBgColor, false);
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
