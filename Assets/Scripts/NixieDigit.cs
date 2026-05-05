using UnityEngine;
using UnityEngine.UI;

public class NixieDigit : MonoBehaviour
{
    private static readonly int[] StackOrder = { 3, 8, 9, 4, 0, 5, 7, 2, 6, 1 }; 
    
    [SerializeField] private Image[] wireLayers = new Image[10];
    
    private int _currentValue = -1;
    private Sprite[] _digitSprites;
    public bool hideZero = false; 
    public bool usePerspective = true;
    private Coroutine _animRoutine;
    private SudokuUIStyler styler;

    public void SetSprites(Sprite[] sprites)
    {
        _digitSprites = sprites;
        UpdateVisuals();
    }

    public void RefreshUI()
    {
        UpdateVisuals();
    }

    public void SetValue(int val, bool immediate = false)
    {
        if (_currentValue == val && !immediate) return;
        
        if (_animRoutine != null) {
            StopCoroutine(_animRoutine);
            _animRoutine = null;
        }

        if (immediate || val <= 0 || !gameObject.activeInHierarchy) {
            _currentValue = val;
            UpdateVisuals();
        } else {
            _animRoutine = StartCoroutine(AnimateSwitch(val));
        }
    }


    private System.Collections.IEnumerator AnimateSwitch(int targetVal)
    {
        int targetIdx = -1;
        for (int i = 0; i < StackOrder.Length; i++) {
            if (StackOrder[i] == targetVal) { targetIdx = i; break; }
        }

        for (int i = 0; i <= targetIdx; i++)
        {
            _currentValue = StackOrder[i];
            UpdateVisuals();
            if (i < targetIdx) yield return new WaitForSeconds(0.05f);
        }
        
        _currentValue = targetVal;
        UpdateVisuals();
        _animRoutine = null;
    }

    private void UpdateVisuals()
    {
        if (!enabled) return;
        bool needsSetup = (wireLayers == null || wireLayers.Length != 10);
        if (!needsSetup) {
            foreach (var img in wireLayers) if (img == null) { needsSetup = true; break; }
        }
        if (needsSetup) {
            Setup();
            return;
        }

        if (styler == null) styler = SudokuUIStyler.Instance;
        if (styler == null) return;
        
        var theme = styler.CurrentTheme;
        var sprites = styler.NixieSprites;
        if (sprites == null || sprites.Length < 10) return;

        int targetIndexInStack = -1;
        for (int i = 0; i < StackOrder.Length; i++) {
            if (StackOrder[i] == _currentValue) {
                targetIndexInStack = i;
                break;
            }
        }

        for (int i = 0; i < StackOrder.Length; i++)
        {
            int digit = StackOrder[i];
            if (digit < 0 || digit >= wireLayers.Length) continue;
            Image img = wireLayers[digit];
            if (img == null) continue;

            if (_currentValue < 0 || (_currentValue == 0 && hideZero)) {
                img.gameObject.SetActive(false);
                continue;
            }

            img.sprite = sprites[digit];
            img.preserveAspect = true;
            float scale = usePerspective ? (0.95f + (i * 0.0055f)) : 1.0f;
            img.transform.localScale = new Vector3(scale, scale, 1f);

            if (digit == _currentValue) {
                img.color = new Color(theme.textColor.r * 0.7f, theme.textColor.g * 0.7f, theme.textColor.b * 0.7f, theme.textColor.a);
                img.gameObject.SetActive(true);
            } else {
                if (targetIndexInStack == -1) {
                    img.gameObject.SetActive(false);
                } else if (i < targetIndexInStack) {
                    img.color = new Color(theme.textColor.r * 0.05f, theme.textColor.g * 0.05f, theme.textColor.b * 0.05f, 0.01f);
                    img.gameObject.SetActive(true);
                } else {
                    img.color = new Color(theme.textColor.r * 0.02f, theme.textColor.g * 0.02f, theme.textColor.b * 0.02f, 0.005f);
                    img.gameObject.SetActive(true);
                }
            }
        }
    }

    private void OnDisable()
    {
        if (wireLayers != null) {
            foreach (var img in wireLayers) if (img != null) img.gameObject.SetActive(false);
        }
        if (_animRoutine != null) {
            StopCoroutine(_animRoutine);
            _animRoutine = null;
        }
    }

    public void Setup()
    {
        _currentValue = -999;
        bool needsRebuild = false;
        if (wireLayers == null || wireLayers.Length != 10) needsRebuild = true;
        else {
            foreach (var img in wireLayers) if (img == null) { needsRebuild = true; break; }
        }

        if (!needsRebuild) { UpdateVisuals(); return; }

        for (int i = transform.childCount - 1; i >= 0; i--) {
            var child = transform.GetChild(i);
            if (child.name.StartsWith("Wire_")) DestroyImmediate(child.gameObject);
        }

        wireLayers = new Image[10];
        for (int i = 0; i < StackOrder.Length; i++)
        {
            int digit = StackOrder[i];
            var go = new GameObject("Wire_" + digit, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            var img = go.GetComponent<Image>();
            img.color = Color.clear; // 初期化時は透明にする
            img.preserveAspect = true;
            img.raycastTarget = false;
            wireLayers[digit] = img; // digitをインデックスとして保存

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }
        UpdateVisuals();
    }
}
