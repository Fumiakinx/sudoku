using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class SimpleMechDisplay : MonoBehaviour
{
    public bool hideZero = false;
    public Color textColor {
        get => _image != null ? _image.color : Color.white;
        set { EnsureImage(); _image.color = value; }
    }
    
    public int TargetValue { get; private set; }
    private Image _image;

    private void EnsureImage() {
        if (_image == null) {
            _image = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            _image.raycastTarget = false;
        }
    }

    public void SetAlpha(float alpha) {
        EnsureImage();
        Color c = _image.color;
        c.a = alpha;
        _image.color = c;
    }

    public void SetValue(int value, Sprite sprite) {
        TargetValue = value;
        EnsureImage();
        
        if (value <= 0 && hideZero) {
            _image.enabled = false;
            return;
        }

        if (sprite != null) {
            _image.enabled = true;
            _image.sprite = sprite;
            _image.color = new Color(_image.color.r, _image.color.g, _image.color.b, _image.color.a);
        } else {
            _image.enabled = false;
        }
    }
}
