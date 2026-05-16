using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// メニュー画面（MenuPanel）内のボタン入力を制御するクラス。
/// </summary>
public class SudokuMenuController : MonoBehaviour
{
    [Header("Menu Buttons")]
    public Button easyButton;
    public Button normalButton;
    public Button hardButton;
    public Button expertButton;
    public Button themeChangeButton;
    public Toggle unlimitedModeToggle;


    private void Start() {
        // 動的な AddListener は廃止（静的バインドに移行）
    }

    public void OnEasyClicked() {
        Debug.Log("[LIFE-LOG] SudokuMenuController.OnEasyClicked ENTER");
        Debug.Log("[DEBUG] OnEasyClicked called");
        OnDifficultySelected(0);
    }
    public void OnNormalClicked() {
        Debug.Log("[DEBUG] OnNormalClicked called");
        OnDifficultySelected(1);
    }
    public void OnHardClicked() {
        Debug.Log("[DEBUG] OnHardClicked called");
        OnDifficultySelected(2);
    }
    public void OnExpertClicked() {
        Debug.Log("[DEBUG] OnExpertClicked called");
        OnDifficultySelected(3);
    }

    public void OnThemeButtonClicked() => OnThemeChangeClicked();


    private void OnDifficultySelected(int difficultyIndex) {
        bool isUnlimited = unlimitedModeToggle != null && unlimitedModeToggle.isOn;
        Debug.Log($"[DIAGNOSTIC] SudokuMenuController: OnDifficultySelected triggered.");
        Debug.Log($"[DIAGNOSTIC]   - Index: {difficultyIndex} ({(SudokuLogic.Difficulty)difficultyIndex})");
        Debug.Log($"[DIAGNOSTIC]   - Unlimited Mode: {isUnlimited}");
        Debug.Log($"[DIAGNOSTIC]   - UIManager Instance: {(UIManager.Instance != null ? "FOUND" : "NOT FOUND")}");

        if (UIManager.Instance != null) {
            UIManager.Instance.StartGame(difficultyIndex, isUnlimited);
        } else {
            Debug.LogError("[CRITICAL] UIManager is missing in the scene!");
        }
    }

    private void OnThemeChangeClicked() {
        if (SudokuThemeManager.Instance != null) {
            SudokuThemeManager.Instance.CycleTheme();
            SudokuThemeManager.Instance.NotifyThemeChanged(false);
            Debug.Log("[SudokuMenuController] Theme Cycled: " + SudokuThemeManager.Instance.CurrentTheme.themeName);
        }
    }
}
