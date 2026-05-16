using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "SudokuData", menuName = "Sudoku/SudokuData")]
public class SudokuData : ScriptableObject
{
    public enum ThemeDisplayType {
        Normal,     // スプライト＋テキスト
        Nixie,      // ニキシー管コンポーネント
        LED7Seg,    // 7セグメントLEDコンポーネント
        Mechanical, // ドラム式（メカニカル）
        FlipFlap    // パタパタ（反転フラップ式）
    }

    [System.Serializable]
    public struct SudokuTheme
    {
        public string themeName;
        public ThemeDisplayType displayType; // 表示ロジックの指定
        public Sprite[] sprites; 
        public Sprite[] allSprites;   // フォルダ内の全スプライト (中間フレーム Anim_* を含む)
        public Sprite[] topSprites;
        public Sprite[] bottomSprites;
        public Color backgroundColor;
        public Color panelColor;
        public Color textColor;       // 追加：文字色
        public Color cellColorFixed;
        public Color cellColorNormal;
        public Color lineColor;
        public Color highlightColor;
        public Color shadowColor;
        public Color selectionColor;  // 追加：選択中の色
        public Color relatedColor;    // 追加：関連するセルの色
        public Color sameDigitColor;  // 追加：同じ数字の色
        public Color errorColor;      // 追加：エラー時の色
        public bool showBezel;        // 追加：ベゼルの有無
        public float previewInterval;  // 追加：プレビューの速度
        public bool hasBlank;         // 追加：空白スプライトの有無
        public bool isLocked;
        public float bevelWidth;      // 追加：ベゼルの厚さ
        public Color relatedHighlightColor; // 追加：関連セルの強調色（未設定なら自動計算）
        public Color correctMarkColor; // 正解時の「丸」の色
        public Color errorMarkColor;   // 不正解時の「バツ」の色
    }

    public SudokuTheme[] themes;
    public int selectedThemeIndex = 0; // デフォルトを0に
    public int defaultMistLimit = 3;

    public SudokuTheme CurrentTheme => (themes != null && selectedThemeIndex >= 0 && selectedThemeIndex < themes.Length) 
        ? themes[selectedThemeIndex] 
        : default;
}
