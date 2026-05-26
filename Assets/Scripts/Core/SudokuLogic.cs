using System;
using System.Collections.Generic;
using UnityEngine;

public static class SudokuLogic
{
    public const int GridSize = 9;
    public const int SubGridSize = 3;

    public enum Difficulty
    {
        Easy,
        Normal,
        Hard,
        Expert
    }

    // 81段階の再帰レベルそれぞれで使用する、1〜9の数字配列をあらかじめ静的バッファとして用意 (GCアロケーションの防止)
    private static readonly int[][] recursionBuffer = new int[82][];

    static SudokuLogic() {
        // 静的コンストラクタで、各深さ用のサイズ9の配列を事前にアロケーションしておく
        for (int i = 0; i < recursionBuffer.Length; i++) {
            recursionBuffer[i] = new int[9];
        }
    }

    public static int[,] GenerateFullGrid()
    {
        int[,] grid = new int[GridSize, GridSize];
        stepCount = 0;
        FillGrid(grid, 0); // 初期深さ 0 で開始
        return grid;
    }

    public static int[,] GeneratePuzzle(int[,] fullGrid, Difficulty difficulty)
    {
        int[,] puzzle = (int[,])fullGrid.Clone();
        int holes = GetHoleCount(difficulty);
        
        // 81個のセルインデックス配列を固定確保
        int[] positions = new int[GridSize * GridSize];
        for (int i = 0; i < positions.Length; i++) {
            positions[i] = i;
        }

        // Fisher-Yatesシャッフルにより、メモリ割り当て無しでO(N)で完全ランダムソート
        System.Random localRng = new System.Random();
        for (int i = positions.Length - 1; i > 0; i--) {
            int j = localRng.Next(i + 1);
            int temp = positions[i];
            positions[i] = positions[j];
            positions[j] = temp;
        }

        int removed = 0;
        foreach (int pos in positions)
        {
            if (removed >= holes) break;
            int row = pos / GridSize;
            int col = pos % GridSize;
            puzzle[row, col] = 0;
            removed++;
        }
        return puzzle;
    }

    private static int GetHoleCount(Difficulty difficulty)
    {
        return difficulty switch
        {
            Difficulty.Easy => 35,
            Difficulty.Normal => 45,
            Difficulty.Hard => 54,
            Difficulty.Expert => 60,
            _ => 40
        };
    }

    private static System.Random rng = new System.Random();
    private static int stepCount = 0;
    private const int MAX_STEPS = 10000;

    /// <summary>
    /// バックトラッキングを用いて盤面を生成します。
    /// depth 引数を用いて事前確保された recursionBuffer を参照し、GC Alloc を 0 にします。
    /// </summary>
    private static bool FillGrid(int[,] grid, int depth)
    {
        stepCount++;
        if (stepCount > MAX_STEPS) return false;

        for (int row = 0; row < GridSize; row++)
        {
            for (int col = 0; col < GridSize; col++)
            {
                if (grid[row, col] == 0)
                {
                    // 事前確保されたこの深さ用のバッファを使用 (GC Alloc なし)
                    int[] nums = recursionBuffer[depth];
                    
                    // 1〜9の数値をセット
                    for (int i = 0; i < 9; i++) {
                        nums[i] = i + 1;
                    }

                    // フィッシャー–イェーツのシャッフル
                    for (int i = 8; i > 0; i--) {
                        int j = rng.Next(i + 1);
                        int temp = nums[i];
                        nums[i] = nums[j];
                        nums[j] = temp;
                    }

                    // 順に数値をあてはめ再帰検証
                    for (int i = 0; i < 9; i++)
                    {
                        int num = nums[i];
                        if (IsValid(grid, row, col, num))
                        {
                            grid[row, col] = num;
                            if (FillGrid(grid, depth + 1)) return true;
                            grid[row, col] = 0;
                        }
                    }
                    return false; // バックトラッキング
                }
            }
        }
        return true;
    }

    public static (int[,] puzzle, int[,] solution) Generate(Difficulty difficulty)
    {
        Debug.Log($"<color=green>【SudokuLogic】Generate START: {difficulty}</color>");
        for (int retry = 0; retry < 10; retry++) {
            int[,] fullGrid = new int[GridSize, GridSize];
            stepCount = 0;
            Debug.Log($"[SudokuLogic] Attempt {retry + 1}/10 - Filling grid...");
            if (FillGrid(fullGrid, 0)) { // 初期深さ 0 で開始
                Debug.Log($"[SudokuLogic] Grid filled successfully in {stepCount} steps.");
                int[,] puzzle = GeneratePuzzle(fullGrid, difficulty);
                Debug.Log("<color=green>【SudokuLogic】Generate SUCCESS</color>");
                return (puzzle, fullGrid);
            }
            Debug.LogWarning($"[SudokuLogic] Generate failed (step: {stepCount}), retrying... {retry + 1}/10");
        }
        
        Debug.LogError("[SudokuLogic] Failed to generate a valid Sudoku after 10 retries.");
        Debug.Log("<color=green>【SudokuLogic】Generate FAILED</color>");
        return (new int[9,9], new int[9,9]);
    }

    public static bool IsValid(int[,] grid, int row, int col, int num)
    {
        for (int i = 0; i < GridSize; i++)
        {
            if (grid[row, i] == num || grid[i, col] == num) return false;
        }

        int startRow = (row / SubGridSize) * SubGridSize;
        int startCol = (col / SubGridSize) * SubGridSize;

        for (int i = 0; i < SubGridSize; i++)
        {
            for (int j = 0; j < SubGridSize; j++)
            {
                if (grid[startRow + i, startCol + j] == num) return false;
            }
        }
        return true;
    }
}
