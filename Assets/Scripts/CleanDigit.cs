using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class CleanDigit : MonoBehaviour
{
    private Image _image;
    private RectTransform _imageRT;
    private Mask _mask;

    public void Setup(Sprite sprite)
    {
        // 1. マスク（表示窓）を作成
        var maskGo = new GameObject("DisplayWindow", typeof(RectTransform), typeof(Image), typeof(Mask));
        maskGo.transform.SetParent(transform, false);
        var maskRT = maskGo.GetComponent<RectTransform>();
        maskRT.anchorMin = Vector2.zero;
        maskRT.anchorMax = Vector2.one;
        maskRT.offsetMin = maskRT.offsetMax = Vector2.zero;
        maskGo.GetComponent<Mask>().showMaskGraphic = false;

        // 2. 画像を作成（本来の 2倍の高さにして、上半分だけを表示窓に収める）
        var imgGo = new GameObject("DigitImage", typeof(RectTransform), typeof(Image));
        imgGo.transform.SetParent(maskRT, false);
        _imageRT = imgGo.GetComponent<RectTransform>();
        _image = imgGo.GetComponent<Image>();
        
        // アンカーを調整して、画像の「上半分」がマスクに合うようにする
        // スプライトが上下に数字を 2つ持っている場合、これで上の 1つだけが見えるようになる
        _imageRT.anchorMin = new Vector2(0, -1); 
        _imageRT.anchorMax = new Vector2(1, 1);
        _imageRT.offsetMin = _imageRT.offsetMax = Vector2.zero;

        _image.sprite = sprite;
        _image.preserveAspect = true;
    }

    public void SetValue(Sprite sprite)
    {
        if (_image != null) _image.sprite = sprite;
    }
}
