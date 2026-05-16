/// <summary>
/// UIのサイズやフォント設定など、プロジェクト全体の定数を管理します。
/// </summary>
public static class SudokuUIConstants
{
    // セルとベゼルの基本サイズ
    public const float CELL_SIZE = 110f;
    public const float BEZEL_THICKNESS = 6f;

    // パネルの基準サイズ
    public const float WIDTH_GAME = 1080f;
    public const float HEIGHT_GAME = 1920f;
    public const float WIDTH_TOP = 1060f;
    public const float HEIGHT_TOP = 120f;
    public const float WIDTH_BOARD = 1060f;
    public const float HEIGHT_BOARD = 1060f;
    public const float WIDTH_INPUT = 1000f;
    public const float HEIGHT_INPUT = 380f;

    // フォントサイズ
    public const int LABEL_FONT_SIZE_CELL = 44;
    public const int LABEL_FONT_SIZE_TITLE = 144;
    public const int LABEL_FONT_SIZE_TIMER = 22;
    public const float TIMER_SIZE = 55f;
}
