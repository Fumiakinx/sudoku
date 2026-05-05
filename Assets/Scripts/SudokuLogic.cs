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

    private static bool FillGrid(int[,] grid)
    {
        for (int row = 0; row < GridSize; row++)
        {
            for (int col = 0; col < GridSize; col++)
            {
                if (grid[row, col] == 0)
                {
                    List<int> nums = Enumerable.Range(1, 9).ToList();
                    System.Random rng = new System.Random();
                    nums = nums.OrderBy(x => rng.Next()).ToList();

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
        int[,] fullGrid = GenerateFullGrid();
        int[,] puzzle = GeneratePuzzle(fullGrid, difficulty);
        return (puzzle, fullGrid);
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
