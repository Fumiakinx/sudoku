using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Led7SegDigit : MonoBehaviour
{
    private Image displayImage;
    private int _currentValue = -1;
    public bool hideZero = false; // 0を空白（Blank）として扱うかどうか
    private Sprite[] _sprites;

    private void Awake() {
        Setup();
    }

    public void Setup() {
        if (displayImage == null) displayImage = GetComponent<Image>();
        if (displayImage == null) {
            displayImage = gameObject.AddComponent<Image>();
        }
        displayImage.preserveAspect = true;
    }

    public void SetSprites(Sprite[] sprites) {
        _sprites = sprites;
        UpdateVisuals();
    }

    private void OnEnable() {
        UpdateVisuals();
    }

    public void SetValue(int value, bool immediate = false) {
        if (_currentValue == value && !immediate) return;
        
        _currentValue = value;
        UpdateVisuals();
    }

    public void RefreshUI() {
        UpdateVisuals();
    }

    public void UpdateVisuals() {
        if (!enabled) return;
        if (displayImage == null) displayImage = GetComponent<Image>();
        
        var styler = SudokuUIStyler.Instance;
        if (styler == null) return;
        
        var sprites = _sprites;
        if (sprites == null) sprites = styler.Led7SegSprites;
        if (sprites == null || sprites.Length < 11) return;

        if (displayImage != null) {
            var theme = styler.CurrentTheme;
            Color ledColor = new Color(theme.textColor.r * 0.7f, theme.textColor.g * 0.7f, theme.textColor.b * 0.7f, theme.textColor.a);
            
            bool isVisible = (_currentValue > 0 && _currentValue <= 9) || (_currentValue == 0 && !hideZero);

            if (isVisible) {
                displayImage.sprite = sprites[_currentValue >= 0 ? _currentValue : 0];
                displayImage.color = ledColor;
                
                var shadow = GetComponent<Shadow>();
                if (shadow != null) shadow.effectColor = new Color(ledColor.r, ledColor.g, ledColor.b, 0.4f);
                var outline = GetComponent<Outline>();
                if (outline != null) outline.effectColor = new Color(ledColor.r, ledColor.g, ledColor.b, 0.2f);
            } else {
                displayImage.sprite = sprites[10];
                displayImage.color = new Color(ledColor.r, ledColor.g, ledColor.b, 0.01f);
                
                var shadow = GetComponent<Shadow>();
                if (shadow != null) shadow.effectColor = Color.clear;
                var outline = GetComponent<Outline>();
                if (outline != null) outline.effectColor = Color.clear;
            }
        }
    }
}
