using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// セルの選択状態とハイライト（関連セル、同じ数字のセル等）の計算を担当するクラス
/// </summary>
public class SudokuSelectionManager : MonoBehaviour {
    public void UpdateHighlights(SudokuCell[,] cells, SudokuCell selectedCell) {
        if (selectedCell != null) {
            Debug.Log($"[DIAGNOSTIC] SudokuSelectionManager: Updating Highlights for Selected Cell ({selectedCell.Row}, {selectedCell.Col})");
        } else {
            Debug.Log("[DIAGNOSTIC] SudokuSelectionManager: Clearing All Highlights (SelectedCell is NULL)");
        }

        foreach (var cell in cells) {
            if (cell == null) continue;

            bool isSelected = (cell == selectedCell);
            bool isRelated = selectedCell != null && 
                            (cell.Row == selectedCell.Row || cell.Col == selectedCell.Col || 
                             (cell.Row / 3 == selectedCell.Row / 3 && cell.Col / 3 == selectedCell.Col / 3));
            bool isSameValue = selectedCell != null && selectedCell.Value != 0 && cell.Value == selectedCell.Value;
            bool isError = cell.IsError;

            // 引数順序: (isSelected, isSameDigit, isError, isRelated)
            cell.SetHighlight(isSelected, isSameValue, isError, isRelated);
        }
    }
}
