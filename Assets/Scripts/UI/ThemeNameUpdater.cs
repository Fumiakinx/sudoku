using UnityEngine;
using TMPro;

/// <summary>
/// テーマ切り替えイベントを監視し、テキストを現在のテーマ名に自動更新します。
/// </summary>
public class ThemeNameUpdater : MonoBehaviour
{
    private TextMeshProUGUI _text;

    private void Awake() {
        _text = GetComponentInChildren<TextMeshProUGUI>();
    }

    private void OnEnable() {
        SudokuThemeManager.OnThemeChanged += HandleThemeChanged;
        Refresh();
    }

    private void OnDisable() {
        SudokuThemeManager.OnThemeChanged -= HandleThemeChanged;
    }

    private void HandleThemeChanged(SudokuData.SudokuTheme theme, bool isInitial) {
        Refresh();
    }

    public void Refresh() {
        if (_text == null) _text = GetComponentInChildren<TextMeshProUGUI>();
        if (_text == null || SudokuThemeManager.Instance == null) return;

        var theme = SudokuThemeManager.Instance.CurrentTheme;
        _text.text = theme.themeName.ToUpper();
        _text.color = theme.textColor;
    }
}
