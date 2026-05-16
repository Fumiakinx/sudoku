// Re-syncing with SudokuCell
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 静的UIベースで動作する、自律型の数独ゲームコントローラー。
/// 既存の壊れやすいマネージャーへの依存を排除し、このスクリプト一つでゲームSceneを完結させます。
/// </summary>
public class SudokuGameStandalone : MonoBehaviour {
    public static SudokuGameStandalone Instance { get; private set; }

    [Header("Data")]
    public SudokuData sudokuData;

    [Header("Static UI References")]
    [Tooltip("81個のセルを順番（行優先）に登録してください")]
    public List<SudokuCell> serializedCells = new List<SudokuCell>();

    [Header("State")]
    private int[,] puzzleGrid;
    private int[,] solutionGrid;
    private SudokuCell[,] cells = new SudokuCell[9, 9];
    private SudokuCell selectedCell;
    public SudokuCell SelectedCell => selectedCell;


    private void Awake() {
        Instance = this;
        
        // データのロード（フォールバック付）
        if (sudokuData == null) {
            sudokuData = UnityEditor.AssetDatabase.LoadAssetAtPath<SudokuData>("Assets/Data/SudokuData.asset");
        }

        // シーン内の要素の紐付け（リストから2次元配列を構築）
        RebuildGridFromSerialized();
    }

    private void Start() {
        // ゲームの初期化は UIManager 側の制御（StartGameInternal）に一任します。
        // ここでの自律的な呼び出しは二重初期化の原因となるため削除しました。
    }


    private void Update() {
        // 入力検知は UIManager に集約されました。
    }

    private void RebuildGridFromSerialized() {
        if (serializedCells.Count != 81) {
            Debug.LogWarning($"[Standalone] Serialized cells count is {serializedCells.Count}, expected 81. Grid might be incomplete.");
        }

        foreach (var cell in serializedCells) {
            if (cell == null) continue;
            
            string[] parts = cell.name.Split('_');
            int r = -1, c = -1;
            if (parts.Length >= 3 && int.TryParse(parts[1], out r) && int.TryParse(parts[2], out c)) {
                if (r >= 0 && r < 9 && c >= 0 && c < 9) {
                    cells[r, c] = cell;
                }
            }
        }
    }

    public void GenerateNewGame(SudokuLogic.Difficulty difficulty) {
        SudokuGameState.SelectedDifficulty = difficulty;
        InitializeGame();
    }

    private void InitializeGame() {
        // 難易度の取得
        SudokuLogic.Difficulty diff = SudokuGameState.SelectedDifficulty;
        
        var result = SudokuLogic.Generate(diff);
        puzzleGrid = result.puzzle;
        solutionGrid = result.solution;
        int initCount = 0;
        for (int r = 0; r < 9; r++) {
            for (int c = 0; c < 9; c++) {
                if (cells[r, c] != null) {
                    cells[r, c].Init(r, c, puzzleGrid[r, c], puzzleGrid[r, c] != 0);
                    initCount++;
                } else {
                    Debug.LogWarning($"[Standalone] Cell ({r},{c}) is MISSING during initialization!");
                }
            }
        }
        Debug.Log($"[LIFE-LOG] SudokuGameStandalone: {initCount} cells initialized.");
        ApplyTheme(true);
    }

    private int CountPreFilled(int[,] grid) {
        int count = 0;
        for (int r = 0; r < 9; r++)
            for (int c = 0; c < 9; c++)
                if (grid[r, c] != 0) count++;
        return count;
    }

    public void OnCellSelected(SudokuCell cell) {
        if (cell == null || cell.IsFixed) return;
        selectedCell = cell;
        try {
            UpdateHighlights();
        } catch (System.Exception e) {
            Debug.LogError($"[DIAGNOSTIC] UpdateHighlights CRASHED: {e.Message}\n{e.StackTrace}");
        }
    }

    public void OnInputButtonClicked(int value) {
        if (selectedCell == null) return;
        if (selectedCell.IsPreFilled) return;

        // Cボタン (-2) の特殊処理：テストクリア
        if (value == -2) {
            selectedCell.SetValue(0);
            Debug.Log("<color=green><b>[INPUT-FLOW] TEST WIN TRIGGERED BY C</b></color>");
            if (GameManager.Instance != null) {
                GameManager.Instance.OnWin(); // GameManager側の遷移処理を呼ぶ
            }
            return;
        }

        // 通常の数字入力
        selectedCell.SetValue(value);

        // ミス判定（正解データとの照合）
        if (value != 0 && value != solutionGrid[selectedCell.Row, selectedCell.Col]) {
            selectedCell.IsError = true;
            Debug.LogWarning($"<color=red><b>[MISTAKE] Mistake detected at ({selectedCell.Row}, {selectedCell.Col})!</b></color> Expected {solutionGrid[selectedCell.Row, selectedCell.Col]}, got {value}");
            
            // 【新規演出】盤面全体に大きなバツを表示
            if (SudokuFeedbackOverlay.Instance != null) {
                SudokuFeedbackOverlay.Instance.ShowMistake();
            }

            if (GameManager.Instance != null) {
                Debug.Log("[MISTAKE] Invoking GameManager.OnMistake()");
                GameManager.Instance.OnMistake();
            }
        } else {
            if (value != 0) {
                // 【修正】正解が入力されたら、そのセルを固定（変更・選択不可）にする
                selectedCell.IsFixed = true;
                selectedCell.IsError = false;

                // 【新規演出】盤面全体に大きな丸を表示し、終了後に選択解除
                if (SudokuFeedbackOverlay.Instance != null) {
                    SudokuFeedbackOverlay.Instance.ShowCorrect(() => {
                        selectedCell = null;
                        UpdateHighlights();
                    });
                } else {
                    // 演出がない場合は即座に解除
                    selectedCell = null;
                    UpdateHighlights();
                }
            } else {
                selectedCell.IsError = false;
            }
        }

        UpdateHighlights();
        
        if (CheckWin()) {
            Debug.Log("<color=green><b>[INPUT-FLOW] NATURAL WIN DETECTED!</b></color>");
            if (GameManager.Instance != null) {
                GameManager.Instance.OnWin();
            }
        }
    }

    private void UpdateHighlights() {
        if (cells == null) {
            return;
        }
        for (int r = 0; r < 9; r++) {
            for (int c = 0; c < 9; c++) {
                if (cells[r, c] == null) continue;
                
                try {
                    bool isSelected = (cells[r, c] == selectedCell);
                    bool isRelated = false;
                    if (selectedCell != null) {
                        isRelated = (cells[r, c].Row == selectedCell.Row || 
                                     cells[r, c].Col == selectedCell.Col ||
                                     (cells[r, c].Row / 3 == selectedCell.Row / 3 && cells[r, c].Col / 3 == selectedCell.Col / 3));
                    }
                    bool isSameDigit = (selectedCell != null && selectedCell.Value != 0 && cells[r, c].Value == selectedCell.Value);
                    
                    cells[r, c].SetHighlight(isSelected, isSameDigit, cells[r, c].IsError, isRelated);
                } catch (System.Exception e) {
                    Debug.LogError($"[DIAGNOSTIC] Loop Error at ({r},{c}): {e.Message}");
                    throw; // 親のcatchへ飛ばす
                }
            }
        }
        // 入力パネルの状態を更新
        if (UIManager.Instance != null && UIManager.Instance.inputPanelController != null) {
            UIManager.Instance.inputPanelController.UpdateButtonStates(this, selectedCell);
        }
    }


    private void CheckErrors() {
        for (int r = 0; r < 9; r++) {
            for (int c = 0; c < 9; c++) {
                if (cells[r, c] == null) continue;
                cells[r, c].IsError = false;
                if (cells[r, c].Value != 0) {
                    if (cells[r, c].Value != solutionGrid[r, c]) {
                         cells[r, c].IsError = true;
                    }
                }
            }
        }
    }

    private bool CheckWin() {
        for (int r = 0; r < 9; r++) {
            for (int c = 0; c < 9; c++) {
                if (cells[r, c] == null || cells[r, c].Value == 0 || cells[r, c].IsError) return false;
            }
        }
        return true;
    }

    public int[] GetNumberCounts() {
        int[] counts = new int[9];
        for (int r = 0; r < 9; r++) {
            for (int c = 0; c < 9; c++) {
                int val = cells[r, c] != null ? cells[r, c].Value : 0;
                if (val >= 1 && val <= 9) counts[val - 1]++;
            }
        }
        return counts;
    }

    public void ApplyTheme(bool initial = false) {
        if (sudokuData == null) {
            Debug.LogError("[Standalone] ApplyTheme: sudokuData is NULL!");
            return;
        }
        if (sudokuData.themes == null || sudokuData.themes.Length == 0) {
            Debug.LogError("[Standalone] ApplyTheme: sudokuData.themes is EMPTY or NULL!");
            return;
        }
        if (sudokuData.selectedThemeIndex < 0 || sudokuData.selectedThemeIndex >= sudokuData.themes.Length) {
            Debug.LogError($"[Standalone] ApplyTheme: selectedThemeIndex ({sudokuData.selectedThemeIndex}) is OUT OF RANGE (0 to {sudokuData.themes.Length - 1})!");
            return;
        }
        var theme = sudokuData.themes[sudokuData.selectedThemeIndex];
        Debug.Log($"[Standalone] ApplyTheme: theme={theme.themeName}, initial={initial}");
        
        // 登録済みのセルのみを更新（Findを排除）
        foreach (var cell in serializedCells) {
            if (cell != null) cell.RefreshUI(initial);
        }
    }

    public void BackToMenu() {
        Debug.Log("<color=yellow>[TRACE] SudokuGameStandalone.BackToMenu: Loading MenuScene...</color>");
        UnityEngine.SceneManagement.SceneManager.LoadScene("MenuScene");
    }
}
