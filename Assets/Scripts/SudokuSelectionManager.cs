using UnityEngine;

/// <summary>
/// セルの選択状態とハイライト（関連セル、同じ数字のセル等）の計算を担当するクラス
/// </summary>
public class SudokuSelectionManager : MonoBehaviour {
    public void UpdateHighlights(SudokuCell[,] cells, SudokuCell selectedCell) {
        if (cells == null || cells.GetLength(0) != 9 || cells.GetLength(1) != 9) return;
        
        int selR = (selectedCell != null) ? selectedCell.Row : -1;
        int selC = (selectedCell != null) ? selectedCell.Col : -1;
        int selVal = (selectedCell != null) ? selectedCell.Value : 0;

        for (int r = 0; r < 9; r++) {
            for (int c = 0; c < 9; c++) {
                var cell = cells[r, c];
                if (cell == null) continue;

                bool isSelected = (r == selR && c == selC);
                bool isRelated = (selR >= 0 && selC >= 0) && (r == selR || c == selC || (r / 3 == selR / 3 && c / 3 == selC / 3));
                bool isSameDigit = (selVal != 0 && cell.Value == selVal);
                cell.SetHighlight(isSelected, isSameDigit, false, isRelated);
            }
        }
    }
}
