using UnityEngine;

/// <summary>
/// 説明専用のシーン（HelpScene）内の「閉じる」ボタン入力を制御するクラス。
/// 静的UIの原則に基づき、ボタンのクリックイベントから静的に呼び出されます。
/// </summary>
public class SudokuHelpController : MonoBehaviour
{
    private void Start() {
        // シーンロード直後に、現在のテーマをUI全体へ即座に強制適用させる (白い四角や黒背景の完全解消)
        if (SudokuThemeManager.Instance != null) {
            SudokuThemeManager.Instance.NotifyThemeChanged(true);
            if (Camera.main != null) {
                Camera.main.backgroundColor = SudokuThemeManager.Instance.CurrentTheme.backgroundColor;
            }
        }
    }

    /// <summary>
    /// 「閉じる」ボタンが押された際に、メニュー画面（MenuScene）へ戻ります。
    /// </summary>
    public void OnCloseClicked() {
        Debug.Log("[DEBUG] OnCloseClicked: Returning to MenuScene...");
        UnityEngine.SceneManagement.SceneManager.LoadScene("MenuScene");
    }
}
