using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// クリック信号がどこで止まっているかをコンソールに実況するデバッグ用スクリプト。
/// </summary>
public class SudokuInteractionDebugger : MonoBehaviour
{
    void Update()
    {
        // マウスの左クリックを検知
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Debug.Log($"[CLICK-LOG] ========================================");
            Debug.Log($"[CLICK-LOG] Mouse Click at: {mousePos}");

            // 現在の EventSystem の状態を確認
            if (EventSystem.current == null)
            {
                Debug.LogError("[CLICK-LOG] ERROR: EventSystem.current が NULL です！司令塔が不在です。");
                return;
            }

            // UIレイキャストをシミュレート
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = mousePos;

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            if (results.Count > 0)
            {
                Debug.Log($"[CLICK-LOG] ヒット件数: {results.Count} 件");
                foreach (var result in results)
                {
                    var graphic = result.gameObject.GetComponent<UnityEngine.UI.Graphic>();
                    Debug.Log($"[CLICK-LOG] ヒット対象: {result.gameObject.name} | Layer: {LayerMask.LayerToName(result.gameObject.layer)} | RaycastTarget: {(graphic != null ? graphic.raycastTarget.ToString() : "N/A")}");
                }
            }
            else
            {
                Debug.LogWarning("[CLICK-LOG] 警告: 何のUIオブジェクトにも当たりませんでした。");
            }
        }
    }
}
