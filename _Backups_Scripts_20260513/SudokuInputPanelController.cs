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
    private Action<int> onNumberClickedCallback;


    /// <summary>
    /// セルの選択状態や数字のカウントに基づいて、ボタンの有効・無効を更新します。
    /// </summary>
    public void UpdateButtonStates(SudokuBoard board, SudokuCell selectedCell) {
        if (board == null) return;
        
        int[] counts = board.GetNumberCounts();
        
        foreach (var inputBtn in buttons) {
            if (inputBtn.Value >= -2 && inputBtn.Value <= 9) {
                bool isClear = (inputBtn.Value <= 0);
                int count = isClear ? 0 : (inputBtn.Value > 0 ? counts[inputBtn.Value - 1] : 0);
                
                var btnComp = inputBtn.GetComponent<Button>();
                if (btnComp != null) {
                    // 全ての数字が埋まった場合は無効化（Clearは常に選択中なら有効）
                    bool canUse = isClear ? (selectedCell != null) : (count < 9 && selectedCell != null);
                    btnComp.interactable = canUse;
                    
                    var cg = inputBtn.GetComponent<CanvasGroup>();
                    if (cg == null) cg = inputBtn.gameObject.AddComponent<CanvasGroup>();
                    cg.alpha = canUse ? 1.0f : 0.3f;
                }
            }
        }
    }

    /// <summary>
    /// ボタンクリック時の処理をバインドします。
    /// </summary>
    public void Initialize(System.Action<int> onNumberClicked) {
        Debug.Log("[DIAGNOSTIC] SudokuInputPanelController: Initializing Input Buttons...");
        
        // 子要素から全てのSudokuInputButtonを探す
        SudokuInputButton[] buttons = GetComponentsInChildren<SudokuInputButton>(true);
        Debug.Log($"[DIAGNOSTIC]   - Found {buttons.Length} input buttons.");

        foreach (var btn in buttons) {
            var buttonComp = btn.GetComponent<UnityEngine.UI.Button>();
            if (buttonComp != null) {
                buttonComp.onClick.RemoveAllListeners();
                int val = btn.Value;
                buttonComp.onClick.AddListener(() => {
                    Debug.Log($"[DIAGNOSTIC] Input Button Clicked: Value={val}");
                    onNumberClicked?.Invoke(val);
                });
                Debug.Log($"[DIAGNOSTIC]   - Bound Button: {btn.gameObject.name} (Value: {val})");
            } else {
                Debug.LogWarning($"[DIAGNOSTIC]   - Button component missing on {btn.gameObject.name}");
            }
        }
    }

    /// <summary>
    /// 静的に配置された数字ボタンから呼び出されるメソッド。
    /// </summary>
    public void OnNumberButtonClicked(int value) {
        Debug.Log($"[SudokuInputPanelController] Button clicked: {value}");
        onNumberClickedCallback?.Invoke(value);
    }


}
