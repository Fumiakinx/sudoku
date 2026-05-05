using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("HUD")]
    public TextMeshProUGUI timeText;
    public GraphicalTimer graphicalTimer;
    public TextMeshProUGUI mistakeText;
    public TextMeshProUGUI difficultyText;

    [Header("Panels")]
    public GameObject menuPanel;
    public GameObject gamePanel;
    public GameObject resultPanel;
    public TextMeshProUGUI resultHeaderText;
    public GameObject inputPanel;
    private Button[] inputButtons;

    [Header("Game Control")]
    [SerializeField] private SudokuBoard sudokuBoard;
    [SerializeField] private Toggle noteToggle;
    [SerializeField] private Toggle unlimitedModeToggle;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        InitializeGameEvents();
        InitializeInputButtons();
        InitializeMenuButtons();
        ShowMenu();
        
        // 起動時にも確実にテーマを反映させる
        if (SudokuUIStyler.Instance != null) {
            SudokuUIStyler.Instance.ApplyTheme(true);
        }
    }

    private void InitializeGameEvents()
    {
        if (GameManager.Instance != null) {
            GameManager.Instance.OnMistakeUpdated += UpdateHUD;
            GameManager.Instance.OnGameWon += () => ShowResult(true);
            GameManager.Instance.OnGameLost += () => ShowResult(false);
        }
    }

    private void InitializeMenuButtons()
    {
        var allButtons = Resources.FindObjectsOfTypeAll<Button>();
        Debug.Log($"UIManager: InitializeMenuButtons found {allButtons.Length} total buttons in Resources");
        foreach (var btn in allButtons)
        {
            var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt == null) continue;

            string label = txt.text.ToUpper().Trim();
            Debug.Log($"UIManager: Examining button '{btn.gameObject.name}' with label '{label}'");

            if (label.Contains("EASY")) {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnDifficultyButtonClick("EASY"));
                Debug.Log("UIManager: Registered EASY listener");
            }
            else if (label.Contains("MEDIUM")) {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnDifficultyButtonClick("MEDIUM"));
                Debug.Log("UIManager: Registered MEDIUM listener");
            }
            else if (label.Contains("HARD")) {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnDifficultyButtonClick("HARD"));
                Debug.Log("UIManager: Registered HARD listener");
            }
            else if (label.Contains("EXPERT")) {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnDifficultyButtonClick("EXPERT"));
                Debug.Log("UIManager: Registered EXPERT listener");
            }
            else if (label.Contains("RESTART") || label.Contains("NEW GAME")) {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => ShowMenu());
                Debug.Log("UIManager: Registered RESTART/NEW GAME listener");
            }
            
            // 追加: テーマ切り替えボタンの処理 (名前で判定)
            // テーマ切り替えボタンはインスペクターで設定済みのため、ここではスキップ（2重登録防止）
            if (btn.gameObject.name == "Btn_ThemeToggle") {
                Debug.Log("UIManager: ThemeToggle found (using persistent listener)");
            }
        }
    }

    private void OnDifficultyButtonClick(string difficultyLabel) {
        Debug.Log($"UIManager: Difficulty Button Clicked: {difficultyLabel}");
        int difficultyIndex = 0;
        switch (difficultyLabel)
        {
            case "EASY":
                difficultyIndex = 0;
                break;
            case "MEDIUM":
                difficultyIndex = 1;
                break;
            case "HARD":
                difficultyIndex = 2;
                break;
            case "EXPERT":
                difficultyIndex = 3;
                break;
        }
        StartGame(difficultyIndex);
    }

    private void AddMenuListener(Button btn, int diffIndex)
    {
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => StartGame(diffIndex));
    }

    private void InitializeInputButtons()
    {
        if (inputPanel == null) return;
        inputButtons = new Button[10];
        
        // 0-9までのボタンを検索
        for (int i = 0; i <= 9; i++)
        {
            int num = i;
            // 0番目は Btn_Clear、1-9番目は Btn_1 ... Btn_9
            string targetName = (i == 0) ? "Btn_Clear" : "Btn_" + i;
            
            foreach (Transform child in inputPanel.transform) {
                if (child.name == targetName) {
                    var btn = child.GetComponent<Button>();
                    if (btn != null) {
                        inputButtons[i] = btn;
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => OnInputButtonClick(num));
                    }
                    break;
                }
            }
        }
    }

    private void OnInputButtonClick(int num)
    {
        Debug.Log($"[UIManager] OnInputButtonClick: Number={num}");
        
        if (sudokuBoard == null) {
            Debug.LogError("[UIManager] OnInputButtonClick FAILED: SudokuBoard is null!");
            return;
        }

        if (sudokuBoard.SelectedCell == null) {
            Debug.Log("[UIManager] OnInputButtonClick IGNORED: No cell is currently selected.");
            return;
        }

        sudokuBoard.InputNumber(num);
        UpdateInputPanel(sudokuBoard.SelectedCell);
    }

    private void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current != null && 
            UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (gamePanel != null && gamePanel.activeSelf) ShowMenu();
        }

        if (GameManager.Instance != null && !GameManager.Instance.IsGameOver && !GameManager.Instance.IsPaused)
        {
            UpdateTimeDisplay();
        }
    }

    public void ShowMenu()
    {
        if (menuPanel != null) menuPanel.SetActive(true);
        if (gamePanel != null) gamePanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);
        if (GameManager.Instance != null) GameManager.Instance.AbortGame();
        
        // メニューに戻った時も背景色を確実に維持
        if (SudokuUIStyler.Instance != null) SudokuUIStyler.Instance.ApplyTheme(true);
    }

    public void StartGame(int difficultyIndex)
    {
        bool unlimited = unlimitedModeToggle != null && unlimitedModeToggle.isOn;
        SudokuLogic.Difficulty diff = (SudokuLogic.Difficulty)difficultyIndex;
        
        if (GameManager.Instance != null) GameManager.Instance.StartNewGame(diff, unlimited);
        
        if (menuPanel != null) menuPanel.SetActive(false);
        if (gamePanel != null) gamePanel.SetActive(true);
        if (difficultyText != null) difficultyText.text = diff.ToString().ToUpper();
        
        // 【最重要】ゲーム開始後、盤面が生成された瞬間にテーマを再適用する
        StartCoroutine(ApplyThemeDeferred());
        UpdateHUD();
    }

    private IEnumerator ApplyThemeDeferred() {
        yield return null; // 1フレーム待って生成を待つ
        if (SudokuUIStyler.Instance != null) {
            SudokuUIStyler.Instance.ApplyTheme(true);
        }
    }

    private void UpdateHUD()
    {
        if (mistakeText == null || GameManager.Instance == null) return;
        mistakeText.text = GameManager.Instance.IsUnlimitedMode ? 
            $"MISTAKES: {GameManager.Instance.MistCount}" : 
            $"MISTAKES: {GameManager.Instance.MistCount}/{GameManager.Instance.MistLimit}";
    }

    private void UpdateTimeDisplay()
    {
        if (GameManager.Instance == null) return;
        float time = GameManager.Instance.GameTime;
        if (graphicalTimer != null) graphicalTimer.UpdateTime(time);
        else if (timeText != null)
        {
            int hours = Mathf.FloorToInt(time / 3600);
            int minutes = Mathf.FloorToInt((time % 3600) / 60);
            int seconds = Mathf.FloorToInt(time % 60);
            timeText.text = string.Format("{0:00}:{1:00}:{2:00}", hours, minutes, seconds);
        }
    }

    private void ShowResult(bool won)
    {
        if (resultPanel != null) resultPanel.SetActive(true);
        if (resultHeaderText != null) {
            resultHeaderText.text = won ? "CONGRATULATIONS!" : "GAME OVER";
            resultHeaderText.color = won ? Color.green : Color.red;
        }
        
        // サブテキスト（詳細メッセージ）の更新
        var subTextTransform = resultPanel.transform.Find("SubText");
        if (subTextTransform != null) {
            var subText = subTextTransform.GetComponent<TextMeshProUGUI>();
            if (subText != null) {
                subText.text = won ? "YOU SOLVED THE PUZZLE!" : "OUT OF LIVES. TRY AGAIN!";
            }
        }
    }

    public void UpdateInputPanel(SudokuCell selectedCell)
    {
        if (inputButtons == null) return;
        
        int[] counts = (sudokuBoard != null) ? sudokuBoard.GetNumberCounts() : new int[9];
        for (int i = 0; i <= 9; i++)
        {
            if (inputButtons[i] == null) continue;
            
            if (i == 0) {
                // 消しゴムボタン (Btn_0)
                // 選択されたセルがエラー状態（×印）である場合のみ有効
                inputButtons[i].interactable = (selectedCell != null && !selectedCell.IsFixed && selectedCell.IsError);
            } else {
                // 数字ボタン (Btn_1 - Btn_9)
                int num = i;
                bool isCompleted = counts[i - 1] >= 9;
                
                if (selectedCell == null || selectedCell.IsFixed) {
                    inputButtons[i].interactable = false;
                } else {
                    inputButtons[i].interactable = !isCompleted;
                }
            }
        }
    }
}
