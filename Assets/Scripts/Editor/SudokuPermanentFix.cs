using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEditor.SceneManagement;
using UnityEditor.Events;

public class SudokuPermanentFix : EditorWindow
{



    [MenuItem("Sudoku/Force Cleanup Scene Overrides")]
    public static void ExecuteFix()
    {
        Debug.Log("[CLEANUP] Starting ULTRA Deep Fix...");
        try {
            // 1. Missing Script の掃除
            var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            int missingCount = 0;
            foreach (var go in allObjects)
            {
                if (PrefabUtility.IsPartOfPrefabAsset(go)) continue;
                int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                if (removed > 0) missingCount += removed;
            }

            // 2. SerializedObject を使用した強制上書き
            var cells = Resources.FindObjectsOfTypeAll<SudokuCell>();
            int cellCount = 0;

            foreach (var cell in cells)
            {
                if (PrefabUtility.IsPartOfPrefabAsset(cell)) continue;
                
                // 参照の修復
                if (cell.bg == null) cell.bg = cell.GetComponent<Image>();
                if (cell.button == null) cell.button = cell.GetComponent<Button>();
                if (cell.display == null) cell.display = cell.GetComponent<SudokuDigitDisplay>();
                if (cell.rectTransform == null) cell.rectTransform = cell.GetComponent<RectTransform>();

                // SerializedObject で m_Enabled を強制的に true にする
                SerializedObject so = new SerializedObject(cell);
                so.FindProperty("m_Enabled").boolValue = true;
                
                // 参照もシリアライズデータとして保存
                if (cell.bg != null) so.FindProperty("bg").objectReferenceValue = cell.bg;
                if (cell.button != null) so.FindProperty("button").objectReferenceValue = cell.button;
                if (cell.display != null) so.FindProperty("display").objectReferenceValue = cell.display;
                if (cell.rectTransform != null) so.FindProperty("rectTransform").objectReferenceValue = cell.rectTransform;

                so.ApplyModifiedProperties();

                // Button と Image も同様に強制
                if (cell.button != null) {
                    SerializedObject soBtn = new SerializedObject(cell.button);
                    soBtn.FindProperty("m_Enabled").boolValue = true;
                    soBtn.ApplyModifiedProperties();
                }
                if (cell.bg != null) {
                    SerializedObject soImg = new SerializedObject(cell.bg);
                    soImg.FindProperty("m_Enabled").boolValue = true;
                    soImg.ApplyModifiedProperties();
                }

                cellCount++;
            }

            // シーンを強制保存
            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene);
            
            Debug.Log($"[CLEANUP] ULTRA Fix Completed. {cellCount} cells forced. Saved: {saved}");
        } catch (System.Exception e) {
            Debug.LogError($"[CLEANUP] Failed: {e.Message}");
        }
    }




    [MenuItem("Sudoku/ULTRA REPAIR: All UI Events")]
    public static void UltraRepairAllUIEvents()
    {
        Debug.Log("<color=cyan>[ULTRA-REPAIR] Starting Total UI Restoration (Deep Mode)...</color>");

        string[] prefabPaths = {
            "Assets/Prefabs/SudokuCell.prefab",
            "Assets/Prefabs/GameUI_Stable.prefab",
            "Assets/Prefabs/MenuUI_Stable.prefab"
        };

        foreach (var path in prefabPaths)
        {
            // プレハブの内容を編集モードでロード
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root == null) continue;

            try {
                Debug.Log($"[ULTRA-REPAIR] Processing Prefab Asset: {path}");
                
                // 1. セル群の修復 (プレハブ内に埋め込まれた全てのセルを対象にする)
                var allCells = root.GetComponentsInChildren<SudokuCell>(true);
                foreach (var cell in allCells) {
                    RepairButton(cell.gameObject, cell, "OnCellClicked");
                }

                // 2. 入力ボタンの修復
                var inputButtons = root.GetComponentsInChildren<SudokuInputButton>(true);
                foreach (var inputBtn in inputButtons) {
                    RepairButton(inputBtn.gameObject, inputBtn, "OnClickSelf");
                }

                // 3. HUDボタンの修復と参照復旧
                var hud = root.GetComponent<SudokuGameHUD>();
                if (hud != null) {
                    // 参照がNULLなら名前で探す
                    if (hud.restartButton == null) hud.restartButton = FindInHierarchy<Button>(root, "Btn_Restart");
                    if (hud.backToMenuButton == null) hud.backToMenuButton = FindInHierarchy<Button>(root, "Btn_BackToMenu");

                    if (hud.restartButton != null) RepairButton(hud.restartButton.gameObject, hud, "OnRestartButtonClicked");
                    if (hud.backToMenuButton != null) RepairButton(hud.backToMenuButton.gameObject, hud, "OnMenuButtonClicked");
                }

                // 4. メニューコントローラーの修復
                var menuCtrl = root.GetComponent<SudokuMenuController>();
                if (menuCtrl != null) {
                    if (menuCtrl.easyButton != null) RepairButton(menuCtrl.easyButton.gameObject, menuCtrl, "OnEasyClicked");
                    if (menuCtrl.normalButton != null) RepairButton(menuCtrl.normalButton.gameObject, menuCtrl, "OnNormalClicked");
                    if (menuCtrl.hardButton != null) RepairButton(menuCtrl.hardButton.gameObject, menuCtrl, "OnHardClicked");
                    if (menuCtrl.expertButton != null) RepairButton(menuCtrl.expertButton.gameObject, menuCtrl, "OnExpertClicked");
                    if (menuCtrl.themeChangeButton != null) RepairButton(menuCtrl.themeChangeButton.gameObject, menuCtrl, "OnThemeButtonClicked");
                }

                // プレハブを保存してアンロード
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log($"[ULTRA-REPAIR] Saved changes to: {path}");
            } finally {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
        AssetDatabase.Refresh();

        // 4. シーン内のインスタンスをプレハブに強制同期（リバート）
        int revertCount = 0;
        var allButtons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include);
        foreach (var btn in allButtons)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(btn))
            {
                PrefabUtility.RevertObjectOverride(btn, InteractionMode.AutomatedAction);
                var so = new SerializedObject(btn);
                var prop = so.FindProperty("m_OnClick");
                if (prop != null) PrefabUtility.RevertPropertyOverride(prop, InteractionMode.AutomatedAction);
                revertCount++;
            }
        }

        // 5. シーン固有のコントローラー修復 (プレハブ化されていない、またはオーバーライドが必要なケース)
        var resControllers = Object.FindObjectsByType<SudokuResultController>(FindObjectsInactive.Include);
        foreach (var res in resControllers) {
            if (res.menuButton != null) RepairButton(res.menuButton.gameObject, res, "OnMenuButtonClicked");
        }

        var menuControllers = Object.FindObjectsByType<SudokuMenuController>(FindObjectsInactive.Include);
        foreach (var mc in menuControllers) {
            if (mc.easyButton != null) RepairButton(mc.easyButton.gameObject, mc, "OnEasyClicked");
            if (mc.normalButton != null) RepairButton(mc.normalButton.gameObject, mc, "OnNormalClicked");
            if (mc.hardButton != null) RepairButton(mc.hardButton.gameObject, mc, "OnHardClicked");
            if (mc.expertButton != null) RepairButton(mc.expertButton.gameObject, mc, "OnExpertClicked");
            if (mc.themeChangeButton != null) RepairButton(mc.themeChangeButton.gameObject, mc, "OnThemeButtonClicked");
        }

        Debug.Log($"<color=green>[ULTRA-REPAIR] SUCCESS! {revertCount} scene buttons synchronized.</color>");
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
    }

    private static T FindInHierarchy<T>(GameObject root, string name) where T : Component
    {
        var all = root.GetComponentsInChildren<T>(true);
        foreach (var c in all) if (c.name == name) return c;
        return null;
    }

    private static Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent) {
            if (child.name == name) return child;
            var result = FindChildRecursive(child, name);
            if (result != null) return result;
        }
        return null;
    }

    private static void RepairButton(GameObject go, MonoBehaviour target, string methodName)
    {
        var btn = go.GetComponent<Button>();
        if (btn == null || target == null) return;

        // 既存のリスナーを一旦クリア
        while (btn.onClick.GetPersistentEventCount() > 0) {
            UnityEventTools.RemovePersistentListener(btn.onClick, 0);
        }

        // 静的リスナーの追加
        try {
            UnityEventTools.AddVoidPersistentListener(btn.onClick, (UnityEngine.Events.UnityAction)System.Delegate.CreateDelegate(typeof(UnityEngine.Events.UnityAction), target, methodName));
            Debug.Log($"  [Repair] Linked {go.name} -> {target.GetType().Name}.{methodName}");
        } catch (System.Exception e) {
            Debug.LogWarning($"  [Repair-Fallback] Failed to create delegate for {methodName} on {go.name}: {e.Message}");
        }
    }

    private static void RevertEnabledOverride(Object target)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty("m_Enabled");
        if (prop != null && prop.isInstantiatedPrefab)
        {
            PrefabUtility.RevertPropertyOverride(prop, InteractionMode.AutomatedAction);
        }
    }
}
