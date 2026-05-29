using UnityEngine;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using TMPro;

/// <summary>
/// リザルト画面（ResultPanel）内のボタン入力およびポータル・ランキング連携を制御するクラス。
/// </summary>
public class SudokuResultController : MonoBehaviour
{
    [Header("Result UI")]
    public TextMeshProUGUI resultText;
    public Button menuButton;

    [Header("Portal & Ranking UI")]
    public TextMeshProUGUI timeText;
    public TMP_InputField playerNameInputField;
    public Button sendButton;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern string GetPlayerNameFromBrowser();

    [DllImport("__Internal")]
    private static extern void SendScoreToBrowser(string difficultyStr, float elapsedTime, string playerNameStr);
#else
    private static string GetPlayerNameFromBrowser() => "";
    private static void SendScoreToBrowser(string difficultyStr, float elapsedTime, string playerNameStr) 
    {
        Debug.Log($"[Editor Demo] SendScore: Diff={difficultyStr}, Time={elapsedTime}, Name={playerNameStr}");
    }
#endif

    private void Start()
    {
        ApplyTheme();
        SetupPortalIntegration();
    }

    private void ApplyTheme()
    {
        if (resultText != null)
        {
            resultText.text = SudokuGameState.LastGameWon ? "CLEAR!" : "GAME OVER";
            
            // テーマに合わせた色を適用
            if (SudokuThemeManager.Instance != null) {
                var theme = SudokuThemeManager.Instance.CurrentTheme;
                resultText.color = theme.textColor; 
                
                // 背景色の適用（カメラ）
                var cam = Camera.main;
                if (cam != null) {
                    cam.backgroundColor = theme.backgroundColor;
                }
                
                if (menuButton != null) {
                    var btnText = menuButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (btnText != null) {
                        btnText.text = "NEW GAME"; // 確実にセット
                        btnText.color = theme.textColor;
                        btnText.fontSize = 60; // サイズも安定させる
                    }
                }

                // 送信ボタンにもテーマ色を適用
                if (sendButton != null) {
                    var sendBtnText = sendButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (sendBtnText != null) {
                        sendBtnText.color = theme.textColor;
                    }
                }
                
                // タイムテキストにもテーマ色を適用
                if (timeText != null) {
                    timeText.color = theme.textColor;
                }
            }
        }

        // ボタンにもテーマ（ベゼル）を適用
        if (menuButton != null && SudokuThemeManager.Instance != null) {
            SudokuBezelRenderer.ApplyBezel(menuButton.gameObject, SudokuThemeManager.Instance.CurrentTheme);
        }
        if (sendButton != null && SudokuThemeManager.Instance != null) {
            SudokuBezelRenderer.ApplyBezel(sendButton.gameObject, SudokuThemeManager.Instance.CurrentTheme);
        }
    }

    private void SetupPortalIntegration()
    {
        bool isWon = SudokuGameState.LastGameWon;

        // 勝利時（クリア時）のみランキング・ポータルUIを表示
        if (timeText != null) timeText.gameObject.SetActive(isWon);
        if (playerNameInputField != null) playerNameInputField.gameObject.SetActive(isWon);
        if (sendButton != null) sendButton.gameObject.SetActive(isWon);

        if (isWon)
        {
            // 1. クリアタイムのフォーマットと表示
            float elapsed = SudokuGameState.LastGameTime;
            int minutes = Mathf.FloorToInt(elapsed / 60F);
            int seconds = Mathf.FloorToInt(elapsed - minutes * 60);
            if (timeText != null)
            {
                timeText.text = string.Format("TIME: {0:00}:{1:00}", minutes, seconds);
            }

            // 2. ブラウザポータルからプレイヤー名を取得してInputFieldにセット
            if (playerNameInputField != null)
            {
                string browserName = "";
                try
                {
                    browserName = GetPlayerNameFromBrowser();
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("[SudokuResultController] Browser integration error: " + ex.Message);
                }

                if (!string.IsNullOrEmpty(browserName))
                {
                    playerNameInputField.text = browserName;
                }
                else
                {
                    // フォールバック: 前回のセーブされたプレイヤー名
                    playerNameInputField.text = PlayerPrefs.GetString("SudokuPlayerName", "");
                }
            }
        }
    }

    /// <summary>
    /// ランキング送信ボタンが押された時の処理
    /// </summary>
    public void OnSendScoreButtonClicked()
    {
        if (!SudokuGameState.LastGameWon) return;

        string playerName = playerNameInputField != null ? playerNameInputField.text.Trim() : "匿名";
        if (string.IsNullOrEmpty(playerName))
        {
            playerName = "匿名";
        }

        // 次回起動時のためにPlayerPrefsに保存
        PlayerPrefs.SetString("SudokuPlayerName", playerName);
        PlayerPrefs.Save();

        // 難易度の文字列変換
        string difficultyStr = SudokuGameState.SelectedDifficulty.ToString().ToUpper();
        float elapsed = SudokuGameState.LastGameTime;

        Debug.Log($"[SudokuResultController] Sending Score: Diff={difficultyStr}, Time={elapsed}, Name={playerName}");

        try
        {
            // WebGLプラグインを呼び出してブラウザにスコアを送信
            SendScoreToBrowser(difficultyStr, elapsed, playerName);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[SudokuResultController] Failed to send score to browser: " + ex.Message);
        }

        // 送信後はボタンを無効化して二重送信を防ぐ
        if (sendButton != null)
        {
            sendButton.interactable = false;
            var btnText = sendButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                btnText.text = "SENT";
            }
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
