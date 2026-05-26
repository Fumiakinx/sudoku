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

    private int[] _lastVals = new int[6] { -1, -1, -1, -1, -1, -1 };
    private readonly int[] _timerVals = new int[6]; // キャッシュされた配列 (GC Alloc 排除)
    private int _lastTotalSeconds = -1; // 秒数変更検知用のガード変数
    
    private void Start() {
        // 初期状態の反映
        UpdateTime(0);
    }

    public void UpdateTime(float seconds)
    {
        int totalSeconds = Mathf.FloorToInt(seconds);
        
        // 秒数が実際に変わったフレームの瞬間のみUI更新処理を実行 (CPU負荷を劇的に削減)
        if (totalSeconds == _lastTotalSeconds) return;
        _lastTotalSeconds = totalSeconds;

        int hours = totalSeconds / 3600;
        int minutes = (totalSeconds % 3600) / 60;
        int secs = totalSeconds % 60;

        // キャッシュされた配列を再利用 (GC Alloc 0)
        _timerVals[0] = hours / 10;
        _timerVals[1] = hours % 10;
        _timerVals[2] = minutes / 10;
        _timerVals[3] = minutes % 10;
        _timerVals[4] = secs / 10;
        _timerVals[5] = secs % 10;

        // ログ出力: どの値をどの桁に表示しようとしているか
        // Debug.Log($"[LIFE-LOG] GraphicalTimer.UpdateTime: {hours:D2}:{minutes:D2}:{secs:D2} (Total: {totalSeconds}s)");

        if (digits != null) {
            // Log removed to prevent console flood
            for (int i = 0; i < digits.Length && i < _timerVals.Length; i++) {
                if (digits[i] != null) {
                    // 値が変わった時だけ更新（SetDigit）を1回だけ行う
                    if (_lastVals[i] != _timerVals[i]) {
                        var currentTheme = (SudokuThemeManager.Instance != null) ? SudokuThemeManager.Instance.CurrentTheme : default;
                        digits[i].SetDigit(_timerVals[i], currentTheme, !UseAnimation);
                        _lastVals[i] = _timerVals[i];
                    }
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

}
