using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SudokuPanelManager : MonoBehaviour {
    [System.Serializable]
    public struct PanelInfo {
        public string name;
        public GameObject panelObject;
    }

    public List<PanelInfo> panels = new List<PanelInfo>();
    private Dictionary<string, GameObject> _panelDict = new Dictionary<string, GameObject>();

    void Awake() {
        Debug.Log("[SudokuPanelManager] Awake on " + gameObject.name + ". (Dynamic rebuild disabled to protect static hierarchy)");
        _panelDict.Clear();
        foreach (var info in panels) {
            if (info.panelObject != null) {
                _panelDict[info.name] = info.panelObject;
            }
        }
        Debug.Log($"[SudokuPanelManager] Initialization complete. {_panelDict.Count} panels loaded from static config.");
    }

    public void SetPanelActive(string panelName, bool active) {
        Debug.Log($"<color=orange>[TRACE] SudokuPanelManager.SetPanelActive: panelName={panelName}, active={active}</color>");
        if (_panelDict.TryGetValue(panelName, out var go)) {
            var group = go.GetComponent<CanvasGroup>();
            if (group != null) {
                group.alpha = active ? 1f : 0f;
                group.blocksRaycasts = active;
                group.interactable = active;
            }
        } else {
            Debug.LogWarning($"[SudokuPanelManager] Panel not found: {panelName}");
        }
    }
}
