using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sudokuの盤面オブジェクト（セルやブロック）の生成とレイアウトのみを担当するクラス
/// </summary>
[ExecuteAlways]
public class SudokuGridGenerator : MonoBehaviour {
    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private Transform gridParent;

    private bool isGenerating = false;

    public void Generate(SudokuBoard board, int[,] puzzle) {
        if (isGenerating) return;
        isGenerating = true;

        try {
            ClearOldBoard();
            
            if (gridParent == null) return;
            
            // 既存のGridLayoutGroupを削除（手動レイアウトのため）
            var rootGrid = gridParent.GetComponent<GridLayoutGroup>();
            if (rootGrid != null) DestroyImmediate(rootGrid);

            RectTransform containerRT = gridParent.GetComponent<RectTransform>();
            containerRT.anchorMin = containerRT.anchorMax = containerRT.pivot = new Vector2(0.5f, 0.5f);
            containerRT.anchoredPosition = Vector2.zero;

            float availableSize = Mathf.Min(containerRT.rect.width, containerRT.rect.height);
            if (availableSize <= 0) availableSize = 1000f; 

            // レイアウト定数
            float innerRatio = 0.02f;   
            float blockRatio = 0.25f;   
            float totalRatio = 9f + (6f * innerRatio) + (2f * blockRatio);
            
            float cellSize = availableSize / totalRatio;
            float innerSpacing = cellSize * innerRatio;
            float blockSpacing = cellSize * blockRatio;
            float blockSize = (cellSize * 3) + (innerSpacing * 2); 
            float totalBoardSize = (blockSize * 3) + (blockSpacing * 2);
            float startOffset = -totalBoardSize / 2f + blockSize / 2f;

            for (int bR = 0; bR < 3; bR++) {
                for (int bC = 0; bC < 3; bC++) {
                    int blockIndex = bR * 3 + bC;
                    GameObject blockGo = new GameObject($"Block_{blockIndex}", typeof(RectTransform));
                    blockGo.transform.SetParent(gridParent, false);
                    
                    var rt = blockGo.GetComponent<RectTransform>();
                    rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(blockSize, blockSize);
                    
                    float posX = startOffset + bC * (blockSize + blockSpacing);
                    float posY = -(startOffset + bR * (blockSize + blockSpacing));
                    rt.anchoredPosition = new Vector2(posX, posY); 

                    float[] localCellCoords = { -cellSize - innerSpacing, 0, cellSize + innerSpacing };

                    for (int r = 0; r < 3; r++) {
                        for (int c = 0; c < 3; c++) {
                            int globalR = bR * 3 + r;
                            int globalC = bC * 3 + c;

                            GameObject cellObj = Instantiate(cellPrefab, blockGo.transform);
                            cellObj.name = $"Cell_{globalR}_{globalC}";
                            
                            var cellRT = cellObj.GetComponent<RectTransform>();
                            cellRT.anchorMin = cellRT.anchorMax = cellRT.pivot = new Vector2(0.5f, 0.5f);
                            cellRT.sizeDelta = new Vector2(cellSize, cellSize);
                            cellRT.anchoredPosition = new Vector2(localCellCoords[c], -localCellCoords[r]);

                            SudokuCell cell = cellObj.GetComponent<SudokuCell>();
                            cell.Init(globalR, globalC, puzzle[globalR, globalC], puzzle[globalR, globalC] != 0, board);
                            board.cells[globalR, globalC] = cell;

                            var btn = cellObj.GetComponent<Button>() ?? cellObj.AddComponent<Button>();
                            btn.onClick.AddListener(() => board.SelectCell(cell));
                            btn.transition = Selectable.Transition.None;
                        }
                    }
                }
            }
        } finally {
            isGenerating = false;
        }
        
        if (SudokuUIStyler.Instance != null) SudokuUIStyler.Instance.ApplyTheme(true);
    }

    private void ClearOldBoard() {
        if (gridParent == null) return;
        
        // 1. gridParent直下の子要素を確実に削除（逆順ループ）
        for (int i = gridParent.childCount - 1; i >= 0; i--) {
            Transform child = gridParent.GetChild(i);
            // 親を切り離してから削除することで、childCountに即座に反映させる
            child.SetParent(null);
            if (Application.isPlaying) Destroy(child.gameObject);
            else DestroyImmediate(child.gameObject);
        }
        
        // 2. 盤面外に浮遊してしまった「Cell_」や「Block_」を徹底的に掃除
        var allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        foreach (var go in allObjects) {
            if (go == null) continue;
            if (go.name.StartsWith("Cell_") || go.name.StartsWith("Block_")) {
                // 正しく生成された現在のセルの親（blockGoなど）はgridParentの下にあるはず
                // 親がnull、またはルートにあるものは不要な残骸
                if (go.transform.parent == null || go.transform.root == go.transform) {
                    if (Application.isPlaying) Destroy(go);
                    else DestroyImmediate(go);
                }
            }
        }
    }
}
