using UnityEditor;
using UnityEngine;

public class SudokuDebugTools
{
    [MenuItem("Sudoku/Generate Easy Game")]
    public static void GenerateEasy()
    {
        if (SudokuBoard.Instance != null)
        {
            SudokuBoard.Instance.GenerateNewGame(SudokuLogic.Difficulty.Easy);
            Debug.Log("Sudoku: Easy Game Generated via Menu");
        }
        else
        {
            Debug.LogError("Sudoku: SudokuBoard.Instance not found!");
        }
    }
}
