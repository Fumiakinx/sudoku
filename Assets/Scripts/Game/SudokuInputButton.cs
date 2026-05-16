using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 入力ボタン（1-9, Clear）にアタッチして、自分の値を自律的に表示・管理します。
/// </summary>
public class SudokuInputButton : MonoBehaviour 
{
    [SerializeField] private int _value;
    public int Value {
        get => _value;
        set {
            if (_value != value) {
                _value = value;
                Refresh();
            }
        }
    }

    private void Awake() {
        var btn = GetComponent<Button>();
        var img = GetComponent<Image>();
        
        string prefabStatus = "Unknown";
#if UNITY_EDITOR
        var prefabAsset = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(gameObject);
        var path = prefabAsset != null ? UnityEditor.AssetDatabase.GetAssetPath(prefabAsset) : "";
        prefabStatus = string.IsNullOrEmpty(path) ? "Scene Instance" : path;
#endif

        Debug.Log($"[Btn-Life] Awake: {gameObject.name}, Prefab: {prefabStatus}, ButtonEnabled: {(btn != null ? btn.enabled : "NULL")}, ImageEnabled: {(img != null ? img.enabled : "NULL")}");
    }


    private void Start() {
        var btn = GetComponent<Button>();
        var img = GetComponent<Image>();
        Debug.Log($"[Btn-Life] Start: {gameObject.name}, Value: {Value}, ButtonEnabled: {(btn != null ? btn.enabled : "NULL")}, ImageEnabled: {(img != null ? img.enabled : "NULL")}");

        Refresh();
    }

    private void OnEnable() {
        SudokuThemeManager.OnThemeChanged += HandleThemeChanged;
    }

    private void OnDisable() {
        SudokuThemeManager.OnThemeChanged -= HandleThemeChanged;
        // コンポーネントが無効化された瞬間の呼び出し元を特定
        Debug.Log($"[Btn-Watcher] OnDisable called on {gameObject.name}! Possible component disable detected.\nStackTrace: {System.Environment.StackTrace}");
    }

    private void HandleThemeChanged(SudokuData.SudokuTheme theme, bool isInitial) {
        Refresh();
    }

    public void Refresh() {
        var display = GetComponent<SudokuDigitDisplay>();
        if (display != null) {
            Debug.Log($"[SudokuInputButton] Refresh: {gameObject.name}, Value: {Value} -> Setting to Display");
            // 0の場合は「Clear」スプライトを表示するために -2 を渡す
            int displayValue = (Value == 0) ? -2 : Value;
            display.SetDigit(displayValue, default, true);
        }

        // ベゼルの適用
        if (SudokuThemeManager.Instance != null) {
            var theme = SudokuThemeManager.Instance.CurrentTheme;
            SudokuBezelRenderer.ApplyBezel(gameObject, theme);
        }
    }

    /// <summary>
    /// インスペクターのButton.onClickから呼び出される自律クリックハンドラ。
    /// </summary>
    public void OnClickSelf() {
        if (SudokuGameStandalone.Instance != null) {
            SudokuGameStandalone.Instance.OnInputButtonClicked(Value);
        }
    }
}
