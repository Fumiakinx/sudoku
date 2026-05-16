
using UnityEngine;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class SudokuSetupTool : EditorWindow {
    [MenuItem("Tools/Sudoku/Full Scene Setup")]
    public static void SetupScene() {
        var standalone = GameObject.FindAnyObjectByType<SudokuGameStandalone>();
        if (standalone == null) {
            Debug.LogError("[SudokuSetup] SudokuGameStandalone not found in scene.");
            return;
        }

        Undo.RecordObject(standalone, "Setup Sudoku Scene");

        // 定数
        float cellSize = SudokuUIConstants.CELL_SIZE;
        float blockSpacing = 10f; 
        float totalGridSize = (cellSize * 9) + (blockSpacing * 2);
        float offset = totalGridSize / 2f;
        float bezelThickness = 6f;

        // 1. BoardPanel のセットアップ
        var boardPanel = GameObject.Find("BoardPanel");
        if (boardPanel != null) {
            SetupBezelRecursive(boardPanel.transform, bezelThickness);
        }

        // セルの配置
        var allCells = GameObject.FindObjectsByType<SudokuCell>(FindObjectsInactive.Include);
        var sortedCells = allCells
            .Where(c => c.name.StartsWith("Cell_"))
            .OrderBy(c => {
                var parts = c.name.Split('_');
                int r = int.Parse(parts[1]);
                int c_idx = int.Parse(parts[2]);
                return r * 10 + c_idx;
            })
            .ToList();

        foreach (var cell in sortedCells) {
            var parts = cell.name.Split('_');
            int r = int.Parse(parts[1]);
            int c = int.Parse(parts[2]);

            var rt = cell.GetComponent<RectTransform>();
            if (rt != null) {
                Undo.RecordObject(rt, "Align Cell");
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(cellSize, cellSize);
                float x = (c * cellSize) + (Mathf.Floor(c / 3) * blockSpacing) - offset + (cellSize / 2f);
                float y = -((r * cellSize) + (Mathf.Floor(r / 3) * blockSpacing) - offset + (cellSize / 2f));
                rt.anchoredPosition = new Vector2(x, y);
            }

            var btn = cell.GetComponent<Button>();
            if (btn != null) {
                while (btn.onClick.GetPersistentEventCount() > 0) UnityEventTools.RemovePersistentListener(btn.onClick, 0);
                UnityEventTools.AddVoidPersistentListener(btn.onClick, cell.OnCellClicked);
            }
            cell.RefreshUI(true);
            EditorUtility.SetDirty(cell);
        }
        standalone.serializedCells = sortedCells;

        // 2. InputPanel のセットアップ
        var inputButtons = new List<Button>();
        var inputPanel = GameObject.Find("InputPanel");
        if (inputPanel != null) {
            var panelRT = inputPanel.GetComponent<RectTransform>();
            if (panelRT != null) {
                Undo.RecordObject(panelRT, "Resize InputPanel");
                panelRT.anchorMin = panelRT.anchorMax = new Vector2(0.5f, 0.5f); // 画面中央基準に変更（より安定）
                panelRT.pivot = new Vector2(0.5f, 0.5f);
                panelRT.sizeDelta = new Vector2(1060f, 400f);
                panelRT.anchoredPosition = new Vector2(0f, -680f); // 画面下部へ（座標は環境に合わせて調整）
            }

            SetupBezelRecursive(inputPanel.transform, bezelThickness);

            float btnSize = cellSize;
            float btnSpacing = 50f;
            float rowWidth = (btnSize * 5) + (btnSpacing * 4);
            float rowOffset = rowWidth / 2f;

            string[] row1Names = { "Btn_1", "Btn_2", "Btn_3", "Btn_4", "Btn_5" };
            string[] row2Names = { "Btn_6", "Btn_7", "Btn_8", "Btn_9", "Btn_Clear" };

            for (int row = 0; row < 2; row++) {
                string[] names = (row == 0) ? row1Names : row2Names;
                float yPos = (row == 0) ? 80f : -80f;

                for (int i = 0; i < names.Length; i++) {
                    var btnObj = inputPanel.transform.Find(names[i]);
                    if (btnObj != null) {
                        var rt = btnObj.GetComponent<RectTransform>();
                        if (rt != null) {
                            Undo.RecordObject(rt, "Align Input Button");
                            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
                            rt.sizeDelta = new Vector2(btnSize, btnSize);
                            float xPos = (i * (btnSize + btnSpacing)) - rowOffset + (btnSize / 2f);
                            rt.anchoredPosition = new Vector2(xPos, yPos);
                        }

                        var btn = btnObj.GetComponent<Button>();
                        var inputBtn = btnObj.GetComponent<SudokuInputButton>();
                        if (btn != null && inputBtn != null) {
                            btn.enabled = true; // ボタン（UI）を確実に有効化
                            inputBtn.enabled = true; // ロジックコンポーネントを確実に有効化
                            while (btn.onClick.GetPersistentEventCount() > 0) UnityEventTools.RemovePersistentListener(btn.onClick, 0);
                            
                            // 自己完結型（OnClickSelf）への紐付けに変更
                            UnityEventTools.AddPersistentListener(btn.onClick, inputBtn.OnClickSelf);
                        }
                    }
                }
            }
        }

        // 全体の更新反映
        var allBezels = GameObject.FindObjectsByType<SudokuBezel>(FindObjectsInactive.Include);
        foreach(var b in allBezels) b.Refresh();

        EditorUtility.SetDirty(standalone);
        AssetDatabase.SaveAssets();
        Debug.Log("<b>[SudokuSetup] SUCCESS: True Static UI Architecture established!</b>");
    }

    private static void SetupBezelRecursive(Transform parent, float thickness) {
        var bezelRoot = parent.Find("_Bezel_");
        if (bezelRoot == null) return;

        var rt = bezelRoot.GetComponent<RectTransform>();
        Undo.RecordObject(rt, "Setup Bezel Root");
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);

        // 各パーツのアンカー矯正（ここが真の静的化の肝）
        SetupPart(bezelRoot, "_B_T", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -thickness), Vector2.zero);
        SetupPart(bezelRoot, "_B_B", new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, new Vector2(0, thickness));
        SetupPart(bezelRoot, "_B_L", new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, new Vector2(thickness, 0));
        SetupPart(bezelRoot, "_B_R", new Vector2(1, 0), new Vector2(1, 1), new Vector2(-thickness, 0), Vector2.zero);
    }

    private static void SetupPart(Transform root, string name, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax) {
        var t = root.Find(name);
        if (t == null) return;
        var rt = t.GetComponent<RectTransform>();
        Undo.RecordObject(rt, "Align Bezel Part");
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = offMin;
        rt.offsetMax = offMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
    }
}
