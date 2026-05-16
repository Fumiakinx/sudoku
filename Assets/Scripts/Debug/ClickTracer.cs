using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class ClickTracer : MonoBehaviour
{
    void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 pos = Mouse.current.position.ReadValue();
            Debug.Log($"<color=yellow>[ClickTracer] Click detected at Screen Pos: {pos}</color>");

            if (EventSystem.current == null) {
                Debug.LogError("[ClickTracer] CRITICAL: EventSystem.current is NULL!");
                return;
            }

            // 1. システム状態の深掘りログ
            var module = EventSystem.current.currentInputModule;
            Debug.Log($"[ClickTracer] EventSystem State: currentInputModule = {(module != null ? module.GetType().Name : "NULL")}, IsPointerOverGameObject = {EventSystem.current.IsPointerOverGameObject()}");
            
            if (Mouse.current != null) {
                Debug.Log($"[ClickTracer] InputSystem Mouse: active = {Mouse.current.enabled}");
            }

            // 2. レイキャスターの状態確認
            var raycasters = FindObjectsByType<GraphicRaycaster>(FindObjectsInactive.Include);
            Debug.Log($"[ClickTracer] Found {raycasters.Length} GraphicRaycasters in scene.");
            foreach (var gr in raycasters) {
                var grCanvas = gr.GetComponent<Canvas>();
                Debug.Log($"[ClickTracer] Raycaster: {gr.gameObject.name} - Enabled: {gr.enabled} - Blocking: {gr.blockingObjects} - Mask: {gr.blockingMask.value}");
                Debug.Log($"[ClickTracer] Canvas: {grCanvas?.gameObject.name} - RenderMode: {grCanvas?.renderMode} - Display: {grCanvas?.targetDisplay}");
            }

            // 3. レイキャスト実行
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = pos;

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            Debug.Log($"[ClickTracer] RaycastAll Results Count: {results.Count}");
            
            if (results.Count == 0) {
                Debug.LogWarning("[ClickTracer] NO HITS detected. Checking hierarchy and physical coordinates.");
                
                // 4. セルの「物理的な画面位置」を逆算してログ
                GameObject targetCell = GameObject.Find("Cell_0_0");
                if (targetCell != null) {
                    RectTransform rt = targetCell.GetComponent<RectTransform>();
                    Canvas targetCanvas = targetCell.GetComponentInParent<Canvas>();
                    Vector2 cellScreenPos = RectTransformUtility.WorldToScreenPoint(targetCanvas != null ? targetCanvas.worldCamera : null, rt.position);
                    
                    Graphic img = targetCell.GetComponent<Graphic>();
                    Debug.Log($"[ClickTracer] PHYSICAL POSITION (Cell_0_0): ScreenPos: {cellScreenPos}, ClickPos: {pos}, Canvas: {(targetCanvas != null ? targetCanvas.name : "NONE")}");
                    Debug.Log($"[ClickTracer] CELL DATA: RaycastTarget: {img?.raycastTarget}, Layer: {targetCell.layer}, ParentActive: {(targetCell.transform.parent != null ? targetCell.transform.parent.gameObject.activeInHierarchy : true)}");
                    
                    Button btn = targetCell.GetComponent<Button>();
                    Debug.Log($"[ClickTracer] CELL CHECK (Cell_0_0): Size: {rt.rect.width}x{rt.rect.height}, AnchoredPos: {rt.anchoredPosition}, ButtonEnabled: {btn?.enabled}");
                }

                // 5. 階層の CanvasGroup 状態を確認
                string[] parents = { "GamePanel", "BoardPanel", "SafeArea", "GameUI" };
                foreach (string pName in parents) {
                    GameObject go = GameObject.Find(pName);
                    if (go != null) {
                        CanvasGroup cg = go.GetComponent<CanvasGroup>();
                        Graphic gImg = go.GetComponent<Graphic>();
                        RectTransform rtParent = go.GetComponent<RectTransform>();
                        RectMask2D mask = go.GetComponent<RectMask2D>();
                        
                        string cgInfo = (cg != null) ? $"Blocks: {cg.blocksRaycasts}, Interactable: {cg.interactable}, Alpha: {cg.alpha}, Enabled: {cg.enabled}" : "No CG";
                        string rtInfo = (rtParent != null) ? $"Size: {rtParent.rect.width}x{rtParent.rect.height}, Scale: {rtParent.localScale}" : "No RT";
                        string maskInfo = (mask != null) ? $"Mask: {mask.enabled}" : "No Mask";
                        
                        Debug.Log($"[ClickTracer] HIERARCHY CHECK ({pName}): {rtInfo}, {cgInfo}, {maskInfo}, Image-Target: {gImg?.raycastTarget}");
                    }
                }

                // 6. 追加のシステム設定確認
                foreach (var gr in raycasters) {
                    Debug.Log($"[ClickTracer] Raycaster Detail: {gr.gameObject.name} - ignoreReversed: {gr.ignoreReversedGraphics}, blocking: {gr.blockingObjects}, eventCamera: {(gr.eventCamera != null ? gr.eventCamera.name : "NULL")}");
                }

                if (targetCell != null) {
                    RectTransform rt = targetCell.GetComponent<RectTransform>();
                    Canvas targetCanvas = targetCell.GetComponentInParent<Canvas>();
                    bool contains = RectTransformUtility.RectangleContainsScreenPoint(rt, pos, targetCanvas.worldCamera);
                    Graphic g = targetCell.GetComponent<Graphic>();
                    Debug.Log($"[ClickTracer] MANUAL TEST (Cell_0_0): RectangleContainsScreenPoint = {contains}, CanvasRenderer.cull = {g?.canvasRenderer.cull}");
                }

                // 7. 全セルの配置状況を一括調査
                var allCells = FindObjectsByType<SudokuCell>(FindObjectsInactive.Include);
                Debug.Log($"[ClickTracer] BATCH POSITION CHECK: Found {allCells.Length} cells.");
                foreach (var c in allCells) {
                    RectTransform rtCell = c.GetComponent<RectTransform>();
                    if (rtCell != null) {
                        // ログが膨大になるのを防ぐため、特徴的な数点または異常値のみ、あるいはサマリーを出力
                        if (c.Row == 0 || c.Row == 8 || (c.Row == 4 && c.Col == 4)) {
                            Debug.Log($"[ClickTracer] Cell({c.Row},{c.Col}) - AnchoredPos: {rtCell.anchoredPosition}, ScreenPos: {RectTransformUtility.WorldToScreenPoint(null, rtCell.position)}");
                        }
                    }
                }

                // 8. 全セルの強制修正テスト（レイヤー5、ボタン有効化）
                if (allCells.Length > 0) {
                    Debug.Log($"[ClickTracer] TEST FIX: Forcing Layer 5 and Enabling Button on {allCells.Length} cells.");
                    foreach (var c in allCells) {
                        c.gameObject.layer = 5; // UI Layer
                        var b = c.GetComponent<Button>();
                        if (b != null) b.enabled = true;
                    }
                }

                // 7. Canvasの設定を最終確認
                GameObject guiObj = GameObject.Find("GameUI");
                if (guiObj != null) {
                    Canvas guiCanvas = guiObj.GetComponent<Canvas>();
                    if (guiCanvas != null) {
                        Debug.Log($"[ClickTracer] CANVAS CHECK (GameUI): RenderMode: {guiCanvas.renderMode}, SortingOrder: {guiCanvas.sortingOrder}, PlaneDistance: {guiCanvas.planeDistance}");
                    }
                }
            }

            foreach (var hit in results)
            {
                var graphic = hit.gameObject.GetComponent<Graphic>();
                Debug.Log($"[ClickTracer] HIT: {hit.gameObject.name} (Layer: {LayerMask.LayerToName(hit.gameObject.layer)}) - RaycastTarget: {graphic?.raycastTarget} - Order: {hit.sortingOrder}");
            }
        }
    }
}
