using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// ゲーム中のHUD（タイマー、ミス回数、難易度表示）およびリザルト画面の制御を担当するクラス。
/// </summary>
public class SudokuGameHUD : MonoBehaviour
{
    [Header("HUD Elements")]
    public TextMeshProUGUI mistakeText;
    public TextMeshProUGUI difficultyText;
    public TextMeshProUGUI themeNameText;
    public GraphicalTimer graphicalTimer;

    [Header("Result Panel")]
    public GameObject resultPanel;
    public TextMeshProUGUI resultHeaderText;
    public Button restartButton;
    public Button backToMenuButton;

    private void Start() {
        // 動的な AddListener は廃止（静的バインドに移行）
        
        // GameManagerのイベントを購読

        if (GameManager.Instance != null) {
            GameManager.Instance.OnMistakeUpdated += UpdateMistakeHUD;
            GameManager.Instance.OnGameWon += () => ShowResult(true);
            GameManager.Instance.OnGameLost += () => ShowResult(false);
        }

        SudokuThemeManager.OnThemeChanged += UpdateThemeHUD;
    }

    private void OnDestroy() {
        SudokuThemeManager.OnThemeChanged -= UpdateThemeHUD;
    }

    public void OnRestartButtonClicked() { if (UIManager.Instance != null) UIManager.Instance.ShowMenu(); }
    public void OnMenuButtonClicked() { if (UIManager.Instance != null) UIManager.Instance.BackToMenu(); }


    public void InitializeHUD(SudokuLogic.Difficulty difficulty) {
        Debug.Log($"[HUD-LOG] InitializeHUD: {difficulty}, Timer: {(graphicalTimer != null ? "OK" : "NULL")}, DiffText: {(difficultyText != null ? "OK" : "NULL")}");
        if (difficultyText != null) difficultyText.text = difficulty.ToString().ToUpper();
        UpdateMistakeHUD();

        if (SudokuThemeManager.Instance != null && themeNameText != null) {
            themeNameText.text = SudokuThemeManager.Instance.CurrentTheme.themeName;
            // 初期化時にテーマスタイルを適用
            ApplyThemeStyles(SudokuThemeManager.Instance.CurrentTheme);
        }
        
        if (resultPanel != null) {
            var cg = resultPanel.GetComponent<CanvasGroup>();
            if (cg != null) {
                cg.alpha = 0f;
                cg.blocksRaycasts = false;
                cg.interactable = false;
            }
        }
    }

    private void UpdateMistakeHUD() {
        if (mistakeText == null || GameManager.Instance == null) return;
        
        if (GameManager.Instance.IsUnlimitedMode) {
            mistakeText.text = $"MISTAKES: {GameManager.Instance.MistCount}";
        } else {
            mistakeText.text = $"MISTAKES: {GameManager.Instance.MistCount}/{GameManager.Instance.MistLimit}";
        }
    }

    private void UpdateThemeHUD(SudokuData.SudokuTheme theme, bool isInitial) {
        if (themeNameText != null) {
            themeNameText.text = theme.themeName;
        }
        // テーマ変更時に共通スタイルを適用
        ApplyThemeStyles(theme);
    }

    /// <summary>
    /// 管理下の全テキストラベルにテーマのスタイルを一括適用します。
    /// </summary>
    private void ApplyThemeStyles(SudokuData.SudokuTheme theme) {
        if (themeNameText != null) SudokuLabelStyler.ApplyStyle(themeNameText, theme);
        if (difficultyText != null) SudokuLabelStyler.ApplyStyle(difficultyText, theme);
        if (mistakeText != null) SudokuLabelStyler.ApplyStyle(mistakeText, theme);
    }

    public void ShowResult(bool won) {
        if (resultPanel != null) {
            var cg = resultPanel.GetComponent<CanvasGroup>();
            if (cg != null) {
                cg.alpha = 1f;
                cg.blocksRaycasts = true;
                cg.interactable = true;
            }

            if (resultHeaderText != null) {
                resultHeaderText.text = won ? "VICTORY" : "GAME OVER";
                resultHeaderText.color = won ? Color.green : Color.red;
            }
        }
    }
}
