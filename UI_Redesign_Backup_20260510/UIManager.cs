using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// 画面の管理とシーン遷移の司令塔となるクラス。
/// 実際のUI制御は専門の各コントローラーに委譲します。
/// </summary>
public class UIManager : MonoBehaviour
{
    private static UIManager instance;
    public static UIManager Instance {
        get {
            if (instance == null) {
                instance = UnityEngine.Object.FindAnyObjectByType<UIManager>(FindObjectsInactive.Include);
            }
            return instance;
        }
    }

    [Header("Controllers")]
    public SudokuPanelManager panelManager;
    public SudokuMenuController menuController;
    public SudokuGameHUD gameHUD;
    public SudokuInputPanelController inputPanelController;

    [Header("Settings")]
    public SudokuBoard sudokuBoard;

    private void Awake() {
        if (instance != null && instance != this && instance.gameObject.scene == this.gameObject.scene) {
            Destroy(gameObject);
            return;
        }
        instance = this;
        if (Camera.main != null) Camera.main.backgroundColor = Color.black;
    }

    private void Start() {
        string sceneName = SceneManager.GetActiveScene().name;
        Debug.Log($"【診断ログ】UIManager.Start: 現在のシーン名は [{sceneName}] です。");

        if (sudokuBoard == null) {
            Debug.LogWarning($"【亡霊アラート】UIManager({gameObject.name}) の sudokuBoard が空です。検索を開始します...");
            sudokuBoard = Object.FindAnyObjectByType<SudokuBoard>(FindObjectsInactive.Include);
            if (sudokuBoard != null) {
                Debug.Log($"【修復成功】SudokuBoard を発見しました: {sudokuBoard.gameObject.name} (Path: {GetPath(sudokuBoard.transform)})");
            } else {
                Debug.LogError("【修復失敗】シーン内に SudokuBoard が存在しません！");
            }
        }

        // パネルの初期状態設定
        if (sceneName.Contains("Menu")) {
            Debug.Log("【遷移判定】Menu用初期化を実行します。");
            InitializeMenu();
        } else if (sceneName.Contains("Game") || sceneName.Contains("Sample")) {
            Debug.Log("【遷移判定】Game用初期化を実行します。");
            InitializeGame();
        } else {
            Debug.LogWarning($"【遷移判定不明】シーン名 [{sceneName}] に合致する初期化がありません。");
        }

        // 勝敗イベントの購読
        if (GameManager.Instance != null) {
            GameManager.Instance.OnGameWon += () => ShowResult(true);
            GameManager.Instance.OnGameLost += () => ShowResult(false);
        }

        // 初期テーマ適用通知
        if (SudokuThemeManager.Instance != null) {
            SudokuThemeManager.Instance.NotifyThemeChanged(true);
        }
    }

    // --- Menu Button Helpers ---
    public void StartGameEasy() => StartGame(0, false);
    public void StartGameNormal() => StartGame(1, false);
    public void StartGameHard() => StartGame(2, false);
    public void StartGameExpert() => StartGame(3, false);
    public void ToggleTheme() {
        if (SudokuThemeManager.Instance != null) SudokuThemeManager.Instance.CycleTheme();
    }
    // ---------------------------

    private void Update() {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) {
            BackToMenu();
        }
    }

    private void InitializeMenu() {
        if (panelManager != null) {
            panelManager.HideAllPanels(); // メニュー以外のパネル（リザルト等）を確実に隠す
            panelManager.SetPanelActive("MenuPanel", true);
        }
    }

    private void InitializeGame() {
        if (panelManager != null) {
            Debug.Log("【初期化ログ1】GamePanelを表示状態に設定します。");
            panelManager.HideAllPanels(); // メニュー等の他パネルを確実に隠す
            // GamePanelとその主要な子パネルを表示
            panelManager.SetPanelActive("GamePanel", true);
            panelManager.SetPanelActive("TopPanel", true);
            panelManager.SetPanelActive("BoardPanel", true);
            panelManager.SetPanelActive("InputPanel", true);
            
            panelManager.SetPanelActive("ResultPanel", false);
        } else {
            Debug.LogError("【初期化エラー】panelManager が NULL です！");
        }

        // 入力パネルの初期化（コールバック登録）
        if (inputPanelController != null) {
            inputPanelController.Initialize(OnInputNumberClicked);
        }

        // ゲームステートに基づく初期化
        if (SudokuGameState.NeedsInitialization) {
            StartGameInternal(SudokuGameState.SelectedDifficulty, SudokuGameState.IsUnlimitedMode);
            SudokuGameState.NeedsInitialization = false;
        } else {
            // テスト用オートスタート（任意）
            StartGameInternal(SudokuLogic.Difficulty.Easy, false);
        }
    }

    public void StartGame(int difficultyIndex, bool isUnlimited) {
        Debug.Log($"【シーン遷移ログ1】StartGame: 難易度[{difficultyIndex}]で開始。GameSceneへ遷移を試みます。");
        SudokuGameState.SelectedDifficulty = (SudokuLogic.Difficulty)difficultyIndex;
        SudokuGameState.IsUnlimitedMode = isUnlimited;
        SudokuGameState.NeedsInitialization = true;

        if (Application.CanStreamedLevelBeLoaded("GameScene")) {
            Debug.Log("【シーン遷移ログ2】SceneManager.LoadScene(\"GameScene\") を実行します。");
            SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
        } else {
            Debug.LogWarning("【シーン遷移警告】GameSceneが見つかりません。InitializeGame()を直接実行します。");
            InitializeGame(); // シーン遷移しない場合は手動で初期化
            StartGameInternal(SudokuGameState.SelectedDifficulty, SudokuGameState.IsUnlimitedMode);
        }
    }

    private void StartGameInternal(SudokuLogic.Difficulty diff, bool unlimited) {
        Debug.Log($"【初期化ログ2】StartGameInternal: ゲームロジックを開始します。 GameManager.Instance = {(GameManager.Instance != null ? "OK" : "NULL")}");
        if (GameManager.Instance != null) {
            GameManager.Instance.StartNewGame(diff, unlimited);
        }
        
        if (gameHUD != null) {
            gameHUD.InitializeHUD(diff);
        }

        // 生成直後のテーマ適用（1フレーム待機して安定させる）
        StartCoroutine(ApplyThemeDeferred());
    }

    private IEnumerator ApplyThemeDeferred() {
        yield return null;
        if (SudokuThemeManager.Instance != null) {
            SudokuThemeManager.Instance.NotifyThemeChanged(true);
        }
    }

    private void OnInputNumberClicked(int number) {
        Debug.Log($"[UIManager] OnInputNumberClicked: Value={number}");
        if (sudokuBoard == null) {
            Debug.LogWarning("[UIManager] sudokuBoard is NULL!");
            return;
        }

        // 特殊コマンド（0はセルの消去、負の数はデバッグ用など）

        // ユーザー様テスト用：Cボタン(-2)で強制勝利（クリア画面遷移）
        if (number == -2) {
            Debug.Log("[UIManager] C-Button Pressed: Triggering Test Win Transition.");
            if (GameManager.Instance != null) GameManager.Instance.OnWin();
            return;
        }
        
        var selected = sudokuBoard.SelectedCell;
        if (selected != null) {
            sudokuBoard.InputNumber(number);
            // 入力後にボタンの状態（カウント制限）を更新
            if (inputPanelController != null) {
                inputPanelController.UpdateButtonStates(sudokuBoard, selected);
            }
        }
    }

    private void ShowResult(bool won) {
        if (panelManager != null) {
            panelManager.SetPanelActive("ResultPanel", true);
            // ゲーム画面を隠すか、重ねるかは設計によりますが、一旦重ねて表示します
            // 必要に応じて panelManager.SetPanelActive("GamePanel", false); を追加
        }
    }

    public void BackToMenu() {
        if (GameManager.Instance != null) GameManager.Instance.AbortGame();

        if (Application.CanStreamedLevelBeLoaded("MenuScene")) {
            SceneManager.LoadScene("MenuScene", LoadSceneMode.Single);
        } else {
            InitializeMenu();
        }
    }

    public void ShowMenu() {
        BackToMenu();
    }
    private string GetPath(Transform t) {
        if (t.parent == null) return t.name;
        return GetPath(t.parent) + "/" + t.name;
    }
}
