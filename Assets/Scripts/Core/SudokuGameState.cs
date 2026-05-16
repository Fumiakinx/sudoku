public static class SudokuGameState
{
    public static SudokuLogic.Difficulty SelectedDifficulty = SudokuLogic.Difficulty.Easy;
    public static bool IsUnlimitedMode = false;
    public static bool NeedsInitialization = false;
    
    // リザルト画面で参照する勝敗フラグ
    public static bool LastGameWon = false;
    
    // 選択されたテーマのインデックスを保持（PlayerPrefsで永続化）
    public static int SelectedThemeIndex {
        get => UnityEngine.PlayerPrefs.GetInt("SelectedThemeIndex", 0);
        set {
            UnityEngine.PlayerPrefs.SetInt("SelectedThemeIndex", value);
            UnityEngine.PlayerPrefs.Save();
        }
    }
}
