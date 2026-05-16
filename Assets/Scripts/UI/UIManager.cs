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
    public SudokuGameStandalone sudokuBoard;

    private void Awake() {
        if (instance != null && instance != this && instance.gameObject.scene == this.gameObject.scene) {
            Destroy(gameObject);
            return;
        }
        instance = this;
        if (Camera.main != null) Camera.main.backgroundColor = Color.black;

        // コンポーネントの自動紐付け（分割UI対応）
        if (panelManager == null) panelManager = GetComponentInChildren<SudokuPanelManager>(true);
        if (menuController == null) menuController = GetComponentInChildren<SudokuMenuController>(true);
        if (gameHUD == null) gameHUD = GetComponentInChildren<SudokuGameHUD>(true);
        if (inputPanelController == null) inputPanelController = GetComponentInChildren<SudokuInputPanelController>(true);
    }

    private void Start() {
        Debug.Log("[LIFE-LOG] UIManager.Start ENTER");
        string sceneName = SceneManager.GetActiveScene().name;
        Debug.Log($"【診断ログ】UIManager.Start: 現在のシーン名は [{sceneName}] です。");

        // 自動検索の再実行（Start時点でも念のため）
        if (panelManager == null) panelManager = GetComponentInChildren<SudokuPanelManager>(true);

        if (sudokuBoard == null) {
            sudokuBoard = Object.FindAnyObjectByType<SudokuGameStandalone>(FindObjectsInactive.Include);
        }

        // パネルの初期状態設定
        if (sceneName.Contains("Menu")) {
            InitializeMenu();
        } else if (sceneName.Contains("Game") || sceneName.Contains("Sample")) {
            InitializeGame();
        }

        if (GameManager.Instance != null) {
            GameManager.Instance.OnGameWon += () => ShowResult(true);
            GameManager.Instance.OnGameLost += () => ShowResult(false);
        }

        if (SudokuThemeManager.Instance != null) {
            SudokuThemeManager.Instance.NotifyThemeChanged(true);
        }
    }

    private static int _lastEscFrame = -1;
    private void Update() {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) {
            // 同一フレーム内での二重処理を防止（チャタリング対策）
            if (Time.frameCount == _lastEscFrame) return;
            _lastEscFrame = Time.frameCount;

            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            
            // すでにメニューにいる場合は、シーンの再ロードを行わない（将来的にここでアプリ終了処理などを追加可能）
            if (sceneName == "MenuScene") {
                Debug.Log("[TRACE] UIManager: ESC detected in MenuScene. Ignoring to prevent recursive load.");
                return;
            }

            Debug.Log($"<color=red>[TRACE] UIManager.Update (Frame:{Time.frameCount}): ESC key detected in Scene [{sceneName}]. Transitioning to Menu.</color>");
            BackToMenu();
        }
    }

    private void InitializeMenu() {
        Debug.Log("[TRACE] UIManager.InitializeMenu ENTER");
        if (panelManager != null) {
            Debug.Log("[TRACE] UIManager.InitializeMenu: Activating MenuPanel");
            panelManager.SetPanelActive("MenuPanel", true);
        } else {
            Debug.LogWarning("[TRACE] UIManager.InitializeMenu: panelManager is NULL!");
        }
    }

    private void InitializeGame() {
        if (panelManager != null) {
            panelManager.SetPanelActive("GamePanel", true);
            panelManager.SetPanelActive("TopPanel", true);
            Debug.Log("[UIManager-Check] Before SetPanelActive(InputPanel, true)");
            panelManager.SetPanelActive("BoardPanel", true);
            panelManager.SetPanelActive("InputPanel", true);
            Debug.Log("[UIManager-Check] After SetPanelActive(InputPanel, true)");
            // ResultPanelは別シーンへ移動したため、ここでの制御は不要です
            // panelManager.SetPanelActive("ResultPanel", false);

        }

        if (inputPanelController != null) {
            // 静的UIでは個別の初期化は不要。必要に応じて状態更新のみ行う
            // inputPanelController.Initialize(OnInputNumberClicked); 
        }

        if (SudokuGameState.NeedsInitialization) {
            StartGameInternal(SudokuGameState.SelectedDifficulty, SudokuGameState.IsUnlimitedMode);
            SudokuGameState.NeedsInitialization = false;
        } else {
            StartGameInternal(SudokuLogic.Difficulty.Easy, false);
        }
    }

    public void StartGame(int difficultyIndex, bool isUnlimited) {
        Debug.Log($"【シーン遷移ログ1】StartGame: 難易度[{difficultyIndex}]で開始。GameSceneへ遷移を試みます。");
        SudokuGameState.SelectedDifficulty = (SudokuLogic.Difficulty)difficultyIndex;
        SudokuGameState.IsUnlimitedMode = isUnlimited;
        SudokuGameState.NeedsInitialization = true;

        Debug.Log($"[DEBUG] UIManager.StartGame: Attempting to load 'GameScene'. SceneManager state: {SceneManager.GetActiveScene().name}, EventSystem.current={(UnityEngine.EventSystems.EventSystem.current != null ? UnityEngine.EventSystems.EventSystem.current.name : "NULL")}");
        Debug.Log($"[DIAGNOSTIC] Total EventSystems in scene: {FindObjectsByType<UnityEngine.EventSystems.EventSystem>().Length}");


        Debug.Log("【シーン遷移ログ2】SceneManager.LoadScene(\"GameScene\") を実行します。");
        SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
        Debug.Log("【シーン遷移ログ3】SceneManager.LoadScene 呼び出し直後。");
    }

    private void StartGameInternal(SudokuLogic.Difficulty diff, bool unlimited) {
        Debug.Log($"<color=yellow>【UIManager】StartGameInternal ENTER: diff={diff}, unlimited={unlimited}</color>");
        Debug.Log($"【初期化ログ2】StartGameInternal: ゲームロジックを開始します。 GameManager.Instance = {(GameManager.Instance != null ? "OK" : "NULL")}");
        if (GameManager.Instance != null) {
            Debug.Log("[UIManager] Calling GameManager.Instance.StartNewGame...");
            GameManager.Instance.StartNewGame(diff, unlimited);
            Debug.Log("[UIManager] GameManager.Instance.StartNewGame returned.");
        } else {
            Debug.LogError("[UIManager] CRITICAL ERROR: GameManager.Instance is NULL!");
        }
        
        if (gameHUD != null) {
            Debug.Log("[UIManager] Initializing HUD...");
            gameHUD.InitializeHUD(diff);
        }

        // 生成直後のテーマ適用（1フレーム待機して安定させる）
        Debug.Log("[UIManager] Starting ApplyThemeDeferred coroutine...");
        StartCoroutine(ApplyThemeDeferred());
        Debug.Log("<color=yellow>【UIManager】StartGameInternal EXIT</color>");
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
            sudokuBoard.OnInputButtonClicked(number);
            // 入力後にボタンの状態（カウント制限）を更新
            if (inputPanelController != null) {
                inputPanelController.UpdateButtonStates(sudokuBoard, selected);
            }
        }
    }

    private void ShowResult(bool won) {
        // 別シーン (ResultScene) に遷移するため、同一シーン内でのパネル操作は不要。
        // GameManager がシーン遷移をハンドルします。
        /*
        if (panelManager != null) {
            panelManager.SetPanelActive("ResultPanel", true);
        }
        */
    }

    public void BackToMenu() {
        Debug.Log("<color=yellow>[TRACE] UIManager.BackToMenu: Aborting game and loading MenuScene...</color>");
        if (GameManager.Instance != null) GameManager.Instance.AbortGame();

        if (UnityEngine.Application.CanStreamedLevelBeLoaded("MenuScene")) {
            UnityEngine.SceneManagement.SceneManager.LoadScene("MenuScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
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
