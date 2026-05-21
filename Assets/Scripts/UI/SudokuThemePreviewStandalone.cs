using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/// <summary>
/// 自律型のテーマプレビュー制御クラス。
/// 小窓の中（自身）の表示のみを更新し、シーン全体のテーマには干渉しません。
/// </summary>
public class SudokuThemePreviewStandalone : MonoBehaviour
{
    [Header("Data Source")]
    public SudokuData sudokuData;

    [Header("Components")]
    [SerializeField] private Image _displayImage;
    [SerializeField] private CanvasGroup _imageCanvasGroup;
    [SerializeField] private TextMeshProUGUI _themeNameText;
    [SerializeField] private Button _themeSwitchButton;

    private int _currentThemeIndex = 0;
    private int _currentDigit = 1;
    private Coroutine _previewRoutine;

    private readonly int[] _sovietOrder = { 1, 6, 2, 7, 5, 0, 4, 9, 8, 3 };

    private void Awake() {
        if (sudokuData == null) {
#if UNITY_EDITOR
            sudokuData = UnityEditor.AssetDatabase.LoadAssetAtPath<SudokuData>("Assets/Data/SudokuData.asset");
#endif
        }
        
        if (sudokuData != null) _currentThemeIndex = sudokuData.selectedThemeIndex;

        // テーマ切り替えは SudokuThemeManager 側で一括管理するため、ここでのリスナー登録は不要です。
    }

    private void OnEnable() {
        SudokuThemeManager.OnThemeChanged += HandleThemeChanged;
        SyncWithGlobalTheme();
        StartPreview();
    }

    private void OnDisable() {
        SudokuThemeManager.OnThemeChanged -= HandleThemeChanged;
        StopPreview();
    }

    private void SyncWithGlobalTheme() {
        if (SudokuThemeManager.Instance != null && sudokuData != null) {
            _currentThemeIndex = SudokuGameState.SelectedThemeIndex;
            UpdateThemeNameUI();
        }
    }

    private void HandleThemeChanged(SudokuData.SudokuTheme theme, bool isInitial) {
        SyncWithGlobalTheme();
        StartPreview();
    }

    public void StartPreview() {
        StopPreview();
        _currentDigit = 1;
        _previewRoutine = StartCoroutine(PreviewLoop());
    }

    public void StopPreview() {
        if (_previewRoutine != null) {
            StopCoroutine(_previewRoutine);
            _previewRoutine = null;
        }
    }


    private void UpdateThemeNameUI() {
        if (_themeNameText != null && sudokuData != null) {
            var theme = sudokuData.themes[_currentThemeIndex];
            _themeNameText.text = theme.themeName.ToUpper();
            _themeNameText.color = theme.textColor;
        }
    }

    private IEnumerator PreviewLoop() {
        while (true) {
            yield return StartCoroutine(AnimateToDigit(_currentDigit));
            yield return new WaitForSeconds(1.0f);
            _currentDigit = (_currentDigit + 1) % 10;
        }
    }

    private IEnumerator AnimateToDigit(int target) {
        var theme = sudokuData.themes[_currentThemeIndex];
        switch (theme.displayType) {
            case SudokuData.ThemeDisplayType.Nixie:
                yield return StartCoroutine(AnimateNixie(target, theme));
                break;
            case SudokuData.ThemeDisplayType.Mechanical:
                yield return StartCoroutine(AnimateMechanical(target, theme));
                break;
            case SudokuData.ThemeDisplayType.Roulette:
                yield return StartCoroutine(AnimateRoulette(target, theme));
                break;
            default:
                ApplySprite(target.ToString(), theme);
                yield return null;
                break;
        }
    }

    private IEnumerator AnimateNixie(int target, SudokuData.SudokuTheme theme) {
        int targetIdx = System.Array.IndexOf(_sovietOrder, target);
        if (targetIdx == -1) {
            ApplySprite(target.ToString(), theme);
            yield break;
        }

        for (int i = _sovietOrder.Length - 1; i >= targetIdx; i--) {
            int val = _sovietOrder[i];
            ApplySprite(val.ToString(), theme);
            float brightness = Mathf.Lerp(0.8f, 1.0f, (float)(_sovietOrder.Length - 1 - i) / (_sovietOrder.Length - 1));
            if (_displayImage != null) {
                Color baseColor = theme.useOriginalSpriteColor ? Color.white : theme.textColor;
                _displayImage.color = baseColor * brightness;
            }
            yield return new WaitForSeconds(0.03f);
        }
    }

    private IEnumerator AnimateMechanical(int target, SudokuData.SudokuTheme theme) {
        int prev = (_currentDigit == 0) ? 9 : (_currentDigit - 1);
        string prevStr = prev.ToString();
        string targetStr = target.ToString();

        string[] angles = { "45", "90", "135" };
        foreach (var angle in angles) {
            string spriteName = $"Anim_{prevStr}_{targetStr}_{angle}";
            Sprite s = FindSprite(spriteName, theme);
            if (s != null && _displayImage != null) {
                _displayImage.sprite = s;
                if (_imageCanvasGroup != null) _imageCanvasGroup.alpha = 1f;
                _displayImage.color = theme.useOriginalSpriteColor ? Color.white : theme.textColor;
                yield return new WaitForSeconds(0.05f);
            }
        }
        ApplySprite(targetStr, theme);
    }

    private IEnumerator AnimateRoulette(int target, SudokuData.SudokuTheme theme) {
        if (target > 0) {
            for (int val = 1; val <= target; val++) {
                ApplySprite(val.ToString(), theme);
                float progress = (float)(val - 1) / target;
                float delay = Mathf.Lerp(0.04f, 0.25f, progress);
                yield return new WaitForSeconds(delay);
            }
        } else {
            ApplySprite("0", theme);
        }
    }

    private void ApplySprite(string spriteName, SudokuData.SudokuTheme theme) {
        Sprite s = FindSprite(spriteName, theme);
        if (_displayImage != null) {
            if (s != null) {
                _displayImage.sprite = s;
                if (_imageCanvasGroup != null) _imageCanvasGroup.alpha = 1f;
            } else {
                if (_imageCanvasGroup != null) _imageCanvasGroup.alpha = 0f;
            }
            Color baseColor = theme.useOriginalSpriteColor 
                ? Color.white 
                : theme.textColor;
            _displayImage.color = baseColor;
        }
    }

    private Sprite FindSprite(string name, SudokuData.SudokuTheme theme) {
        if (theme.sprites == null) return null;
        foreach (var s in theme.sprites) {
            if (s != null && (s.name == name || s.name.EndsWith("_" + name) || s.name.EndsWith(name))) return s;
        }
        if (theme.allSprites != null) {
            foreach (var s in theme.allSprites) {
                if (s != null && (s.name == name || s.name.EndsWith("_" + name) || s.name.EndsWith(name))) return s;
            }
        }
        return null;
    }
}
