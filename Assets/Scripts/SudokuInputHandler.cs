using System.Collections;
using UnityEngine;

/// <summary>
/// ユーザー入力の正誤判定とフィードバック（インジケータ表示）を担当するクラス
/// </summary>
public class SudokuInputHandler : MonoBehaviour {
    private SudokuBoard board;
    private TMPro.TextMeshProUGUI globalIndicator;
    private Coroutine activeInputRoutine;

    public void Init(SudokuBoard board) {
        this.board = board;
    }

    public void HandleInput(SudokuCell selectedCell, int number, int[,] solution) {
        if (selectedCell == null || selectedCell.IsFixed) return;

        if (number == 0) {
            if (selectedCell.IsError) {
                selectedCell.IsError = false;
                selectedCell.SetValue(0);
                board.UpdateSelectionVisuals();
            }
            return;
        }

        if (activeInputRoutine != null) {
            StopCoroutine(activeInputRoutine);
            if (globalIndicator != null) globalIndicator.gameObject.SetActive(false);
        }

        activeInputRoutine = StartCoroutine(ProcessInput(selectedCell, number, solution));
    }

    private IEnumerator ProcessInput(SudokuCell cell, int number, int[,] solution) {
        bool isCorrect = (solution[cell.Row, cell.Col] == number);
        ShowGlobalIndicator(isCorrect);
        
        cell.IsError = !isCorrect;
        cell.SetValue(number, false);
        
        yield return new WaitForSeconds(0.6f);
        
        if (!isCorrect) {
            if (GameManager.Instance != null) GameManager.Instance.OnMistake();
        } else {
            if (board.CheckWinCondition() && GameManager.Instance != null) GameManager.Instance.OnWin();
        }
        
        if (globalIndicator != null) globalIndicator.gameObject.SetActive(false);
        board.UpdateSelectionVisuals();
        activeInputRoutine = null;
    }

    private void ShowGlobalIndicator(bool isCorrect) {
        if (globalIndicator == null) {
            var boardPanel = board.gridParent.parent; 
            GameObject go = new GameObject("GlobalIndicator", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            go.transform.SetParent(boardPanel, false);
            globalIndicator = go.GetComponent<TMPro.TextMeshProUGUI>();
            globalIndicator.alignment = TMPro.TextAlignmentOptions.Center;
            globalIndicator.fontSize = 1200;
            globalIndicator.fontStyle = TMPro.FontStyles.Bold;
            globalIndicator.raycastTarget = false;
            
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
        
        globalIndicator.text = isCorrect ? "○" : "×";
        globalIndicator.color = isCorrect ? new Color(0, 1, 0, 0.8f) : new Color(1, 0, 0, 0.8f);
        globalIndicator.gameObject.SetActive(true);
        globalIndicator.transform.SetAsLastSibling(); 
    }
}
