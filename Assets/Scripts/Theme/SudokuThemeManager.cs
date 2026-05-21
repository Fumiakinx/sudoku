using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// ゲーム全体のテーマ状態（選択中のテーマ）を管理するクラスです。
/// </summary>
public class SudokuThemeManager : MonoBehaviour
{
    private static SudokuThemeManager instance;
    public static SudokuThemeManager Instance {
        get {
            if (instance == null) {
                instance = UnityEngine.Object.FindAnyObjectByType<SudokuThemeManager>(FindObjectsInactive.Include);
            }
            return instance;
        }
    }

    // アプリ起動後に最初の初期化が行われたかを記録する静的フラグ
    private static bool isAppFirstInitialized = false;

    [Header("Data Source")]
    public SudokuData sudokuData;

    /// <summary>
    /// 現在適用されているテーマを取得します。
    /// </summary>
    public SudokuData.SudokuTheme CurrentTheme => sudokuData != null ? sudokuData.CurrentTheme : default;

    /// <summary>
    /// テーマが変更されたときに通知されるイベントです。
    /// bool引数は初期化時（isInitial）かどうかを示します。
    /// </summary>
    public static event Action<SudokuData.SudokuTheme, bool> OnThemeChanged;

    private void Awake() {
        instance = this;
        
        if (!isAppFirstInitialized) {
            // アプリ起動後の最初のシーンロード時のみ、強制的にNixieテーマを選択する
            isAppFirstInitialized = true;
            if (sudokuData != null && sudokuData.themes != null) {
                int nixieIndex = -1;
                for (int i = 0; i < sudokuData.themes.Length; i++) {
                    if (sudokuData.themes[i].themeName == "Nixie") {
                        nixieIndex = i;
                        break;
                    }
                }
                if (nixieIndex != -1) {
                    sudokuData.selectedThemeIndex = nixieIndex;
                    SudokuGameState.SelectedThemeIndex = nixieIndex;
                } else {
                    // Nixieテーマが見つからない場合は以前のテーマを復元
                    sudokuData.selectedThemeIndex = SudokuGameState.SelectedThemeIndex;
                }
            } else if (sudokuData != null) {
                sudokuData.selectedThemeIndex = SudokuGameState.SelectedThemeIndex;
            }
        } else {
            // 2回目以降（シーン遷移時やメニューに戻った際など）は、ユーザーが選んだテーマを正しく復元
            if (sudokuData != null) {
                sudokuData.selectedThemeIndex = SudokuGameState.SelectedThemeIndex;
            }
        }

        // 起動時のテーマを通知
        NotifyThemeChanged(true);
    }

    private bool _isNotifying = false;
    public bool IsNotifying => _isNotifying;

    /// <summary>
    /// テーマ変更を全システムに通知します。
    /// </summary>
    public void NotifyThemeChanged(bool isInitial = false) {
        if (_isNotifying) {
            Debug.LogWarning("[DIAGNOSTIC] SudokuThemeManager: Notification loop detected and blocked.");
            return;
        }
        _isNotifying = true;
        
        try {
            SudokuData.SudokuTheme theme = CurrentTheme;
            var invocationList = OnThemeChanged?.GetInvocationList();
            int count = invocationList?.Length ?? 0;
            if (invocationList != null) {
                for (int i = 0; i < invocationList.Length; i++) {
                    var subscriber = invocationList[i];
                    ((System.Action<SudokuData.SudokuTheme, bool>)subscriber).Invoke(theme, isInitial);
                }
            }
            
            // 通知完了
        } catch (System.Exception e) {
            Debug.LogError($"[THEME-LOG] CRITICAL EXCEPTION during NotifyThemeChanged: {e.Message}\n{e.StackTrace}");
        } finally {
            _isNotifying = false;
        }
    }

    /// <summary>
    /// 指定された名前のテーマに切り替えます（メカニカル系のみ許可）。
    /// </summary>
    public void SetTheme(string themeName) {
        if (sudokuData == null || sudokuData.themes == null) return;
        
        // 現在のテーマ名をチェックして重複を防止
        if (CurrentTheme.themeName == themeName) return;

        for (int i = 0; i < sudokuData.themes.Length; i++) {
            var theme = sudokuData.themes[i];
            if (theme.themeName == themeName) {
                // Any theme type can now be selected

                sudokuData.selectedThemeIndex = i;
                SudokuGameState.SelectedThemeIndex = i;
                NotifyThemeChanged(false);
                Debug.Log($"[SudokuThemeManager] テーマを切り替えました: {themeName}");
                return;
            }
        }
    }

    /// <summary>
    /// 次の利用可能なメカニカルテーマに切り替えます。
    /// </summary>
    public void CycleTheme() {
        if (sudokuData == null || sudokuData.themes == null || sudokuData.themes.Length == 0) return;
        
        int next = sudokuData.selectedThemeIndex;
        for (int i = 0; i < sudokuData.themes.Length; i++) {
            next = (next + 1) % sudokuData.themes.Length;
            var theme = sudokuData.themes[next];

            // アンロック済み（!isLocked）のテーマを対象とする
            if (!theme.isLocked) {
                sudokuData.selectedThemeIndex = next;
                SudokuGameState.SelectedThemeIndex = next;
                NotifyThemeChanged(false);
                Debug.Log($"[SudokuThemeManager] 次のテーマに切り替えました: {theme.themeName}");
                return;
            }
        }
    }
}
