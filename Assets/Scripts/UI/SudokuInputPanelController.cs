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
        
        int[] counts = board.GetNumberCounts();
        
        foreach (var inputBtn in buttons) {
            if (inputBtn.Value >= -2 && inputBtn.Value <= 9) {
                var btnComp = inputBtn.GetComponent<Button>();
                if (btnComp != null) {
                    Debug.Log($"[Btn-Check] Controller accessing {inputBtn.name}: ButtonEnabled={btnComp.enabled}");
                }

                bool isClear = (inputBtn.Value <= 0);
                int count = isClear ? 0 : (inputBtn.Value > 0 ? counts[inputBtn.Value - 1] : 0);
                
                btnComp = inputBtn.GetComponent<Button>();
                if (btnComp != null) {
                    // 全ての数字が埋まった場合は無効化（Clearは常に選択中なら有効）
                    bool canUse = selectedCell != null && !selectedCell.IsPreFilled && (isClear || count < 9);
                    
                    if (btnComp.interactable != canUse) {
                        Debug.Log($"[DIAGNOSTIC] InputButton {inputBtn.name} (Value:{inputBtn.Value}) interactable changed: {btnComp.interactable} -> {canUse}");
                    }
                    
                    btnComp.interactable = canUse;
                    
                    var cg = inputBtn.GetComponent<CanvasGroup>();
                    if (cg == null) {
                        Debug.LogError($"[SudokuInputPanelController] CanvasGroup missing on {inputBtn.name}. Please add it to the prefab statically.");
                        continue;
                    }
                    cg.alpha = canUse ? 1.0f : 0.3f;
                }
            }
        }
    }
}
