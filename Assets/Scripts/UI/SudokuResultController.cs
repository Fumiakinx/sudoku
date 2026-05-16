using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// リザルト画面（ResultPanel）内のボタン入力を制御するクラス。
/// </summary>
public class SudokuResultController : MonoBehaviour
{
    [Header("Result UI")]
    public TMPro.TextMeshProUGUI resultText;
    public Button menuButton;

    private void Start()
    {
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        if (resultText != null)
        {
            resultText.text = SudokuGameState.LastGameWon ? "CLEAR!" : "GAME OVER";
            
            // テーマに合わせた色を適用
            if (SudokuThemeManager.Instance != null) {
                var theme = SudokuThemeManager.Instance.CurrentTheme;
                // 勝利時はテーマのメイン色、敗北時は少し暗め、などの調整が可能
                resultText.color = theme.textColor; 
                
                // 背景色の適用（カメラ）
                var cam = Camera.main;
                if (cam != null) {
                    cam.backgroundColor = theme.backgroundColor;
                }
                
                if (menuButton != null) {
                    var btnText = menuButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                    if (btnText != null) {
                        btnText.text = "NEW GAME"; // 確実にセット
                        btnText.color = theme.textColor;
                        btnText.fontSize = 60; // サイズも安定させる
                    }
                }
            }
        }

        // ボタンにもテーマ（ベゼル）を適用
        if (menuButton != null && SudokuThemeManager.Instance != null) {
            SudokuBezelRenderer.ApplyBezel(menuButton.gameObject, SudokuThemeManager.Instance.CurrentTheme);
        }
    }

    public void OnMenuButtonClicked()
    {
        Debug.Log("[SudokuResultController] New Game (Menu) Clicked");
        if (UIManager.Instance != null)
        {
            UIManager.Instance.BackToMenu();
        }
        else
        {
            // フォールバック：直接ロード
            UnityEngine.SceneManagement.SceneManager.LoadScene("MenuScene");
        }
    }
}
