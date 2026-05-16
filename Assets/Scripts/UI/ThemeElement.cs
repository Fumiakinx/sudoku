using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ThemeElement : MonoBehaviour
{
    [Header("Basic Settings")]
    public bool styleImage = true;
    public bool styleText = true;
    public bool styleBevel = true; // 他のスクリプトで使用されているため復活

    protected virtual void OnEnable()
    {
        SudokuThemeManager.OnThemeChanged += HandleThemeChanged;
    }

    protected virtual void Start()
    {
        // シーン開始時に、現在のテーマを確実に反映させる
        if (SudokuThemeManager.Instance != null)
        {
            Refresh();
        }
    }

    protected virtual void OnDisable()
    {
        SudokuThemeManager.OnThemeChanged -= HandleThemeChanged;
    }

    public virtual void Refresh()
    {
        if (SudokuThemeManager.Instance != null)
        {
            HandleThemeChanged(SudokuThemeManager.Instance.CurrentTheme, false);
        }
    }

    public void ManualApplyTheme(SudokuData.SudokuTheme theme)
    {
        HandleThemeChanged(theme, false);
    }

    protected virtual void HandleThemeChanged(SudokuData.SudokuTheme theme, bool isInitial)
    {
        if (string.IsNullOrEmpty(theme.themeName)) return;
        Debug.Log($"<color=white>[ThemeElement] HandleThemeChanged START: {gameObject.name}</color>");
        
        // 1. テキストの色同期
        if (styleText)
        {
            var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in texts)
            {
                SudokuLabelStyler.ApplyStyle(t, theme);
            }
        }

        // 2. 枠線（ベゼル）の色と表示同期
        if (styleBevel)
        {
            SudokuBezelRenderer.ApplyBezel(gameObject, theme);
        }

        // 3. 各種Imageの色同期
        if (styleImage)
        {
            var images = GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                if (img == null) continue;
                string objName = img.gameObject.name;
                
                // 【重要】枠線パーツ、数字スプライト、およびそれらの管理用オブジェクトはスキップ
                // これらは SudokuBezelRenderer や SudokuDigitDisplay が個別に管理するため、上書きを禁止する。
                if (objName.StartsWith("_B_") || objName.Contains("Bezel") || objName.Contains("Digit")) {
                    continue;
                }
                
                // オブジェクト名に基づいてテーマカラーを割り当てる
                if (objName.Contains("Background") || objName.Contains("Grid") || objName.Contains("Board")) {
                    img.color = theme.backgroundColor;
                } else if (objName.Contains("Panel") || (objName.Contains("Block") && !objName.Contains("Cell"))) {
                    img.color = theme.panelColor;
                } else if (img.GetComponent<Button>() != null && !objName.Contains("Cell")) {
                    // メニューボタン本体などは透明にする
                    img.color = new Color(0,0,0,0);
                } else if (img.GetComponentInParent<SudokuCell>() != null || img.GetComponentInParent<Button>() != null) {
                    img.color = theme.cellColorNormal;
                } else {
                    // 特定できない要素はテキストカラーに合わせる（予備）
                    img.color = theme.textColor;
                }
            }
        }
        Debug.Log($"<color=white>[ThemeElement] HandleThemeChanged END: {gameObject.name}</color>");
    }
}
