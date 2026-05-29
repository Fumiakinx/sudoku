using UnityEngine;
using System;

public class GameManager : MonoBehaviour
{
    private static GameManager instance;
    public static GameManager Instance {
        get {
            if (instance == null) instance = UnityEngine.Object.FindAnyObjectByType<GameManager>(FindObjectsInactive.Include);
            return instance;
        }
    }

    [SerializeField] private SudokuGameStandalone sudokuBoard;
    [SerializeField] private SudokuData sudokuData;
    [SerializeField] private GraphicalTimer graphicalTimer;

    public int MistCount { get; private set; }
    public int MistLimit { get; private set; }
    public bool IsUnlimitedMode { get; private set; }
    public float GameTime { get; private set; }
    public bool IsPaused { get; private set; }
    public bool IsGameOver { get; private set; }

    public event Action OnMistakeUpdated;
    public event Action OnGameWon;
    public event Action OnGameLost;

    private void Awake()
    {
        if (Instance != null && Instance != this) {
            Debug.Log($"<color=yellow>[TRACE] GameManager.Awake: Found existing instance. Destroying OLD on [{Instance.gameObject.name}] to prioritize NEW on [{gameObject.name}]</color>");
            Destroy(Instance.gameObject);
        }
        instance = this;
        IsGameOver = true;
        Debug.Log($"<color=yellow>[TRACE] GameManager.Awake: NEW Instance initialized on [{gameObject.name}] in Scene [{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}]</color>");
    }

    private void Start() {
        EnsureTimer();
    }

    private void EnsureTimer() {
        if (graphicalTimer == null || graphicalTimer.gameObject == null) {
            if (UIManager.Instance != null && UIManager.Instance.gameHUD != null && UIManager.Instance.gameHUD.graphicalTimer != null) {
                graphicalTimer = UIManager.Instance.gameHUD.graphicalTimer;
            } else {
                graphicalTimer = UnityEngine.Object.FindAnyObjectByType<GraphicalTimer>(FindObjectsInactive.Include);
            }
        }
    }

    public void StartNewGame(SudokuLogic.Difficulty difficulty, bool unlimited)
    {
        Debug.Log($"<color=cyan>【GameManager】StartNewGame ENTER: difficulty={difficulty}, unlimited={unlimited}</color>");
        EnsureTimer();
        MistCount = 0;
        IsUnlimitedMode = unlimited;
        MistLimit = unlimited ? int.MaxValue : (sudokuData != null ? sudokuData.defaultMistLimit : 3);
        Debug.Log($"[GameManager] MistLimit set to: {MistLimit} (sudokuData: {(sudokuData != null ? "OK" : "NULL")})");
        GameTime = 0;
        IsPaused = false;
        IsGameOver = false;
        if (sudokuBoard != null) {
            Debug.Log("[GameManager] Calling sudokuBoard.GenerateNewGame...");
            sudokuBoard.GenerateNewGame(difficulty);
        } else {
            Debug.LogError("[GameManager] CRITICAL ERROR: sudokuBoard is NULL! Initialization cannot proceed.");
        }
        Debug.Log("<color=cyan>【GameManager】StartNewGame EXIT</color>");
    }

    private void Update()
    {
        if (IsGameOver || IsPaused) return;

        GameTime += Time.deltaTime;
        
        if (graphicalTimer != null) {
            graphicalTimer.UpdateTime(GameTime);
        }
    }

    public void OnMistake()
    {
        MistCount++;
        Debug.Log($"[GameManager] Mistake: {MistCount}/{MistLimit}");
        OnMistakeUpdated?.Invoke();
        if (!IsUnlimitedMode && !IsGameOver && MistCount >= MistLimit) GameOver(false);
    }

    public void OnWin() {
        Debug.Log("[GameManager] Game Clear! Notifying via Event.");
        SudokuGameState.LastGameWon = true;
        GameOver(true);
    }

    private void GameOver(bool won)
    {
        Debug.Log($"[GameManager] GameOver: {(won ? "WON" : "LOST")}");
        IsGameOver = true;
        SudokuGameState.LastGameWon = won;
        SudokuGameState.LastGameTime = GameTime; // タイムを保存
        
        if (won) OnGameWon?.Invoke();
        else OnGameLost?.Invoke();

        // リザルト画面へ遷移
        UnityEngine.SceneManagement.SceneManager.LoadScene("ResultScene");
    }

    public void TogglePause() => IsPaused = !IsPaused;
    public void AbortGame() => IsGameOver = true;
}
