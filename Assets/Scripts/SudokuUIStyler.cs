using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class SudokuUIStyler : MonoBehaviour
{
    private static SudokuUIStyler instance;
    public static SudokuUIStyler Instance {
        get {
            if (instance == null) instance = GameObject.FindAnyObjectByType<SudokuUIStyler>();
            return instance;
        }
        private set => instance = value;
    }

    [Header("Themes")]
    public SudokuData sudokuData;
    
    public SudokuData.SudokuTheme CurrentTheme => sudokuData != null ? sudokuData.CurrentTheme : default;

    [Header("Shared Sprites")]
    public Sprite[] Led7SegSprites;
    public Sprite[] NixieSprites;

    public float bevelThickness = 3f;

    private float previewTimer;
    private int previewValue = 1;
    private GameObject cachedPreviewGo;

    private void Awake() {
        Instance = this;
        if (sudokuData == null) {
            sudokuData = Resources.Load<SudokuData>("SudokuData");
            if (sudokuData == null) {
                // Resourcesにない場合はAssetDatabaseなどで検索（エディタ時）
#if UNITY_EDITOR
                string[] guids = UnityEditor.AssetDatabase.FindAssets("t:SudokuData");
                if (guids.Length > 0) {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    sudokuData = UnityEditor.AssetDatabase.LoadAssetAtPath<SudokuData>(path);
                }
#endif
            }
        }
    }

    private void Update() {
        if (sudokuData == null) return;
        
        float interval = CurrentTheme.previewInterval;
        if (interval <= 0) interval = 2.0f;

        previewTimer += Time.deltaTime;
        if (previewTimer >= interval) {
            previewTimer = 0;
            
            // 1-9, 0, Blank (-1) の順でサイクル
            // 1-9: そのまま
            // 10: 0
            // 11: -1 (Blank)
            int currentState = (previewValue >= 1 && previewValue <= 9) ? previewValue : (previewValue == 0 ? 10 : 11);
            int nextState = (currentState % 11) + 1;
            
            if (nextState <= 9) previewValue = nextState;
            else if (nextState == 10) previewValue = 0;
            else previewValue = -1;

            UpdateThemePreview();
        }
    }

    public void UpdateThemePreview() {
        if (cachedPreviewGo == null) {
            cachedPreviewGo = GameObject.Find("SudokuUI/SafeArea/MenuPanel/ThemePreview/DigitImage");
        }
        
        if (cachedPreviewGo != null && cachedPreviewGo.activeInHierarchy) {
            ApplyDigitVisual(cachedPreviewGo, previewValue, CurrentTheme, false, false);
        }
    }

    public void ApplyTheme(bool isInitial) {
        if (isInitial) cachedTargetSize = -1f;
        ApplyTheme(CurrentTheme);
    }

    public void ApplyTheme(SudokuData.SudokuTheme theme) {
        var bg = FindObject("Background")?.GetComponent<Image>();
        if (bg != null) bg.color = theme.backgroundColor;

        // 1. 主要パネルと入力ボタンの更新
        string[] panels = { "GamePanel", "TopPanel", "BoardPanel", "InputPanel", "MenuPanel" };
        foreach (var pName in panels) {
            var go = FindObject(pName);
            if (go == null) continue;

            NuclearClean(go);

            var img = go.GetComponent<Image>();
            if (img != null) img.color = theme.panelColor;
            ApplyBezel(go, theme, false, bevelThickness);

            // 入力パネル内のボタンにも一貫したスタイルを適用
        }

        // 入力パネルのボタン
        var inputPanel = FindObject("InputPanel");
        if (inputPanel != null) {
            foreach (var btn in inputPanel.GetComponentsInChildren<Button>(true)) {
                NuclearClean(btn.gameObject);
                ApplyBezel(btn.gameObject, theme, false, bevelThickness);
                int val = -1;
                if (btn.name.StartsWith("Btn_")) {
                    string suffix = btn.name.Substring(4);
                    if (suffix == "Clear") val = -1;
                    else int.TryParse(suffix, out val);
                }
                var digitGo = btn.transform.Find("DigitImage")?.gameObject ?? btn.gameObject;
                ApplyDigitVisual(digitGo, val, theme, true, false);
            }
        }

        // メニューパネルのボタン
        var menuPanel = FindObject("MenuPanel");
        if (menuPanel != null) {
            var menuButtons = menuPanel.GetComponentsInChildren<Button>(true);
            foreach (var btn in menuButtons) {
                NuclearClean(btn.gameObject);
                
                // ボタンのサイズを調整 (幅600, 高さ120に設定)
                var btnRT = btn.GetComponent<RectTransform>();
                if (btnRT != null) {
                    btnRT.sizeDelta = new Vector2(600, 120);
                }

                ApplyBezel(btn.gameObject, theme, false, bevelThickness);
            }
        }

        // 2. タイマースタイルの適用
        ApplyTimerStyle(theme);
        
        // 3. 3x3ブロックのベベル適用
        for (int i = 0; i < 9; i++) {
            var blockGo = FindObject($"Block_{i}");
            if (blockGo != null) ApplyBezel(blockGo, theme, false, bevelThickness);
        }

        // 4. 盤面セルの更新
        if (SudokuBoard.Instance != null && SudokuBoard.Instance.cells != null) {
            foreach (var cell in SudokuBoard.Instance.cells) {
                if (cell != null) {
                    NuclearClean(cell.gameObject);

                    var img = cell.GetComponent<Image>();
                    if (img != null) img.color = theme.panelColor;
                    
                    ApplyBezel(cell.gameObject, theme, false, bevelThickness);
                    var digitGo = cell.transform.Find("DigitImage")?.gameObject ?? cell.gameObject;
                    ApplyDigitVisual(digitGo, cell.Value, theme, true, true);
                }
            }
        }
    }

    private float cachedTargetSize = -1f;

    private void ApplyTimerStyle(SudokuData.SudokuTheme theme) {
        var container = FindObject("DigitContainer");
        if (container == null) return;

        if (cachedTargetSize <= 0) {
            var refBtn = FindObject("Btn_1");
            if (refBtn != null) {
                float btnW = refBtn.GetComponent<RectTransform>().rect.width;
                if (btnW > 0) cachedTargetSize = btnW / 2f;
            }
        }
        float targetSize = (cachedTargetSize > 0) ? cachedTargetSize : 57f;

        string[] allElements = { "Timer_H1", "Timer_H2", "Timer_C1", "Timer_M1", "Timer_M2", "Timer_C2", "Timer_S1", "Timer_S2" };
        float totalWidth = targetSize * allElements.Length;

        // コンテナのサイズ調整
        var containerRT = container.GetComponent<RectTransform>();
        containerRT.anchorMin = containerRT.anchorMax = containerRT.pivot = new Vector2(0.5f, 0.5f);
        containerRT.sizeDelta = new Vector2(totalWidth, targetSize);
        containerRT.anchoredPosition = Vector2.zero;

        // 背景のサイズ調整
        var bg = FindObject("TimerBG");
        if (bg != null) {
            var rt = bg.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(totalWidth + 30f, targetSize + 20f);
            rt.anchoredPosition = Vector2.zero;
            ApplyBezel(bg, theme, false, bevelThickness);
        }
        
        float startX = -(allElements.Length - 1) * targetSize / 2f;

        for (int i = 0; i < allElements.Length; i++) {
            // DigitContainerから直接子を探す
            var t = container.transform.Find(allElements[i]);
            if (t == null) continue;
            var go = t.gameObject;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(targetSize, targetSize);
            rt.localScale = Vector3.one;
            rt.anchoredPosition = new Vector2(startX + (i * targetSize), 0);

            if (allElements[i].Contains("_C")) {
                var txt = go.GetComponent<TMPro.TextMeshProUGUI>();
                if (txt != null) {
                    txt.color = (theme.displayType == SudokuData.ThemeDisplayType.Nixie) ? theme.textColor : theme.lineColor;
                    txt.alignment = TMPro.TextAlignmentOptions.Center;
                    txt.fontSize = targetSize * 0.9f;
                    txt.enableAutoSizing = false;
                }
            } else {
                int currentVal = GetCurrentValue(go);
                ApplyDigitVisual(go, currentVal, theme, true, false);
            }
        }
    }

    private int GetCurrentValue(GameObject go) {
        var nixie = go.GetComponent<NixieDigit>();
        if (nixie != null) {
            var field = nixie.GetType().GetField("_currentValue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) return (int)field.GetValue(nixie);
        }
        var led = go.GetComponent<Led7SegDigit>();
        if (led != null) {
            var field = led.GetType().GetField("_currentValue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) return (int)field.GetValue(led);
        }
        var flip = go.GetComponent<FlipFlapDisplay>();
        if (flip != null) return flip.TargetValue;
        return 0;
    }

    private GameObject FindObject(string name) {
        var all = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        foreach (var go in all) {
            if (go.name == name) return go;
        }
        return null;
    }

    public void ApplyDigitVisual(GameObject digitGo, int value, SudokuData.SudokuTheme theme, bool immediate, bool hideZero) {
        if (digitGo == null) return;
        Debug.Log($"[SudokuUIStyler] ApplyDigitVisual: {digitGo.name}, Value: {value}");
        
        var nixie = digitGo.GetComponent<NixieDigit>();
        var led = digitGo.GetComponent<Led7SegDigit>();
        var img = digitGo.GetComponent<Image>();

        if (theme.displayType == SudokuData.ThemeDisplayType.Nixie) {
            if (nixie == null) {
                nixie = digitGo.AddComponent<NixieDigit>();
                nixie.Setup();
            }
            if (led != null) DestroyImmediate(led);
            nixie.hideZero = hideZero;
            nixie.SetValue(value, immediate);
            nixie.RefreshUI();
            if (img != null) img.enabled = false;
        } else if (theme.displayType == SudokuData.ThemeDisplayType.LED7Seg) {
            if (led == null) {
                led = digitGo.AddComponent<Led7SegDigit>();
                led.Setup();
            }
            if (nixie != null) DestroyImmediate(nixie);
            led.hideZero = hideZero;
            led.SetValue(value, immediate);
            led.RefreshUI();
            if (img != null) img.enabled = false;
        } else {
            if (nixie != null) DestroyImmediate(nixie);
            if (led != null) DestroyImmediate(led);
            if (img != null) {
                img.enabled = true;
                if (value > 0 && value <= theme.sprites.Length) {
                    img.sprite = theme.sprites[value - 1];
                    img.color = Color.white;
                } else {
                    img.sprite = null;
                    img.color = Color.clear;
                }
            }
        }
    }

    public void ApplyBezel(GameObject go, SudokuData.SudokuTheme theme, bool isSelected, float thickness) {
        if (go == null) return;
        if (!theme.showBezel) {
            HideBevelImages(go);
            return;
        }

        // 明るい色（上・左）と暗い色（下・右）を使い分けて立体感を出す
        Color light = theme.highlightColor;
        Color dark = theme.shadowColor;

        SetBevel(go, "_Bevel_Top", light, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -thickness), Vector2.zero);
        SetBevel(go, "_Bevel_Left", light, new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, new Vector2(thickness, 0));
        SetBevel(go, "_Bevel_Bottom", dark, new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, new Vector2(0, thickness));
        SetBevel(go, "_Bevel_Right", dark, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-thickness, 0), Vector2.zero);
        
        // 枠線を常に最前面にする
        foreach (Transform child in go.transform) {
            if (child.name.StartsWith("_Bevel")) child.SetAsLastSibling();
        }
    }

    private void SetBevel(GameObject parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax) {
        RectTransform rt = GetOrCreateOverlay(parent, name, typeof(Image), typeof(LayoutElement));
        rt.gameObject.SetActive(true);
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
        rt.GetComponent<Image>().color = color;
        
        // レイアウトグループ（GridLayoutGroup等）に無視させる
        var le = rt.GetComponent<LayoutElement>();
        if (le != null) le.ignoreLayout = true;
    }

    public void ApplySelectionOutline(GameObject go, bool show) {
        ApplyOutline(go, "_SelectionRoot", Color.yellow, 4f, show);
    }

    public void ApplyRelatedHighlight(GameObject go, bool show) {
        var theme = CurrentTheme;
        Color relColor = (theme.displayType == SudokuData.ThemeDisplayType.Nixie) 
            ? new Color(0.7f, 1f, 0f, 0.8f) 
            : new Color(0f, 1f, 0f, 0.8f);   
        
        ApplyOutline(go, "_RelatedRoot", relColor, 3f, show);
    }
    
    public void ApplyCellBackgroundHighlight(GameObject go, bool isSelected, bool isRelated) {
        if (!isSelected && !isRelated) {
            Transform existing = go.transform.Find("_BackgroundHighlight");
            if (existing != null) existing.gameObject.SetActive(false);
            return;
        }

        RectTransform rt = GetOrCreateOverlay(go, "_BackgroundHighlight", typeof(Image));
        rt.gameObject.SetActive(true);
        rt.SetAsFirstSibling();
        
        var bgImg = rt.GetComponent<Image>();
        if (isSelected) {
            bgImg.color = new Color(1f, 1f, 0.5f, 0.2f); 
        } else {
            bgImg.color = new Color(0.7f, 1f, 0.5f, 0.15f); 
        }
    }

    private void ApplyOutline(GameObject go, string rootName, Color color, float thickness, bool show) {
        if (!show) {
            Transform existing = go.transform.Find(rootName);
            if (existing != null) existing.gameObject.SetActive(false);
            return;
        }

        RectTransform root = GetOrCreateOverlay(go, rootName);
        root.gameObject.SetActive(true);
        root.SetAsLastSibling();

        if (rootName == "_SelectionRoot") {
            Canvas cv = root.GetComponent<Canvas>();
            if (cv == null) cv = root.gameObject.AddComponent<Canvas>();
            if (cv != null) {
                cv.overrideSorting = true;
                cv.sortingOrder = 30000;
            }
        } else {
            Canvas cv = root.GetComponent<Canvas>();
            if (cv != null) DestroyImmediate(cv);
        }
        
        SetOutlineSide(root.gameObject, "_Out_Top", color, new Vector2(0, 1), new Vector2(1, 1), new Vector2(-thickness, 0), new Vector2(thickness, thickness));
        SetOutlineSide(root.gameObject, "_Out_Bottom", color, new Vector2(0, 0), new Vector2(1, 0), new Vector2(-thickness, -thickness), new Vector2(thickness, 0));
        SetOutlineSide(root.gameObject, "_Out_Left", color, new Vector2(0, 0), new Vector2(0, 1), new Vector2(-thickness, -thickness), new Vector2(0, thickness));
        SetOutlineSide(root.gameObject, "_Out_Right", color, new Vector2(1, 0), new Vector2(1, 1), Vector2.zero, new Vector2(thickness, thickness));
    }

    private void SetOutlineSide(GameObject parent, string name, Color color, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax) {
        RectTransform rt = GetOrCreateOverlay(parent, name, typeof(Image));
        rt.gameObject.SetActive(true);
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.offsetMin = oMin; rt.offsetMax = oMax;
        rt.GetComponent<Image>().color = color;
    }

    private RectTransform GetOrCreateOverlay(GameObject parent, string name, params System.Type[] components) {
        Transform t = parent.transform.Find(name);
        GameObject go;
        if (t != null) {
            go = t.gameObject;
        } else {
            go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
        }

        foreach (var type in components) {
            if (type != typeof(RectTransform) && go.GetComponent(type) == null) {
                go.AddComponent(type);
            }
        }
        
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        
        var img = go.GetComponent<Image>();
        if (img != null) img.raycastTarget = false;
        
        return rt;
    }

    private void NuclearClean(GameObject go) {
        if (go == null) return;
        var toDestroy = new System.Collections.Generic.List<GameObject>();
        foreach (Transform t in go.transform) {
            if (t.name.StartsWith("_Bevel") || t.name.StartsWith("_Out") || t.name.StartsWith("_Selection") || t.name.StartsWith("_Related")) {
                toDestroy.Add(t.gameObject);
            }
        }
        foreach (var d in toDestroy) DestroyImmediate(d);
    }

    private void HideBevelImages(GameObject parent) {
        foreach (Transform child in parent.transform) {
            if (child.name.StartsWith("_Bevel")) child.gameObject.SetActive(false);
        }
    }
}
