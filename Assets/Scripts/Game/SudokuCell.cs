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
        Debug.Log($"<b>[CLICK-TRACE] Cell ({Row},{Col}) Button Clicked! Position: {UnityEngine.InputSystem.Pointer.current.position.ReadValue()}</b>");
        
        if (SudokuGameStandalone.Instance != null) {
            Debug.Log($"[DIAGNOSTIC] Notifying Standalone Instance. SelectedCell: {(SudokuGameStandalone.Instance.SelectedCell != null ? SudokuGameStandalone.Instance.SelectedCell.name : "NULL")}");
            SudokuGameStandalone.Instance.OnCellSelected(this);
        } else {
            Debug.LogError("[DIAGNOSTIC] SudokuGameStandalone.Instance is NULL!");
        }
    }


    private void OnEnable() {
        SudokuThemeManager.OnThemeChanged += HandleThemeChanged;
    }


    private void OnDisable() {
        SudokuThemeManager.OnThemeChanged -= HandleThemeChanged;
        
        if (UnityEngine.Application.isPlaying) {
            // 診断用ログ：負荷軽減のためスタックトレースを除去し、通常のログに変更
            Debug.Log($"[CRITICAL-TRACE] SudokuCell on {gameObject.name} was DISABLED!");
        }
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

    public void SetHighlight(bool isSelected, bool isSameDigit, bool isError, bool isRelated) {
        if (bg == null) return;
        var theme = SudokuThemeManager.Instance != null ? SudokuThemeManager.Instance.CurrentTheme : default;
        float thickness = theme.bevelWidth > 0 ? theme.bevelWidth : 1f;
        Color targetColor = theme.cellColorNormal;
        
        if (isError) {
            targetColor = theme.errorColor;
            SudokuBezelRenderer.ApplyBezel(gameObject, theme);
        } else if (isSelected) {
            targetColor = theme.selectionColor; 
            SudokuBezelRenderer.ApplyBezel(gameObject, theme.selectionColor, theme.selectionColor * 0.8f, thickness * 1.5f);
        } else if (isSameDigit) {
            targetColor = theme.sameDigitColor;
            SudokuBezelRenderer.ApplyBezel(gameObject, theme);
        } else if (isRelated) {
            targetColor = theme.relatedColor; 
            SudokuBezelRenderer.ApplyBezel(gameObject, theme.relatedColor, theme.relatedColor * 0.7f, thickness);
        } else {
            SudokuBezelRenderer.ApplyBezel(gameObject, theme);
        }
        bg.color = targetColor;
    }

    public void RefreshUI(bool immediate = true) {
        var theme = SudokuThemeManager.Instance != null ? SudokuThemeManager.Instance.CurrentTheme : default;
        if (bg != null) bg.color = theme.cellColorNormal;
        SudokuBezelRenderer.ApplyBezel(gameObject, theme);
        if (display != null) {
            display.HideZero = true;
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
        }
    }


    private void Awake() {
        if (button == null) button = GetComponent<Button>();
        
        // 【強制有効化テスト】
        if (button != null) {
            button.enabled = true;
            button.interactable = true;
        }

        string btnStatus = (button == null) ? "NULL" : button.enabled.ToString();
        int clickCount = (button != null) ? button.onClick.GetPersistentEventCount() : -1;
        
        Debug.Log($"[DIAGNOSTIC-FORCE] {gameObject.name} Awake. Button={btnStatus}, Clicks={clickCount}");
    }




    

}
