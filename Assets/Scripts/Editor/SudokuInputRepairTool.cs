using UnityEngine;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine.UI;

namespace Sudoku.EditorTools
{
    public class SudokuInputRepairTool : EditorWindow
    {
        [MenuItem("Sudoku/Final Repair (Self-Contained Buttons)")]
        public static void FinalRepair()
        {
            // 1. プレハブの取得
            string prefabPath = "Assets/Prefabs/SudokuInputButton.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            
            if (prefab == null) {
                Debug.LogError($"[RepairTool] Prefab not found at {prefabPath}");
                return;
            }

            // 2. プレハブの編集開始
            Button btn = prefab.GetComponent<Button>();
            SudokuInputButton inputBtn = prefab.GetComponent<SudokuInputButton>();

            if (btn == null || inputBtn == null) {
                Debug.LogError("[RepairTool] Button or SudokuInputButton component missing on prefab.");
                return;
            }

            // 3. 既存のイベントをクリア（動的なゴミを排除）
            while (btn.onClick.GetPersistentEventCount() > 0) {
                UnityEventTools.RemovePersistentListener(btn.onClick, 0);
            }

            // 4. 自分自身の OnClickSelf() を静的に紐付け
            // これにより、プレハブ自体が「自分が押されたら自分のメソッドを呼ぶ」という静的設定を持つ
            UnityEventTools.AddPersistentListener(btn.onClick, inputBtn.OnClickSelf);

            // 5. 変更を保存
            EditorUtility.SetDirty(prefab);
            AssetDatabase.SaveAssets();

            Debug.Log("<color=green>[RepairTool] SUCCESS: Prefab is now self-contained. Please apply to all scene instances if needed.</color>");
            
            // シーン内のインスタンスも念のため一掃してリセット
            RepairSceneInstances();
        }

        private static void RepairSceneInstances()
        {
            var sceneButtons = Object.FindObjectsByType<SudokuInputButton>(FindObjectsInactive.Include);
            foreach (var sb in sceneButtons)
            {
                var btn = sb.GetComponent<Button>();
                if (btn == null) continue;

                while (btn.onClick.GetPersistentEventCount() > 0) {
                    UnityEventTools.RemovePersistentListener(btn.onClick, 0);
                }
                UnityEventTools.AddPersistentListener(btn.onClick, sb.OnClickSelf);
                
                EditorUtility.SetDirty(sb.gameObject);
            }
            Debug.Log($"[RepairTool] Repaired {sceneButtons.Length} instances in the current scene.");
        }
    }
}
