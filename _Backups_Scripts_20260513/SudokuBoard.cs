using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SudokuBoard : MonoBehaviour {
    private static SudokuBoard _instance;
    public static SudokuBoard Instance {
        get {
            if (_instance == null) _instance = FindAnyObjectByType<SudokuBoard>();
            return _instance;
        }
    }
    public Transform gridParent;
    public SudokuCell[,] cells = new SudokuCell[9, 9];
    
    private int[,] puzzle = new int[9, 9];
    private int[,] solution = new int[9, 9];
    private SudokuCell selectedCell;
    public SudokuCell SelectedCell => selectedCell;

    // モジュール化されたコンポーネントへの参照
    private SudokuGridGenerator gridGenerator;
    private SudokuInputHandler inputHandler;
    private SudokuSelectionManager selectionManager;

    void Awake() { 
        if (_instance == null) _instance = this;
        gridGenerator = GetComponent<SudokuGridGenerator>();
        inputHandler = GetComponent<SudokuInputHandler>();
        selectionManager = GetComponent<SudokuSelectionManager>();
        
        if (inputHandler != null) inputHandler.Init(this);
    }

    private void OnEnable() {
        SudokuThemeManager.OnThemeChanged += HandleThemeChanged;
    }

    private void OnDisable() {
        SudokuThemeManager.OnThemeChanged -= HandleThemeChanged;
    }

    private void HandleThemeChanged(SudokuData.SudokuTheme theme, bool immediate) {
        UpdateSelectionVisuals();
    }

    void Start() {
        // 自動生成を無効化。ユーザーの操作（UIManager経由）でのみ生成を開始する。
        // これにより、起動時のクラッシュリスクを最小限に抑えます。
    }

    public void GenerateNewGame(SudokuLogic.Difficulty difficulty) {
        Debug.Log($"[SudokuBoard] GenerateNewGame called with difficulty: {difficulty}");
        var result = SudokuLogic.Generate(difficulty);
        puzzle = result.puzzle;
        solution = result.solution;
        
        int count = 0;
        for(int r=0; r<9; r++) for(int c=0; c<9; c++) if (puzzle[r,c] > 0) count++;
        Debug.Log($"[DEBUG] SudokuBoard: Puzzle generated with {count} values. Initializing grid...");



    }


    public void SelectCell(SudokuCell cell) {
        if (cell == null) {
            return;
        }

        if (cell.IsFixed) {
            return;
        }

        selectedCell = cell;
        UpdateSelectionVisuals();

        if (UIManager.Instance != null && UIManager.Instance.inputPanelController != null) {
            UIManager.Instance.inputPanelController.UpdateButtonStates(this, selectedCell);
        }
    }

    public void ClearSelection() {
        selectedCell = null;
        UpdateSelectionVisuals();
        if (UIManager.Instance != null && UIManager.Instance.inputPanelController != null) {
            UIManager.Instance.inputPanelController.UpdateButtonStates(this, null);
        }
    }

    public void UpdateSelectionVisuals() {
        if (selectionManager != null) {
            selectionManager.UpdateHighlights(cells, selectedCell);
        }
    }

    public void InputNumber(int number) {
        Debug.Log($"【トレース3】SudokuBoard: 数字 [{number}] を書き込みます。選択セル: {(selectedCell != null ? selectedCell.name : "null")}");
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;
        if (selectedCell == null) {
            return;
        }

        if (selectedCell.IsFixed) {
            return;
        }

        if (inputHandler != null) {
            inputHandler.HandleInput(selectedCell, number, solution);
        } else {
            selectedCell.SetValue(number);
        }
    }

    public bool CheckWinCondition() {
        foreach (var cell in cells) {
            if (cell == null || cell.Value == 0 || cell.IsError || cell.Value != solution[cell.Row, cell.Col]) return false;
        }
        return true;
    }

    public int[] GetNumberCounts() {
        int[] counts = new int[9];
        for (int r = 0; r < 9; r++) {
            for (int c = 0; c < 9; c++) {
                if (cells[r, c] == null) continue;
                int val = cells[r, c].Value;
                if (val >= 1 && val <= 9) counts[val - 1]++;
            }
        }
        return counts;
    }

    public bool IsPossibleMove(int r, int c, int num) {
        int[,] currentGrid = new int[9, 9];
        for (int row = 0; row < 9; row++) {
            for (int col = 0; col < 9; col++) currentGrid[row, col] = cells[row, col].Value;
        }
        currentGrid[r, c] = 0;
        return SudokuLogic.IsValid(currentGrid, r, c, num);
    }

    public void RefreshVisuals() {
        if (cells == null || cells.GetLength(0) != 9 || cells.GetLength(1) != 9) return;
        for (int r = 0; r < 9; r++) {
            for (int c = 0; c < 9; c++) {
                if (cells[r, c] != null) cells[r, c].RefreshUI();
            }
        }
        UpdateSelectionVisuals();
    }
}
