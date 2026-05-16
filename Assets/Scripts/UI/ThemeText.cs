using UnityEngine;
using TMPro;

/// <summary>
/// 個別のTextMeshProUGUIに対してテーマを適用します。
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class ThemeText : MonoBehaviour
{
    private TextMeshProUGUI _text;

    private void Awake() {
        _text = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable() {
        SudokuThemeManager.OnThemeChanged += HandleThemeChanged;
        Refresh();
    }

    private void OnDisable() {
        SudokuThemeManager.OnThemeChanged -= HandleThemeChanged;
    }

    public void Refresh() {
        if (SudokuThemeManager.Instance != null) {
            HandleThemeChanged(SudokuThemeManager.Instance.CurrentTheme, false);
        }
    }

    private void HandleThemeChanged(SudokuData.SudokuTheme theme, bool isInitial) {
        if (string.IsNullOrEmpty(theme.themeName) || _text == null) return;
        SudokuLabelStyler.ApplyStyle(_text, theme);
    }
}
