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
    public List<Button> inputButtons = new List<Button>();

    [Header("State")]
    private int[,] puzzleGrid;
    private int[,] solutionGrid;
    private SudokuCell[,] cells = new SudokuCell[9, 9];
    private SudokuCell selectedCell;

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
        // ゲームの生成
        InitializeGame();
        
        // テーマの初期適用
        ApplyTheme(true);
    }

    private void Update() {
        // ESCキーでメニューに戻る（確実な復帰用）
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) {
            BackToMenu();
        }
    }

    private void RebuildGridFromSerialized() {
        if (serializedCells.Count != 81) {
            Debug.LogWarning($"[Standalone] Serialized cells count is {serializedCells.Count}, expected 81. Grid might be incomplete.");
        }

        foreach (var cell in serializedCells) {
            if (cell == null) continue;
            
            // 名前のパース（行_列）から座標を特定
            string[] parts = cell.name.Split('_');
            int r = -1, c = -1;
            if (parts.Length >= 3 && int.TryParse(parts[1], out r) && int.TryParse(parts[2], out c)) {
                if (r >= 0 && r < 9 && c >= 0 && c < 9) {
                    cells[r, c] = cell;
                    
                    // クリックイベントの静的登録（インスペクターで設定済みなら不要だが、確実性のために残す）
                    var btn = cell.GetComponent<Button>();
                    if (btn != null) {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => OnCellSelected(cell));
                    }
                }
            }
        }
    }

    private void InitializeGame() {
        // 難易度の取得
        SudokuLogic.Difficulty diff = SudokuGameState.SelectedDifficulty;
        Debug.Log($"[Standalone] Generating new game. Difficulty: {diff}");
        
        var result = SudokuLogic.Generate(diff);
        puzzleGrid = result.puzzle;
        solutionGrid = result.solution;

        // セルへの反映
        for (int r = 0; r < 9; r++) {
            for (int c = 0; c < 9; c++) {
                if (cells[r, c] != null) {
                    cells[r, c].Init(r, c, puzzleGrid[r, c], puzzleGrid[r, c] != 0, null);
                }
            }
        }
    }

    private void OnCellSelected(SudokuCell cell) {
        if (cell == null) return;
        selectedCell = cell;
        UpdateHighlights();
    }

    public void OnInputButtonClicked(int value) {
        if (selectedCell == null || selectedCell.IsPreFilled) return;

        if (value == -2) {
            selectedCell.SetValue(0, false);
        } else if (value >= 1 && value <= 9) {
            selectedCell.SetValue(value, false);
        }

        CheckErrors();
        UpdateHighlights();
        
        if (CheckWin()) {
            Debug.Log("[Standalone] GAME CLEAR!");
        }
    }

    private void UpdateHighlights() {
        for (int r = 0; r < 9; r++) {
            for (int c = 0; c < 9; c++) {
                if (cells[r, c] == null) continue;
                
                bool isSelected = (cells[r, c] == selectedCell);
                bool isRelated = false;
                if (selectedCell != null) {
                    isRelated = (cells[r, c].Row == selectedCell.Row || cells[r, c].Col == selectedCell.Col);
                }
                bool isSameDigit = (selectedCell != null && selectedCell.Value != 0 && cells[r, c].Value == selectedCell.Value);
                
                cells[r, c].SetHighlight(isSelected, isSameDigit, cells[r, c].IsError, isRelated);
            }
        }
        UpdateInputPanelAlpha();
    }

    private void UpdateInputPanelAlpha() {
        float targetAlpha = (selectedCell != null && !selectedCell.IsPreFilled) ? 1.0f : 0.3f;
        foreach (var b in inputButtons) {
            if (b == null) continue;
            var cg = b.GetComponent<CanvasGroup>();
            if (cg == null) cg = b.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = targetAlpha;
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

    public void ApplyTheme(bool initial = false) {
        if (sudokuData == null) return;
        var theme = sudokuData.themes[sudokuData.selectedThemeIndex];
        
        // 登録済みのセルのみを更新（Findを排除）
        foreach (var cell in serializedCells) {
            if (cell != null) cell.RefreshUI(initial);
        }

        // 入力ボタンのテーマ更新
        foreach (var b in inputButtons) {
            if (b == null) continue;
            var display = b.GetComponent<SudokuDigitDisplay>();
            if (display != null) {
                int val = -1;
                if (b.name.Contains("_")) {
                    string suffix = b.name.Split('_')[1];
                    if (suffix == "C") val = -2;
                    else int.TryParse(suffix, out val);
                }
                display.SetDigit(val, theme, initial, true);
            }
        }
    }

    public void BackToMenu() {
        SceneManager.LoadScene("MenuScene");
    }
}
