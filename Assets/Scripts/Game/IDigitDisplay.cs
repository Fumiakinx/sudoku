using UnityEngine;

public interface IDigitDisplay
{
    void SetValue(int value, bool immediate = false);
    void RefreshUI();
    void SetSprites(Sprite[] sprites);
    bool HideZero { get; set; }
    int Value { get; }
}
