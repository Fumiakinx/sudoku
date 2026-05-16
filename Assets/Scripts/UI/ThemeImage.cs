using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 個別のImageに対してテーマを適用します。
/// ThemeElementによる一括検索を排除し、静的な管理を可能にします。
/// </summary>
[RequireComponent(typeof(Image))]
public class ThemeImage : MonoBehaviour
{
    public enum ColorType {
        Background,
        CellNormal,
        Panel,
        Text, // 枠線などでテキスト色を使いたい場合
        Highlight
    }

    public ColorType colorType = ColorType.CellNormal;
    private Image _image;

    private void Awake() {
        _image = GetComponent<Image>();
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
        if (string.IsNullOrEmpty(theme.themeName) || _image == null) return;

        switch (colorType) {
            case ColorType.Background: _image.color = theme.backgroundColor; break;
            case ColorType.CellNormal: _image.color = theme.cellColorNormal; break;
            case ColorType.Panel:      _image.color = theme.panelColor; break;
            case ColorType.Text:       _image.color = theme.textColor; break;
            case ColorType.Highlight:  _image.color = theme.highlightColor; break;
        }
    }
}
