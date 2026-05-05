using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class GraphicalTimer : MonoBehaviour
{
    [SerializeField] private Component[] digits = new Component[6];
    [SerializeField] private TextMeshProUGUI colonText;
    [SerializeField] private TextMeshProUGUI colonText2; // 秒の前のコロン用
    [SerializeField] private TextMeshProUGUI legacyText;
    
    public float scaleFactor = 1.0f;
    public bool isBilliards = false;
    private int _lastTotalSeconds = -1;
    
    private void OnEnable() {
        _lastTotalSeconds = -1;
    }

    private static readonly Dictionary<int, Vector2> BilliardsOffsets = new Dictionary<int, Vector2> {
        { 0, Vector2.zero },
        { 1, new Vector2(0, 5) },
        { 2, new Vector2(0, 5) },
        { 3, new Vector2(0, 5) },
        { 4, new Vector2(0, 5) },
        { 5, new Vector2(0, 5) },
        { 6, Vector2.zero },
        { 7, Vector2.zero },
        { 8, Vector2.zero },
        { 9, Vector2.zero }
    };

    private Sprite[] digitSprites;

    public void SetDigits6(Component h1, Component h2, Component m1, Component m2, Component s1, Component s2, TextMeshProUGUI c1, TextMeshProUGUI c2)
    {
        digits = new Component[] { h1, h2, m1, m2, s1, s2 };
        colonText = c1;
        colonText2 = c2;
        _lastTotalSeconds = -1; 
    }

    public void SetSprites(Sprite[] sprites)
    {
        digitSprites = sprites;
    }

    public void SetLegacyText(TextMeshProUGUI text)
    {
        legacyText = text;
    }

    public void UpdateTime(float gameTime)
    {
        int totalSeconds = Mathf.FloorToInt(gameTime);
        if (totalSeconds == _lastTotalSeconds) return; 
        _lastTotalSeconds = totalSeconds;

        int hours = totalSeconds / 3600;
        int minutes = (totalSeconds % 3600) / 60;
        int seconds = totalSeconds % 60;

        int h1 = (hours / 10) % 10;
        int h2 = hours % 10;
        int m1 = (minutes / 10) % 10;
        int m2 = minutes % 10;
        int s1 = (seconds / 10) % 10;
        int s2 = seconds % 10;

        if (digits != null && digits.Length >= 6) {
            SetDigitValue(0, h1);
            SetDigitValue(1, h2);
            SetDigitValue(2, m1);
            SetDigitValue(3, m2);
            SetDigitValue(4, s1);
            SetDigitValue(5, s2);
        } else if (digits != null && digits.Length == 4) {
            SetDigitValue(0, m1);
            SetDigitValue(1, m2);
            SetDigitValue(2, s1);
            SetDigitValue(3, s2);
        }

        if (legacyText != null)
        {
            legacyText.text = string.Format("{0:00}:{1:00}:{2:00}", hours, minutes, seconds);
        }
    }

    public void SetScale(float scale) { scaleFactor = scale; }

    private void SetDigitValue(int index, int val)
    {
        if (index < 0 || index >= (digits?.Length ?? 0) || digits[index] == null) return;

        var go = digits[index].gameObject;
        var nixie = go.GetComponent<NixieDigit>();
        var flip = go.GetComponent<FlipFlapDisplay>();
        var led = go.GetComponent<Led7SegDigit>();
        var txt = go.GetComponentInChildren<TextMeshProUGUI>();
        var img = go.GetComponent<Image>();

        if (nixie != null) { nixie.SetValue(val, true); if (txt) txt.gameObject.SetActive(false); }
        else if (flip != null) { flip.SetValue(val, true); if (txt) txt.gameObject.SetActive(false); }
        else if (led != null) { led.SetValue(val, true); if (txt) txt.gameObject.SetActive(false); }
        else if (txt != null) { txt.text = val.ToString(); }
        else if (img != null)
        {
            img.preserveAspect = true;
            if (digitSprites != null && digitSprites.Length > 0 && val >= 0 && val < 10)
            {
                bool isZeroBased = (digitSprites[0] != null && digitSprites[0].name.Contains("0"));
                int sIdx = isZeroBased ? val : (val == 0 ? 9 : val - 1);
                
                if (sIdx >= 0 && sIdx < digitSprites.Length) {
                    img.sprite = digitSprites[sIdx];
                    img.gameObject.SetActive(true);
                    if (txt) txt.gameObject.SetActive(false);

                    if (isBilliards) {
                        img.rectTransform.localScale = Vector3.one * scaleFactor;
                        if (BilliardsOffsets.TryGetValue(val, out Vector2 offset)) {
                            img.rectTransform.anchoredPosition = offset * scaleFactor; 
                        } else {
                            img.rectTransform.anchoredPosition = Vector2.zero;
                        }
                    } else {
                        img.rectTransform.localScale = Vector3.one;
                        // img.rectTransform.anchoredPosition = Vector2.zero; // 手動レイアウトを維持するため
                    }

                } else {
                    img.gameObject.SetActive(false);
                    if (txt) { txt.gameObject.SetActive(true); txt.text = val.ToString(); }
                }
            }
        }
        if (colonText != null) colonText.gameObject.SetActive(true);
        if (colonText2 != null) colonText2.gameObject.SetActive(true);
    }
}
