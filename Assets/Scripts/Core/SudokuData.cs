using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "SudokuData", menuName = "Sudoku/SudokuData")]
public class SudokuData : ScriptableObject
{
    public enum ThemeDisplayType {
        Normal = 0,     // スプライト＋テキスト
        Nixie = 1,      // ニキシー管コンポーネント
        LED7Seg = 2,    // 7セグメントLEDコンポーネント
        Mechanical = 3, // メカニカル（反転フラップ式 / ドラム式）
        Roulette = 4    // 人生ゲーム（ルーレット）
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
        public Color sameDigitColor;  // 追加：同じ数字の色
        public Color errorColor;      // 追加：エラー時の色
        public bool showBezel;        // 追加：ベゼルの有無
        public float previewInterval;  // 追加：プレビューの速度
        public bool hasBlank;         // 追加：空白スプライトの有無
        public bool isLocked;
        public float bevelWidth;      // 追加：ベゼルの厚さ
        public Color correctMarkColor; // 正解時の「丸」の色
        public Color errorMarkColor;   // 不正解時の「バツ」の色
        public bool useOriginalSpriteColor; // 元画像の色をそのまま使うフラグ（チェックボックス）
        public Color originalSpriteBgColor; // 元画像色使用時の目に優しい背景カラー
    }

    public SudokuTheme[] themes;
    public int selectedThemeIndex = 0; // デフォルトを0に
    public int defaultMistLimit = 3;

    public SudokuTheme CurrentTheme => (themes != null && selectedThemeIndex >= 0 && selectedThemeIndex < themes.Length) 
        ? themes[selectedThemeIndex] 
        : default;
}
