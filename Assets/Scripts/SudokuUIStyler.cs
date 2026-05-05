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

    private void Start() {
        if (!Application.isPlaying) return;
        ApplyTheme(true);
    }

    private void Update() {
        if (sudokuData == null) return;
        
        float interval = CurrentTheme.previewInterval;
        if (interval <= 0) interval = 2.0f;

        previewTimer += Time.deltaTime;
        if (previewTimer >= interval) {
            previewTimer = 0;
            
            // 1-9, 0 の順でサイクル
            int currentState = (previewValue >= 1 && previewValue <= 9) ? previewValue : 0;
            int nextState = (currentState % 10) + 1;
            
            if (nextState <= 9) previewValue = nextState;
            else previewValue = 0;

            UpdateThemePreview(true);
        }
    }

    public void UpdateThemePreview(bool immediate = false) {
        if (cachedPreviewGo == null) {
            var previewPanel = FindObject("ThemePreview");
            if (previewPanel != null) {
                var digitTransform = previewPanel.transform.Find("DigitImage");
                if (digitTransform != null) cachedPreviewGo = digitTransform.gameObject;
                else cachedPreviewGo = previewPanel; // fallback to panel itself
            }
        }
        
        if (cachedPreviewGo != null && cachedPreviewGo.activeInHierarchy) {
            ApplyDigitVisual(cachedPreviewGo, previewValue, CurrentTheme, immediate, false);
        }
    }

    public void ApplyTheme(bool isInitial) {
        if (isInitial) cachedTargetSize = -1f;
        ApplyTheme(CurrentTheme);
    }

    public void ApplyTheme(SudokuData.SudokuTheme theme) {
        if (string.IsNullOrEmpty(theme.themeName)) return;
        Debug.Log($"<color=#00FF00><b>[SudokuUIStyler]</b></color> テーマ適用開始: <b>{theme.themeName}</b> (表示タイプ: {theme.displayType})");
        
        // スプライト配列を即座に更新（各コンポーネントが参照するため）
        if (theme.displayType == SudokuData.ThemeDisplayType.Nixie) NixieSprites = theme.sprites;
        else if (theme.displayType == SudokuData.ThemeDisplayType.LED7Seg) Led7SegSprites = theme.sprites;

        var bgGo = FindObject("Background");
        if (bgGo != null) {
            var bg = bgGo.GetComponent<Image>();
            if (bg != null) {
                bg.color = theme.backgroundColor;
                Debug.Log($"[SudokuUIStyler] 背景色を設定: {theme.backgroundColor}");
            }
        }

        // 1. 主要パネルの更新
        string[] panels = { "GamePanel", "TopPanel", "BoardPanel", "InputPanel", "MenuPanel" };
        foreach (var pName in panels) {
            var go = FindObject(pName);
            if (go == null) {
                Debug.LogWarning($"[SudokuUIStyler] パネルが見つかりません: {pName}");
                continue;
            }
            
            Debug.Log($"[SudokuUIStyler] パネルをスタイリング: {pName}");
            var img = go.GetComponent<Image>();
            if (img != null) img.color = theme.panelColor;
            
            ApplyBezel(go, theme, false, bevelThickness);

            var texts = go.GetComponentsInChildren<TMPro.TMP_Text>(true);
            foreach (var txt in texts) {
                // _Label などの生成済みパーツ以外のテキストの色を更新
                if (!txt.name.StartsWith("_")) {
                    txt.color = theme.textColor;
                }
            }
        }

        // 2. 入力パネルのボタンの個別更新
        var inputPanel = FindObject("InputPanel");
        if (inputPanel != null) {
            var buttons = inputPanel.GetComponentsInChildren<Button>(true);
            Debug.Log($"[SudokuUIStyler] 入力ボタンの更新開始 (合計: {buttons.Length})");
            foreach (var btn in buttons) {
                int val = -1;
                if (btn.name.StartsWith("Btn_")) {
                    string suffix = btn.name.Substring(4);
                    if (suffix == "Clear") val = -1;
                    else int.TryParse(suffix, out val);
                }
                
                var digitGo = btn.transform.Find("DigitImage")?.gameObject ?? btn.gameObject;
                ApplyDigitVisual(digitGo, val, theme, true, false);
                ApplyBezel(btn.gameObject, theme, false, bevelThickness);
            }
        }

        // 3. メニューパネルのボタン調整
        var menuPanel = FindObject("MenuPanel");
        if (menuPanel != null) {
            var menuTexts = new string[] { "EASY", "MEDIUM", "HARD", "EXPERT" };
            var buttons = menuPanel.GetComponentsInChildren<Button>(true);
            Debug.Log($"[SudokuUIStyler] メニューボタンの更新開始 (合計: {buttons.Length})");
            int diffIdx = 0;
            foreach (var btn in buttons) {
                var btnRT = btn.GetComponent<RectTransform>();
                if (btnRT != null) btnRT.sizeDelta = new Vector2(600, 120);

                if (btn.name == "Btn_ThemeToggle") {
                    var txt = btn.GetComponentInChildren<TMPro.TMP_Text>(true);
                    if (txt == null) {
                        Debug.Log("[SudokuUIStyler] ThemeToggle のラベルを作成します");
                        var go = new GameObject("_Label", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
                        go.transform.SetParent(btn.transform, false);
                        txt = go.GetComponent<TMPro.TextMeshProUGUI>();
                        var rt = go.GetComponent<RectTransform>();
                        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                        rt.offsetMin = rt.offsetMax = Vector2.zero;
                    }
                    if (txt != null) {
                        txt.name = "_Label";
                        txt.alignment = TMPro.TextAlignmentOptions.Center;
                        txt.gameObject.SetActive(true);
                        txt.color = theme.textColor;
                    }
                    ApplyBezel(btn.gameObject, theme, false, bevelThickness);
                    continue;
                }

                if (btn.name.StartsWith("Btn_")) {
                    var txt = btn.GetComponentInChildren<TMPro.TMP_Text>(true);
                    if (txt == null && diffIdx < menuTexts.Length) {
                        Debug.Log($"[SudokuUIStyler] {btn.name} のラベルを作成します");
                        var go = new GameObject("_Label", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
                        go.transform.SetParent(btn.transform, false);
                        txt = go.GetComponent<TMPro.TextMeshProUGUI>();
                        txt.text = menuTexts[diffIdx];
                        var rt = go.GetComponent<RectTransform>();
                        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                        rt.offsetMin = rt.offsetMax = Vector2.zero;
                    }
                    if (txt != null) {
                        txt.color = theme.textColor;
                        txt.gameObject.SetActive(true);
                        txt.name = "_Label";
                        txt.alignment = TMPro.TextAlignmentOptions.Center;
                    }
                    ApplyBezel(btn.gameObject, theme, false, bevelThickness);
                    diffIdx++;
                }
            }
        }

        // 4. タイマースタイルの適用
        Debug.Log("[SudokuUIStyler] タイマースタイルを適用中...");
        ApplyTimerStyle(theme);
        
        // 5. 3x3ブロックのベベル適用
        Debug.Log("[SudokuUIStyler] 3x3ブロックのベベルを適用中...");
        for (int i = 0; i < 9; i++) {
            var blockGo = FindObject($"Block_{i}");
            if (blockGo != null) ApplyBezel(blockGo, theme, false, bevelThickness);
        }

        // 6. 盤面セルの更新
        if (SudokuBoard.Instance != null && SudokuBoard.Instance.cells != null) {
            Debug.Log($"[SudokuUIStyler] 盤面セルの更新開始 (合計: {SudokuBoard.Instance.cells.Length})");
            foreach (var cell in SudokuBoard.Instance.cells) {
                if (cell != null) {
                    // セルのクリーンアップ
                    RobustCleanUp(cell.gameObject, false);
                    
                    var img = cell.GetComponent<Image>();
                    if (img != null) img.color = theme.panelColor;
                    
                    ApplyBezel(cell.gameObject, theme, false, bevelThickness);
                    
                    var digitGo = cell.transform.Find("DigitImage")?.gameObject ?? cell.gameObject;
                    ApplyDigitVisual(digitGo, cell.Value, theme, true, true);
                }
            }
        } else {
            Debug.LogWarning("[SudokuUIStyler] SudokuBoard.Instance または cells が null です。セルの更新をスキップします。");
        }

        // 7. 選択中のセルのハイライト更新
        if (SudokuBoard.Instance != null) {
            SudokuBoard.Instance.UpdateSelectionVisuals();
        }

        // 8. ボタンラベルの更新
        UpdateThemeButtonLabel();

        // 9. テーマプレビューの即時更新
        UpdateThemePreview(true);
        
        Debug.Log($"<color=#00FF00><b>[SudokuUIStyler]</b></color> テーマ適用完了: <b>{theme.themeName}</b>");
    }


    private string GetPath(Transform t) {
        if (t.parent == null) return t.name;
        return GetPath(t.parent) + "/" + t.name;
    }

    private static int lastCycleFrame = -1;
    private static float lastCycleTime = -1f;

    public void CycleTheme() {
        if (UnityEngine.Time.frameCount == lastCycleFrame) return;
        if (UnityEngine.Time.unscaledTime - lastCycleTime < 0.1f) return;
        
        lastCycleFrame = UnityEngine.Time.frameCount;
        lastCycleTime = UnityEngine.Time.unscaledTime;

        if (sudokuData == null || sudokuData.themes == null || sudokuData.themes.Length == 0) return;

        int nextIndex = GetNextThemeIndex();

        if (nextIndex != sudokuData.selectedThemeIndex) {
            sudokuData.selectedThemeIndex = nextIndex;
            
            // カウントアップ表示をリセットして開始
            previewValue = 1;
            previewTimer = 0;
            
            ApplyTheme(true);
            Debug.Log($"[SudokuUIStyler] Theme Cycled to: {CurrentTheme.themeName} (Index: {nextIndex})");
        }
    }

    private int GetNextThemeIndex() {
        int count = sudokuData.themes.Length;
        for (int i = 1; i <= count; i++) {
            int idx = (sudokuData.selectedThemeIndex + i) % count;
            var t = sudokuData.themes[idx];
            // アンロックされており、かつ Nixie または LED であるテーマを選択
            if (!t.isLocked && (t.displayType == SudokuData.ThemeDisplayType.Nixie || t.displayType == SudokuData.ThemeDisplayType.LED7Seg)) {
                return idx;
            }
        }
        return sudokuData.selectedThemeIndex;
    }

    public void UpdateThemeButtonLabel() {
        var btn = GameObject.Find("Btn_ThemeToggle");
        if (btn == null) btn = FindObject("Btn_ThemeToggle");
        if (btn == null) return;

        var txt = btn.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (txt != null) {
            txt.text = $"THEME: {CurrentTheme.themeName.ToUpper()}";
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
            GameObject go;
            if (t == null) {
                go = new GameObject(allElements[i], typeof(RectTransform));
                go.transform.SetParent(container.transform, false);
            } else {
                go = t.gameObject;
            }

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(targetSize, targetSize);
            rt.localScale = Vector3.one;
            rt.anchoredPosition = new Vector2(startX + (i * targetSize), 0);

            if (allElements[i].Contains("_C")) {
                var txt = go.GetComponent<TMPro.TextMeshProUGUI>();
                if (txt == null) txt = go.AddComponent<TMPro.TextMeshProUGUI>();
                if (txt != null) {
                    txt.text = ":";
                    Color colonColor = (theme.displayType == SudokuData.ThemeDisplayType.Nixie) ? theme.textColor : theme.lineColor;
                    txt.color = AdjustLuminance(colonColor, 0.7f);
                    txt.alignment = TMPro.TextAlignmentOptions.Center;
                    txt.fontSize = targetSize * 0.9f;
                    txt.enableAutoSizing = false;
                }
            } else {
                int currentVal = GetCurrentValue(go);
                ApplyDigitVisual(go, currentVal, theme, true, false);
            }
        }

        // GraphicalTimerの参照を更新
        var gt = GameObject.FindAnyObjectByType<GraphicalTimer>();
        if (gt != null) {
            var h1 = container.transform.Find("Timer_H1");
            var h2 = container.transform.Find("Timer_H2");
            var m1 = container.transform.Find("Timer_M1");
            var m2 = container.transform.Find("Timer_M2");
            var s1 = container.transform.Find("Timer_S1");
            var s2 = container.transform.Find("Timer_S2");
            var c1 = container.transform.Find("Timer_C1")?.GetComponent<TMPro.TextMeshProUGUI>();
            var c2 = container.transform.Find("Timer_C2")?.GetComponent<TMPro.TextMeshProUGUI>();
            
            if (h1 != null && s2 != null) {
                gt.SetDigits6(h1, h2, m1, m2, s1, s2, c1, c2);
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
        return 0;
    }

    private GameObject FindObject(string name) {
        var canvas = GameObject.Find("SudokuUI");
        if (canvas == null) return null;
        
        // Canvas直下または子孫から再帰的に検索
        return FindRecursive(canvas.transform, name);
    }

    private GameObject FindRecursive(Transform parent, string name) {
        if (parent == null) return null;
        if (parent.name == name) return parent.gameObject;
        foreach (Transform child in parent) {
            var result = FindRecursive(child, name);
            if (result != null) return result;
        }
        return null;
    }

    public void ApplyDigitVisual(GameObject digitGo, int value, SudokuData.SudokuTheme theme, bool immediate = false, bool hideZero = false) {
        if (digitGo == null) return;
        
        // 詳細ログ（大量に出るため、必要な場合のみ有効化するか、特定条件で出す）
        // Debug.Log($"[SudokuUIStyler] ApplyDigitVisual: {digitGo.name} | 値: {value} | テーマタイプ: {theme.displayType}");

        Color displayColor = AdjustLuminance(theme.textColor, 0.7f);
        var nixie = digitGo.GetComponent<NixieDigit>();
        var led = digitGo.GetComponent<Led7SegDigit>();
        var img = digitGo.GetComponent<Image>();

        // 1. テーマの切り替えに伴う不一致コンポーネントの除去
        if (theme.displayType != SudokuData.ThemeDisplayType.Nixie && nixie != null) {
            Debug.Log($"[SudokuUIStyler] コンポーネント除去 (Nixie -> Other): {digitGo.name}");
            // Nixie特有の子オブジェクト(Wire_)を削除
            for (int i = digitGo.transform.childCount - 1; i >= 0; i--) {
                Transform child = digitGo.transform.GetChild(i);
                if (child.name.StartsWith("Wire_")) SafeDestroy(child.gameObject);
            }
            SafeDestroy(nixie); nixie = null;
        }
        if (theme.displayType != SudokuData.ThemeDisplayType.LED7Seg && led != null) {
            Debug.Log($"[SudokuUIStyler] コンポーネント除去 (LED -> Other): {digitGo.name}");
            SafeDestroy(led); led = null;
        }

        // 2. 以前の表示生成物を一掃（Digit系コンポーネントが自前で管理するものは除外）
        // _Label (Cボタン用) のみを確認して必要なら消す
        var oldLabel = digitGo.transform.Find("_Label");
        if (value != -1 && oldLabel != null) {
            SafeDestroy(oldLabel.gameObject);
        }

        // 3. 元のテキストコンポーネントを保護（非アクティブ化）
        // DigitImage 自体が TMP_Text を持っている場合の干渉を防ぐ
        var tComp = digitGo.GetComponent<TMPro.TMP_Text>();
        if (tComp != null) tComp.enabled = (theme.displayType == SudokuData.ThemeDisplayType.Normal);

        // 4. 特別なラベル（クリアボタン等）の処理
        if (value == -1) {
            if (nixie != null) nixie.enabled = false;
            if (led != null) led.enabled = false;
            if (img != null) img.enabled = false;

            var clearLabelGo = new GameObject("_Label", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            clearLabelGo.transform.SetParent(digitGo.transform, false);
            var labelTxt = clearLabelGo.GetComponent<TMPro.TextMeshProUGUI>();
            var rt = clearLabelGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            
            labelTxt.text = "C";
            labelTxt.color = displayColor;
            labelTxt.alignment = TMPro.TextAlignmentOptions.Center;
            labelTxt.fontSize = 80;
            labelTxt.enableAutoSizing = true;
            return;
        }

        // 5. 通常の数字表示処理
        if (theme.displayType == SudokuData.ThemeDisplayType.Nixie) {
            if (nixie == null) {
                Debug.Log($"[SudokuUIStyler] NixieDigit 追加: {digitGo.name}");
                nixie = digitGo.AddComponent<NixieDigit>();
                nixie.Setup();
            }
            nixie.enabled = true;
            nixie.hideZero = hideZero;
            nixie.SetValue(value, immediate);
            nixie.RefreshUI();
            
            if (led != null) led.enabled = false; // LEDを無効化
            if (img != null) {
                img.sprite = null;
                img.enabled = false;
            }
        } else if (theme.displayType == SudokuData.ThemeDisplayType.LED7Seg) {
            if (led == null) {
                Debug.Log($"[SudokuUIStyler] Led7SegDigit 追加: {digitGo.name}");
                led = digitGo.AddComponent<Led7SegDigit>();
                led.Setup();
            }
            led.enabled = true;
            led.hideZero = hideZero;
            led.SetValue(value, immediate);
            led.RefreshUI();
            
            if (nixie != null) nixie.enabled = false; // Nixieを無効化
            
            // 重要: Nixieで Image が無効化されていた場合、ここで再度有効化する必要がある
            if (img != null) {
                img.enabled = true;
                // color は Led7SegDigit.RefreshUI 内で設定されるが、
                // アルファ値が 0 になっている可能性があるためリセット
                img.color = new Color(img.color.r, img.color.g, img.color.b, 1f);
            }
        } else {
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
        // 標準ベベル(3px)の倍の 6px に設定。色は黄色に固定。
        ApplyOutline(go, "_SelectionRoot", Color.yellow, 6f, show);
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
        
        // すべてのテーマで、数字の上に色を薄く重ねる（SetAsLastSibling）
        rt.SetAsLastSibling();
        
        var bgImg = rt.GetComponent<Image>();
        if (isSelected) {
            // 選択中：さらに薄い黄色（透過度 0.08）
            bgImg.color = new Color(1f, 1f, 0f, 0.08f); 
        } else {
            // 関連セル：さらに薄い緑色（透過度 0.04）
            bgImg.color = new Color(0f, 1f, 0f, 0.04f); 
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

    private void SafeDestroy(Object obj) {
        if (obj == null) return;
        if (Application.isPlaying) Destroy(obj);
        else DestroyImmediate(obj);
    }

    /// <summary>
    /// UIの生成済みオブジェクトやコンポーネントを安全かつ確実に削除します。
    /// </summary>
    /// <param name="go">対象のGameObject</param>
    /// <param name="keepLabel">_Label という名前のオブジェクトを保持するかどうか</param>
    private void RobustCleanUp(GameObject go, bool keepLabel) {
        if (go == null) return;

        // 削除対象のプレフィックス定義（Stylerが直接生成したものに限定）
        string[] targetPrefixes = { "_Bevel", "_Out", "_Selection", "_Related", "_BackgroundHighlight" };
        
        var toDestroy = new System.Collections.Generic.List<Object>();

        for (int i = go.transform.childCount - 1; i >= 0; i--) {
            Transform child = go.transform.GetChild(i);
            string n = child.name;

            // DigitImage は絶対に消さない
            if (n == "DigitImage") continue;

            bool shouldDestroy = false;
            foreach (var prefix in targetPrefixes) {
                if (n.StartsWith(prefix)) {
                    shouldDestroy = true;
                    break;
                }
            }

            if (n == "_Label" && !keepLabel) shouldDestroy = true;

            if (shouldDestroy) {
                toDestroy.Add(child.gameObject);
            }
        }

        if (toDestroy.Count > 0) {
            foreach (var d in toDestroy) SafeDestroy(d);
        }

        // 3. 不要になった Canvas コンポーネント等のチェック（必要に応じて）
        var canvas = go.GetComponent<Canvas>();
        if (canvas != null && !go.name.Contains("UI") && !go.name.Contains("Panel")) {
            // セル等に付与された一時的な Canvas を削除
            SafeDestroy(canvas);
        }
    }

    // 後方互換性のためのスタブ
    private void NuclearClean(GameObject go) => RobustCleanUp(go, false);
    private void CleanPanelVisuals(GameObject go) => RobustCleanUp(go, true);

    private void HideBevelImages(GameObject parent) {
        foreach (Transform child in parent.transform) {
            if (child.name.StartsWith("_Bevel")) child.gameObject.SetActive(false);
        }
    }

    // 輝度を調整（目に優しい明るさに）
    private Color AdjustLuminance(Color col, float factor) {
        float h, s, v;
        Color.RGBToHSV(col, out h, out s, out v);
        return Color.HSVToRGB(h, s, v * factor);
    }
}
