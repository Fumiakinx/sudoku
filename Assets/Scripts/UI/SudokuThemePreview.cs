using UnityEngine;
using System.Collections;

/// <summary>
/// メニュー画面でテーマのサンプル表示（0-9のカウントアップ）を制御するクラス。
/// </summary>
public class SudokuThemePreview : MonoBehaviour {
    private SudokuData.SudokuTheme _currentTheme;
    private Coroutine _cycleRoutine;
    private SudokuDigitDisplay _display;

    private void OnEnable() {
        SudokuThemeManager.OnThemeChanged += HandleThemeChanged;
        // 現在のテーマで初期化
        if (SudokuThemeManager.Instance != null) {
            HandleThemeChanged(SudokuThemeManager.Instance.CurrentTheme, true);
        }
    }

    private void OnDisable() {
        SudokuThemeManager.OnThemeChanged -= HandleThemeChanged;
        StopCycle();
    }

    private void HandleThemeChanged(SudokuData.SudokuTheme theme, bool isInitial) {
        _currentTheme = theme;
        StopCycle();
        if (gameObject.activeInHierarchy) {
            _cycleRoutine = StartCoroutine(CycleDigits());
        }
    }

    private void StopCycle() {
        if (_cycleRoutine != null) {
            StopCoroutine(_cycleRoutine);
            _cycleRoutine = null;
        }
    }

    private IEnumerator CycleDigits() {
        int[] sequence = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0 };
        int seqIdx = 0;

        if (_display == null) {
            _display = GetComponentInChildren<SudokuDigitDisplay>();
            Debug.Log($"[DIAGNOSTIC] SudokuThemePreview: Searching for DigitDisplay -> {(_display != null ? "FOUND" : "NOT FOUND")}");
        }

        while (true) {
            int targetDigit = sequence[seqIdx];
            Debug.Log($"[DIAGNOSTIC] SudokuThemePreview: Cycle Step -> Target: {targetDigit}, Theme: {_currentTheme.themeName}");
            
            if (_display != null) {
                _display.HideZero = false;
                _display.SetDigit(targetDigit, _currentTheme, false);
            } else {
                Debug.LogWarning("[DIAGNOSTIC] SudokuThemePreview: _display is NULL, cannot update digit.");
            }
            
            seqIdx = (seqIdx + 1) % sequence.Length;
            yield return new WaitForSeconds(1.0f);
        }
    }
}
