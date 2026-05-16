using UnityEngine;

/// <summary>
/// オブジェクトに現在のテーマに基づいたベベル（枠線）を自動的に適用するコンポーネント。
/// </summary>
public class SudokuBezel : MonoBehaviour
{
    private RectTransform _rt;

    private void Awake() {
        _rt = GetComponent<RectTransform>();
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
        SudokuBezelRenderer.ApplyBezel(gameObject, theme);
    }
}
