using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

# if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
# endif

/// <summary>
/// 診断用スクリプト：クリックされたUIオブジェクトをすべてコンソールに出力します。
/// </summary>
public class SudokuClickTracer : MonoBehaviour {
    void Update() {
        bool pressed = false;
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) pressed = true;
#else
        if (Input.GetMouseButtonDown(0)) pressed = true;
#endif

        if (pressed) {
            PointerEventData eventData = new PointerEventData(EventSystem.current);
#if ENABLE_INPUT_SYSTEM
            eventData.position = Mouse.current.position.ReadValue();
#else
            eventData.position = Input.mousePosition;
#endif
            List<RaycastResult> results = new List<RaycastResult>();
            if (EventSystem.current != null) {
                EventSystem.current.RaycastAll(eventData, results);

                string msg = $"[Click Tracer] MousePosition: {eventData.position}\n";
                if (results.Count == 0) {
                    msg += " - No UI Object Hit.";
                } else {
                    for (int i = 0; i < results.Count; i++) {
                        msg += $" - Hit[{i}]: {results[i].gameObject.name} (ID: {results[i].gameObject.GetEntityId()})\n";
                    }
                }
                Debug.Log(msg);
            }
        }
    }
}
