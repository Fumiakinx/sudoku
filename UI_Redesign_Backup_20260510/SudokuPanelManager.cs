using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 各UIパネル（Menu, Game, HUD, Input, Result）の表示状態を一括管理するクラス。
/// </summary>
public class SudokuPanelManager : MonoBehaviour
{
    [System.Serializable]
    public struct PanelInfo {
        public string name;
        public GameObject panelObject;
    }

    [Header("Panels Configuration")]
    public List<PanelInfo> panels = new List<PanelInfo>();

    private Dictionary<string, GameObject> _panelDict = new Dictionary<string, GameObject>();

    private void Awake() {
        Debug.Log($"[SudokuPanelManager] Awake started on {gameObject.name}. Rebuilding panel map...");
        
        // シーン上の古いリスト設定を破棄し、常に最新の階層からパネルを自動構築する
        panels = new List<PanelInfo>();
        _panelDict.Clear();

        // GetComponentsInChildren(true) は非アクティブなオブジェクトも再帰的に取得する
        Transform[] allTransforms = GetComponentsInChildren<Transform>(true);
        Debug.Log($"[SudokuPanelManager] Found {allTransforms.Length} total transforms under {gameObject.name}");

        foreach (Transform t in allTransforms) {
            if (t == transform) continue;

            // 名前が "Panel" で終わる、または "Panel " (末尾スペース) などの微細な表記揺れを許容
            string cleanName = t.name.Trim();
            if (cleanName.EndsWith("Panel", System.StringComparison.OrdinalIgnoreCase)) {
                Debug.Log($"[SudokuPanelManager] Registered panel: {cleanName} (GameObject: {t.name})");
                PanelInfo info = new PanelInfo { name = cleanName, panelObject = t.gameObject };
                panels.Add(info);
                _panelDict[cleanName] = t.gameObject;
            }
        }

        Debug.Log($"[SudokuPanelManager] Initialization complete. {panels.Count} panels registered.");
    }

    /// <summary>
    /// 指定した名前のパネルを表示または非表示にします。
    /// </summary>
    public void SetPanelActive(string panelName, bool visible) {
        if (_panelDict.TryGetValue(panelName, out GameObject panel)) {
            // SetActive(false)はレイアウト計算を止めてしまう「毒」であるため廃止。
            // 常にActive状態を維持し、CanvasGroupで見せ方と操作性のみを制御する。
            CanvasGroup cg = panel.GetComponent<CanvasGroup>();
            if (cg == null) cg = panel.AddComponent<CanvasGroup>();

            cg.alpha = visible ? 1f : 0f;
            cg.interactable = visible;
            cg.blocksRaycasts = visible;
            
            // 物理的に座標を動かすなどの処理が必要な場合はここに追加可能ですが、
            // 現状はアルファとブロック設定で十分安定します。
        } else {
            Debug.LogWarning($"[SudokuPanelManager] Panel not found in list: {panelName}");
        }
    }

    /// <summary>
    /// 全てのパネルを非表示にします。
    /// </summary>
    public void HideAllPanels() {
        foreach (var name in _panelDict.Keys) {
            SetPanelActive(name, false);
        }
    }
}
