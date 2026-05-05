using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class FlipFlapDisplay : MonoBehaviour
{
    public Sprite[] topSprites; 
    public Sprite[] bottomSprites;
    public bool hideZero = false;
    
    // インターフェース互換性のためのプロパティ
    private Color _textColor = Color.white;
    public Color textColor {
        get => _textColor;
        set {
            _textColor = value;
            ApplyColors();
        }
    }

    private Color _panelColor = Color.white;
    public Color panelColor {
        get => _panelColor;
        set {
            _panelColor = value;
            ApplyColors();
        }
    }

    public int TargetValue { get; private set; }

    public void SetSprites(Sprite[] tops, Sprite[] bottoms) {
        topSprites = tops;
        bottomSprites = bottoms;
    }

    public void SetAlpha(float alpha) {
        var c = textColor;
        c.a = alpha;
        textColor = c;
    }

    public void Initialize() {
        RefreshDisplay(0);
    }

    public void SetValue(int value, bool immediate = false) {
        TargetValue = value;
        RefreshDisplay(value);
    }

    private void RefreshDisplay(int value) {
        // 1. 全削除（ゾンビ排除）
        for (int i = transform.childCount - 1; i >= 0; i--) {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        if (value < 0) value = 0;
        bool show = !(value <= 0 && hideZero);

        if (show && topSprites != null && topSprites.Length >= 10 && bottomSprites != null && bottomSprites.Length >= 10) {
            int idx = (value == 0) ? 9 : Mathf.Clamp(value - 1, 0, 9);
            
            // 2. 上半分
            var topGo = new GameObject("TopHalf", typeof(RectTransform), typeof(Image));
            topGo.transform.SetParent(transform, false);
            var topRT = topGo.GetComponent<RectTransform>();
            topRT.anchorMin = new Vector2(0, 0.5f); topRT.anchorMax = new Vector2(1, 1);
            topRT.offsetMin = topRT.offsetMax = Vector2.zero;
            var topImg = topGo.GetComponent<Image>();
            topImg.sprite = topSprites[idx];
            topImg.preserveAspect = true;
            topImg.color = _textColor;

            // 3. 下半分
            var bottomGo = new GameObject("BottomHalf", typeof(RectTransform), typeof(Image));
            bottomGo.transform.SetParent(transform, false);
            var bottomRT = bottomGo.GetComponent<RectTransform>();
            bottomRT.anchorMin = new Vector2(0, 0); bottomRT.anchorMax = new Vector2(1, 0.5f);
            bottomRT.offsetMin = bottomRT.offsetMax = Vector2.zero;
            var bottomImg = bottomGo.GetComponent<Image>();
            bottomImg.sprite = bottomSprites[idx];
            bottomImg.preserveAspect = true;
            bottomImg.color = _textColor;
        }
    }

    private void ApplyColors() {
        var images = GetComponentsInChildren<Image>();
        foreach (var img in images) {
            img.color = _textColor;
        }
    }
}
