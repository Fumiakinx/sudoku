using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/// <summary>
/// セルやボタンの数字表示を制御するクラス。
/// ルール（第7条、第9条）に基づき、CanvasGroupによる制御と静的紐付けを徹底しています。
/// </summary>
public class SudokuDigitDisplay : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Image _targetImage;
    [SerializeField] private CanvasGroup _imageCanvasGroup;
    [SerializeField] private TextMeshProUGUI _textLabel;
    [SerializeField] private CanvasGroup _labelCanvasGroup;
    
    public Image baseImage; // 背景Image（ボタンの土台など）

    [Header("Settings")]
    public float animationSpeed = 0.5f;
    public bool HideZero = false; // 0を表示しない（ブランクにする）設定
    private int _lastValue = -1;
    private static System.Collections.Generic.Dictionary<string, Sprite> _spriteCache = new System.Collections.Generic.Dictionary<string, Sprite>();
    private float _currentAlpha = 1.0f; // 画像の透明度（背面のハイライトを透かすため）

    public void SetDigit(int value, SudokuData.SudokuTheme theme, bool instant = false, bool forceRefresh = false) {
        if (string.IsNullOrEmpty(theme.themeName)) {
            if (SudokuThemeManager.Instance != null) {
                theme = SudokuThemeManager.Instance.CurrentTheme;
            }
        }
        // Log removed to prevent console flood

        if (instant) {
            _lastValue = value;
            ApplyVisual(value, theme);
        } else {
            StopAllCoroutines();
            StartCoroutine(AnimateDigit(value, theme));
        }
    }

    /// <summary>
    /// ハイライト色などを数字表示の背景（baseImage）に反映させます。
    /// どのテーマでも共通で使用できる汎用的な背景色設定です。
    /// </summary>
    public void SetBackgroundColor(Color color) {
        if (baseImage != null) {
            baseImage.color = color;
        }
    }

    /// <summary>
    /// 背面のハイライトを透かせるため、画像の透明度を制御します
    /// </summary>
    public void SetImageAlpha(float alpha) {
        _currentAlpha = alpha;
        // 即座に反映
        if (_targetImage != null && _targetImage.enabled) {
            Color c = _targetImage.color;
            c.a = _currentAlpha;
            _targetImage.color = c;
        }
    }

    private IEnumerator AnimateDigit(int value, SudokuData.SudokuTheme theme) {
        try {
            if (theme.displayType == SudokuData.ThemeDisplayType.Normal || 
                theme.displayType == SudokuData.ThemeDisplayType.LED7Seg) {
                ApplyVisual(value, theme);
                yield return null;
            } else if (theme.displayType == SudokuData.ThemeDisplayType.Nixie) {
                int[] sovietOrder = { 1, 6, 2, 7, 5, 0, 4, 9, 8, 3 }; 
                int targetPos = System.Array.IndexOf(sovietOrder, value);
                for (int i = sovietOrder.Length - 1; i >= targetPos; i--) {
                    float brightness = Mathf.Lerp(0.8f, 1.0f, (float)(sovietOrder.Length - 1 - i) / (sovietOrder.Length - 1));
                    ApplyVisual(sovietOrder[i], theme, brightness);
                    yield return new WaitForSeconds(0.03f);
                }
            } else if (theme.displayType == SudokuData.ThemeDisplayType.Mechanical || 
                       theme.displayType == SudokuData.ThemeDisplayType.FlipFlap) {
                
                // 初回などで過去値がない場合は便宜上現在値を遷移前とする
                int prev = _lastValue >= 0 ? _lastValue : value;
                string prevStr = prev.ToString();
                string targetStr = value.ToString();
                string[] angles = { "45", "90", "135" };

                // 途中コマの画像を順次表示
                foreach (var angle in angles) {
                    string spriteName = $"Anim_{prevStr}_{targetStr}_{angle}";
                    Sprite s = FindSpriteInTheme(theme, spriteName);
                    
                    if (s != null && _targetImage != null) {
                        _targetImage.enabled = true;
                        _targetImage.sprite = s;
                        
                        // ベース色とアルファ値を適用
                        Color baseColor = theme.textColor;
                        baseColor.a *= _currentAlpha;
                        _targetImage.color = baseColor;
                        
                        if (_textLabel != null) _textLabel.enabled = false;
                        
                        yield return new WaitForSeconds(0.05f);
                    }
                }
                
                // 最後に目標の数字を表示する
                ApplyVisual(value, theme);
            }
        } finally {
            _lastValue = value;
        }
    }

    private Sprite GetThemeSprite(int value, SudokuData.SudokuTheme theme) {
        if (value == -2) return null; // Clear用画像は使わず、常にテキスト表示させる
        if (value == 10) return FindSpriteInTheme(theme, "Blank");
        if (value <= 0) return FindSpriteInTheme(theme, HideZero ? "Blank" : "0");
        return FindSpriteInTheme(theme, value.ToString());
    }

    private Sprite FindSpriteInTheme(SudokuData.SudokuTheme theme, string spriteName) {
        if (theme.sprites == null) return null;
        
        string cacheKey = $"{theme.themeName}_{spriteName}";
        if (_spriteCache.TryGetValue(cacheKey, out Sprite cached)) return cached;

        foreach (var s in theme.sprites) {
            if (s != null && s.name.IndexOf(spriteName, System.StringComparison.OrdinalIgnoreCase) >= 0) {
                _spriteCache[cacheKey] = s;
                return s;
            }
        }
        
        // 途中コマ用（Anim_）に allSprites からも検索する
        if (theme.allSprites != null) {
            foreach (var s in theme.allSprites) {
                if (s != null && s.name.IndexOf(spriteName, System.StringComparison.OrdinalIgnoreCase) >= 0) {
                    _spriteCache[cacheKey] = s;
                    return s;
                }
            }
        }
        
        return null;
    }

    private void ApplyVisual(int value, SudokuData.SudokuTheme theme, float brightness = 1.0f) {
        int displayValue = value;
        if (HideZero && value == 0) displayValue = 10; 
        if (value < 0 && value != -2) displayValue = 10;
        
        Sprite s = GetThemeSprite(displayValue, theme);
        
        if (s == null) {
            if (_targetImage != null) {
                _targetImage.sprite = null;
                // テキスト表示時は、前面の画像領域を非表示にして「白い四角」を出さないようにする
                _targetImage.enabled = false; 
            }
            // ベベルを消さないため、CanvasGroup.alpha = 0 の操作は行わない

            if (_textLabel != null) {
                // 数字が有効な場合のみラベルを表示
                bool shouldShowLabel = (displayValue != 10 && displayValue > 0) || displayValue == -2;
                _textLabel.enabled = shouldShowLabel;
                
                if (shouldShowLabel) {
                    if (displayValue == -2) {
                        _textLabel.text = "<size=150%>C</size>"; // 文字を少し大きく表示
                    } else {
                        _textLabel.text = displayValue.ToString();
                    }
                    _textLabel.color = theme.textColor;
                }
            }
        } else {
            if (_targetImage != null) {
                _targetImage.enabled = true;
                _targetImage.sprite = s;
                
                // メニューのサンプル（PreviewStandalone）に合わせて、Nixie/LED7Seg時は基本輝度を0.8倍に調整
                float themeAdjust = (theme.displayType == SudokuData.ThemeDisplayType.Nixie || 
                                     theme.displayType == SudokuData.ThemeDisplayType.LED7Seg) ? 0.8f : 1.0f;
                // 基本色を設定し、指定されたアルファ値を適用する
                Color baseColor = theme.textColor * brightness * themeAdjust;
                baseColor.a *= _currentAlpha;
                _targetImage.color = baseColor;
                _targetImage.rectTransform.localScale = Vector3.one;
            }

            // 画像表示時はテキストを無効化（明るすぎ防止、重なり防止）
            if (_textLabel != null) _textLabel.enabled = false;
        }
    }
}
