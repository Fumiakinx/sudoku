using UnityEngine;
using TMPro;

/// <summary>
/// ゲーム中のタイマー表示を制御するクラス。
/// ルールに基づき、静的紐付けとクリーンなプロパティ更新のみを行います。
/// </summary>
public class GraphicalTimer : MonoBehaviour
{
    [SerializeField] public bool UseAnimation = true;
    
    [Header("Static References")]
    [SerializeField] private SudokuDigitDisplay[] digits;
    [SerializeField] private TextMeshProUGUI[] colons;
    
    private void Start() {
        // 初期状態の反映
        UpdateTime(0);
    }

    public void UpdateTime(float seconds)
    {
        int totalSeconds = Mathf.FloorToInt(seconds);
        int hours = totalSeconds / 3600;
        int minutes = (totalSeconds % 3600) / 60;
        int secs = totalSeconds % 60;

        int[] vals = new int[6];
        vals[0] = hours / 10;
        vals[1] = hours % 10;
        vals[2] = minutes / 10;
        vals[3] = minutes % 10;
        vals[4] = secs / 10;
        vals[5] = secs % 10;

        // ログ出力: どの値をどの桁に表示しようとしているか
        // Debug.Log($"[LIFE-LOG] GraphicalTimer.UpdateTime: {hours:D2}:{minutes:D2}:{secs:D2} (Total: {totalSeconds}s)");

        if (digits != null) {
            // Log removed to prevent console flood
            for (int i = 0; i < digits.Length && i < vals.Length; i++) {
                if (digits[i] != null) {
                    var currentTheme = (SudokuThemeManager.Instance != null) ? SudokuThemeManager.Instance.CurrentTheme : default;
                    digits[i].SetDigit(vals[i], currentTheme, true);
                } else {
                    Debug.LogWarning($"[TIMER-ERROR] Digit_{i} is NULL in GraphicalTimer array!");
                }
            }
        } else {
            Debug.LogError("[TIMER-ERROR] GraphicalTimer digits array is NULL!");
        }

        // コロンの表示と色同期
        if (colons != null) {
            var currentTheme = (SudokuThemeManager.Instance != null) ? SudokuThemeManager.Instance.CurrentTheme : default;
            foreach (var colon in colons) {
                if (colon != null) {
                    colon.enabled = true;
                    if (!string.IsNullOrEmpty(currentTheme.themeName)) {
                        colon.color = currentTheme.textColor;
                    }
                }
            }
        }
    }

    private void SetDigit(int index, int val, SudokuData.SudokuTheme theme) {
        if (index < digits.Length) {
            var d = digits[index];
            if (d != null) {
                d.HideZero = false;
                d.SetDigit(val, theme, !UseAnimation, false);
            }
        }
    }
}
