using System;
using System.Collections.Generic;
using System.Linq;
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

    public static int[,] GenerateFullGrid()
    {
        int[,] grid = new int[GridSize, GridSize];
        FillGrid(grid);
        return grid;
    }

    public static int[,] GeneratePuzzle(int[,] fullGrid, Difficulty difficulty)
    {
        int[,] puzzle = (int[,])fullGrid.Clone();
        int holes = GetHoleCount(difficulty);
        
        List<int> positions = Enumerable.Range(0, GridSize * GridSize).ToList();
        System.Random rng = new System.Random();
        positions = positions.OrderBy(x => rng.Next()).ToList();

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

    private static bool FillGrid(int[,] grid)
    {
        stepCount++;
        if (stepCount > MAX_STEPS) return false;

        for (int row = 0; row < GridSize; row++)
        {
            for (int col = 0; col < GridSize; col++)
            {
                if (grid[row, col] == 0)
                {
                    List<int> nums = Enumerable.Range(1, 9).ToList();
                    // フィッシャー–イェーツのシャッフル
                    for (int i = nums.Count - 1; i > 0; i--) {
                        int j = rng.Next(i + 1);
                        int temp = nums[i];
                        nums[i] = nums[j];
                        nums[j] = temp;
                    }

                    foreach (int num in nums)
                    {
                        if (IsValid(grid, row, col, num))
                        {
                            grid[row, col] = num;
                            if (FillGrid(grid)) return true;
                            grid[row, col] = 0;
                        }
                    }
                    return false;
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
            if (FillGrid(fullGrid)) {
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
