using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class SudokuInputDebugger : MonoBehaviour {
    void Update() {
        if (UnityEngine.InputSystem.Pointer.current != null && UnityEngine.InputSystem.Pointer.current.press.wasPressedThisFrame) {
            var es = EventSystem.current;
            if (es == null) {
                Debug.LogError("[CRITICAL] EventSystem.current is NULL in Update!");
                return;
            }

            PointerEventData eventData = new PointerEventData(es);
            eventData.position = UnityEngine.InputSystem.Pointer.current.position.ReadValue();
            List<RaycastResult> results = new List<RaycastResult>();
            es.RaycastAll(eventData, results);

            Debug.Log($"[INPUT-DEBUG] Click at {UnityEngine.InputSystem.Pointer.current.position.ReadValue()}. Hits: {results.Count}");
            for (int i = 0; i < results.Count; i++) {
                Debug.Log($"  - [{i}] {results[i].gameObject.name} (Layer: {LayerMask.LayerToName(results[i].gameObject.layer)})");
            }
        }
    }
}
