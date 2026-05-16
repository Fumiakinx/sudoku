using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ゲーム全体の見た目のグローバル設定（カメラの背景色など）を制御します。
/// 個別のパネルの色変更は各オブジェクトの ThemeElement に委譲し、冗長な処理を削除しました。
/// </summary>
public class SudokuUIController : MonoBehaviour
{
    private static SudokuUIController _instance;
    public static SudokuUIController Instance => _instance;

    private void Awake() {
        if (_instance != null && _instance != this) {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    private void OnEnable() {
        SudokuThemeManager.OnThemeChanged += HandleThemeChanged;
    }

    private void Start() {
        Refresh();
    }

    private void OnDisable() {
        SudokuThemeManager.OnThemeChanged -= HandleThemeChanged;
    }

    public void Refresh() {
        if (SudokuThemeManager.Instance == null) return;
        HandleThemeChanged(SudokuThemeManager.Instance.CurrentTheme, false);
    }

    private void HandleThemeChanged(SudokuData.SudokuTheme theme, bool isInitial) {
        if (string.IsNullOrEmpty(theme.themeName)) return;

        // カメラの背景色をテーマに合わせる（グローバルな設定）
        if (Camera.main != null) {
            Camera.main.backgroundColor = theme.backgroundColor;
        }
        
        // 注意：かつてここで UIManager を探して各パネルの色を外側から変えていましたが、
        // 各パネルの ThemeElement が自律的に処理するようになったため、そのコードは削除しました。
    }
}
