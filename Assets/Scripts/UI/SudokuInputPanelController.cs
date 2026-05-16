using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

/// <summary>
/// 数字入力パネル（1-9ボタン）のボタン有効化・無効化などのUI制御を担当するクラス。
/// </summary>
public class SudokuInputPanelController : MonoBehaviour
{
    [Header("Input Buttons")]
    public List<SudokuInputButton> buttons = new List<SudokuInputButton>();

    private void Awake() {
        int count = (buttons != null) ? buttons.Count : -1;
        Debug.Log($"[Controller-Life] Awake: {gameObject.name}, ButtonsListCount: {count}");
        if (buttons != null) {
            for (int i = 0; i < buttons.Count; i++) {
                if (buttons[i] == null) Debug.LogError($"[Controller-Life] Button at index {i} is NULL!");
            }
        }
    }


    /// <summary>
    /// セルの選択状態や数字のカウントに基づいて、ボタンの有効・無効を更新します。
    /// </summary>
    public void UpdateButtonStates(SudokuGameStandalone board, SudokuCell selectedCell) {
        if (board == null) return;
        
        foreach (var inputBtn in buttons) {
            if (inputBtn.Value >= -2 && inputBtn.Value <= 9) {
                var btnComp = inputBtn.GetComponent<Button>();
                if (btnComp != null) {
                    bool isClear = (inputBtn.Value <= 0);
                    
                    // 【修正】ヒントにならないよう、数字のカウント（9個制限）による無効化を廃止
                    // セルが選択されており、かつ書き換え可能なセルであればボタンを有効にする
                    bool canUse = selectedCell != null && !selectedCell.IsPreFilled;
                    btnComp.interactable = canUse;
                    
                    var cg = inputBtn.GetComponent<CanvasGroup>();
                    if (cg == null) {
                        Debug.LogError($"[SudokuInputPanelController] CanvasGroup missing on {inputBtn.name}. Please add it to the prefab statically.");
                        continue;
                    }
                    // 【修正】常に一定の明るさを維持（見た目によるヒントを完全に排除）
                    cg.alpha = 1.0f;
                }
            }
        }
    }
}
