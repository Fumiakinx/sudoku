using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// 数独プロジェクトのテーマ素材（テクスチャシート）を視覚的にスライスし、
/// 自動でSudokuDataに新規テーマとして追加・更新するためのエディタウィンドウツールです。
/// </summary>
public class SudokuThemeCreatorWindow : EditorWindow
{
    [MenuItem("Sudoku/Theme Creator Tool")]
    public static void ShowWindow() {
        GetWindow<SudokuThemeCreatorWindow>("Theme Creator");
    }

    public enum DigitLayoutType {
        ZeroToNine = 0, // 0, 1, 2, ... 9, Blank (Nixie等)
        OneToZero = 1   // 1, 2, 3, ... 9, 0, Blank (Metal, Flipflap等)
    }

    private Texture2D sourceTexture;
    private SudokuData sudokuData;
    private string themeName = "NewTheme";
    private SudokuData.ThemeDisplayType displayType = SudokuData.ThemeDisplayType.Normal;
    private DigitLayoutType layoutType = DigitLayoutType.ZeroToNine;

    // グリッドスライス設定
    private int rows = 3;
    private int cols = 5;
    private float cellWidth = 110f;
    private float cellHeight = 110f;
    private float startX = 0f;
    private float startY = 0f;
    private float paddingX = 0f;
    private float paddingY = 0f;
    
    // スライスの拡大縮小（余白を削って数字を大きく表示するためのスケール係数）
    private float scaleFactor = 1.0f;

    // 各セルの個別微調整用オフセット (最大15マスに対応)
    private Vector2[] individualOffsets = new Vector2[15];

    // 各マスの自動アライメント試行回数を記憶（押すたびに候補を切り替えるため）
    private int[] individualAlignAttempts = new int[15];

    // ドラッグ＆ドロップ移動機能用変数
    private int draggingIndex = -1;
    private Vector2 dragStartMousePos;
    private Vector2 dragStartOffset;
    private int hoveredIndex = -1;

    private Vector2 scrollPos;

    // UI折りたたみ制御フラグ
    private bool showColorSettings = true;
    private bool showIndividualOffsets = false;

    // カスタムカラー設定変数
    private Color customBgColor = Color.black;
    private Color customTextColor = Color.white;
    private Color customPanelColor = Color.black;
    private Color customCellNormalColor = new Color(0.15f, 0.15f, 0.15f, 1.0f);
    private Color customCellFixedColor = new Color(0.1f, 0.1f, 0.1f, 1.0f);
    private Color customBezelBaseColor = new Color(0.3f, 0.3f, 0.3f, 1.0f);
    private Color customHighlightColor = Color.white;
    private Color customShadowColor = Color.black;
    private Color customLineColor = Color.gray;

    // 同期チェック用キャッシュ
    private string lastLoadedThemeName = "";
    private SudokuData lastLoadedSudokuData = null;

    /// <summary>
    /// アセットの選択変更やテーマ名の変更を監視し、既存の配色があれば自動でロードします。
    /// </summary>
    private void CheckAndLoadThemeColors() {
        if (sudokuData != lastLoadedSudokuData || themeName != lastLoadedThemeName) {
            lastLoadedSudokuData = sudokuData;
            lastLoadedThemeName = themeName;
            LoadExistingThemeColors();

            // METALテーマの場合は、自動で cols = 4、レイアウト形式 = OneToZero に切り替える親切設計
            if (!string.IsNullOrEmpty(themeName) && themeName.ToUpper().Contains("METAL")) {
                cols = 4;
                rows = 3;
                layoutType = DigitLayoutType.OneToZero;
            } else {
                // デフォルトは 5列、ZeroToNine
                cols = 5;
                rows = 3;
                layoutType = DigitLayoutType.ZeroToNine;
            }
        }
    }

    /// <summary>
    /// 既存テーマのカラー設定をアセットからロードします。
    /// </summary>
    private void LoadExistingThemeColors() {
        if (sudokuData == null || string.IsNullOrEmpty(themeName)) return;
        for (int i = 0; i < sudokuData.themes.Length; i++) {
            if (sudokuData.themes[i].themeName == themeName) {
                var t = sudokuData.themes[i];
                customBgColor = t.backgroundColor;
                customTextColor = t.textColor;
                customPanelColor = t.panelColor;
                customCellNormalColor = t.cellColorNormal;
                customCellFixedColor = t.cellColorFixed;
                customHighlightColor = t.highlightColor;
                customShadowColor = t.shadowColor;
                customLineColor = t.lineColor;
                
                // ベゼルの基本色は、明るい色と暗い色の中間として近似計算します
                customBezelBaseColor = Color.Lerp(t.highlightColor, t.shadowColor, 0.5f);
                
                // アルファが0の場合はデフォルトにする
                if (customHighlightColor.a == 0 && customShadowColor.a == 0) {
                    UpdateBevelColors(new Color(0.3f, 0.3f, 0.3f, 1.0f));
                }
                return;
            }
        }
        // 既存のテーマが見つからない場合はデフォルトで初期化
        ResetToDefaultColors();
    }

    /// <summary>
    /// 配色を規定のダークモード設定値に初期化します。
    /// </summary>
    private void ResetToDefaultColors() {
        customBgColor = Color.black;
        customTextColor = Color.white;
        customPanelColor = Color.black;
        customCellNormalColor = new Color(0.15f, 0.15f, 0.15f, 1.0f);
        customCellFixedColor = new Color(0.1f, 0.1f, 0.1f, 1.0f);
        customLineColor = Color.gray;
        UpdateBevelColors(new Color(0.3f, 0.3f, 0.3f, 1.0f));
    }

    /// <summary>
    /// ベゼルの基本色を元に、立体感を出すための「明るい色（ハイライト）」と「暗い色（シャドウ）」を自動算出します。
    /// </summary>
    private void UpdateBevelColors(Color baseColor) {
        customBezelBaseColor = baseColor;
        
        float h, s, v;
        Color.RGBToHSV(baseColor, out h, out s, out v);
        
        // 明るい色（ハイライト）: 輝度を30%上げる
        customHighlightColor = Color.HSVToRGB(h, s, Mathf.Clamp01(v + 0.3f));
        // 暗い色（シャドウ）: 輝度を30%下げる
        customShadowColor = Color.HSVToRGB(h, s, Mathf.Clamp01(v - 0.3f));
    }

    private void OnGUI() {
        // テーマ名の入力などによる自動ロードのチェック
        CheckAndLoadThemeColors();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        GUILayout.Label("数独テーマクリエイター & スライサー (アドバンスド)", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 左右分割レイアウトの開始
        EditorGUILayout.BeginHorizontal();

        // ================= 左カラム: 設定パネル =================
        EditorGUILayout.BeginVertical(GUILayout.Width(380));

        GUILayout.Label("📁 基本アセット設定", EditorStyles.boldLabel);
        sourceTexture = (Texture2D)EditorGUILayout.ObjectField("元画像テクスチャ", sourceTexture, typeof(Texture2D), false);
        sudokuData = (SudokuData)EditorGUILayout.ObjectField("Sudoku Data アセット", sudokuData, typeof(SudokuData), false);
        themeName = EditorGUILayout.TextField("登録テーマ名", themeName);
        displayType = (SudokuData.ThemeDisplayType)EditorGUILayout.EnumPopup("表示ロジック形式", displayType);
        layoutType = (DigitLayoutType)EditorGUILayout.EnumPopup("数字配列パターン", layoutType);

        EditorGUILayout.Space();
        GUILayout.Label("📐 グリッドスライス設定 (全体)", EditorStyles.boldLabel);
        rows = EditorGUILayout.IntField("  行数 (Rows)", rows);
        cols = EditorGUILayout.IntField("  列数 (Cols)", cols);
        cellWidth = EditorGUILayout.FloatField("  セル幅 (Cell Width)", cellWidth);
        cellHeight = EditorGUILayout.FloatField("  セル高さ (Cell Height)", cellHeight);
        startX = EditorGUILayout.FloatField("  左開始 (Start X)", startX);
        startY = EditorGUILayout.FloatField("  上開始 (Start Y)", startY);
        paddingX = EditorGUILayout.FloatField("  横間隔 (Padding X)", paddingX);
        paddingY = EditorGUILayout.FloatField("  縦間隔 (Padding Y)", paddingY);
        scaleFactor = EditorGUILayout.Slider("  数字の大きさ", scaleFactor, 0.3f, 3.0f);

        // 3. テーマカラー＆デザイン設定 (Foldout)
        EditorGUILayout.Space();
        showColorSettings = EditorGUILayout.Foldout(showColorSettings, "🎨 テーマカラー設定 (配色調整)", true);
        if (showColorSettings) {
            EditorGUI.indentLevel++;
            
            // 同期・リセットボタン
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🔄 アセットから再読込", GUILayout.Height(22))) {
                LoadExistingThemeColors();
            }
            if (GUILayout.Button("🎨 配色をリセット", GUILayout.Height(22))) {
                ResetToDefaultColors();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            customBgColor = EditorGUILayout.ColorField("背景色 (Background)", customBgColor);
            customTextColor = EditorGUILayout.ColorField("文字色 (Text Color)", customTextColor);
            customPanelColor = EditorGUILayout.ColorField("盤面背景色 (Panel Bg)", customPanelColor);
            customCellNormalColor = EditorGUILayout.ColorField("通常マス色 (Normal Cell)", customCellNormalColor);
            customCellFixedColor = EditorGUILayout.ColorField("初期マス色 (Fixed Cell)", customCellFixedColor);
            customLineColor = EditorGUILayout.ColorField("グリッド枠線色 (Line Color)", customLineColor);

            EditorGUILayout.Space();

            // ベベルの基本色と明るい色・暗い色
            EditorGUI.BeginChangeCheck();
            Color newBezelBase = EditorGUILayout.ColorField("ベベル基本色 (Bevel Base)", customBezelBaseColor);
            if (EditorGUI.EndChangeCheck()) {
                UpdateBevelColors(newBezelBase);
            }
            
            // 明るい色と暗い色（表示＆手動微調整）
            customHighlightColor = EditorGUILayout.ColorField(" ├ 明るい色 (Highlight)", customHighlightColor);
            customShadowColor = EditorGUILayout.ColorField(" └ 暗い色 (Shadow)", customShadowColor);

            EditorGUI.indentLevel--;
        }

        // 4. 個別セルの位置微調整 (Foldout)
        EditorGUILayout.Space();
        showIndividualOffsets = EditorGUILayout.Foldout(showIndividualOffsets, "🎯 個別セルの位置微調整 (数字ズレ補正)", true);
        if (showIndividualOffsets) {
            EditorGUI.indentLevel++;
            EditorGUILayout.HelpBox("特定の数字だけ位置がズレている場合、ここで上下左右にピクセル単位の微調整が可能です。", MessageType.Info);
            
            if (GUILayout.Button("✨ すべての数字を自動で中心に合わせる (Auto Align)", GUILayout.Height(30))) {
                AutoAlignCellsToCharacterCenter();
            }
            EditorGUILayout.Space();

            int totalCells = rows * cols;
            for (int i = 0; i < totalCells && i < individualOffsets.Length; i++) {
                if (i >= 11) continue;

                string labelName;
                if (i == 10) {
                    labelName = "空 (ブランク)";
                } else if (layoutType == DigitLayoutType.OneToZero) {
                    labelName = (i == 9) ? "数字 「0」" : $"数字 「{i + 1}」";
                } else {
                    labelName = $"数字 「{i}」";
                }

                EditorGUILayout.BeginHorizontal();
                individualOffsets[i] = EditorGUILayout.Vector2Field(labelName, individualOffsets[i]);
                if (GUILayout.Button("🎯 自動位置", GUILayout.Width(70))) {
                    AlignSingleCell(i);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
        }

        // 5. 実行ボタン
        EditorGUILayout.Space(20);
        if (GUILayout.Button("🚀 スライス実行 ＆ テーマを新規登録/更新", GUILayout.Height(45))) {
            ExecuteSliceAndAddTheme();
        }

        EditorGUILayout.EndVertical();

        // 縦方向の間仕切り線
        GUILayout.Space(15);
        GUILayout.Box("", GUILayout.Width(2), GUILayout.ExpandHeight(true));
        GUILayout.Space(15);

        // ================= 右カラム: プレビューパネル =================
        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));

        if (sourceTexture != null) {
            GUILayout.Label("👀 スライスプレビュー (赤枠が切り取り範囲です)", EditorStyles.boldLabel);
            
            // 描画矩形を確保
            Rect previewRect = GUILayoutUtility.GetRect(300, 320, GUILayout.ExpandWidth(true));
            DrawTexturePreview(previewRect);
            
            // リアルタイム簡易カラープレビュー
            DrawColorThemePreview();
        } else {
            EditorGUILayout.HelpBox("元画像テクスチャを割り当てると、ここにリアルタイムプレビューが表示されます。", MessageType.Info);
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// 設定されたテーマカラーを視覚的にわかりやすく簡易プレビュー描画します。
    /// </summary>
    private void DrawColorThemePreview() {
        EditorGUILayout.Space(15);
        GUILayout.Label("🎨 配色プレビュー (簡易イメージ)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("ゲーム中のマスの立体感や色の重なり具合をその場で確認できます。", MessageType.Info);
        
        // プレビュー用の矩形領域を確保 (高さ110)
        Rect rect = GUILayoutUtility.GetRect(200, 120, GUILayout.ExpandWidth(true));
        
        // 1. 背景色の描画 (backgroundColor)
        EditorGUI.DrawRect(rect, customBgColor);
        
        // 2. パネル部分の描画 (panelColor)
        float panelW = rect.width * 0.94f;
        float panelH = rect.height * 0.85f;
        Rect panelRect = new Rect(rect.x + (rect.width - panelW) / 2f, rect.y + (rect.height - panelH) / 2f, panelW, panelH);
        EditorGUI.DrawRect(panelRect, customPanelColor);
        
        // 3. サンプルマスのサイズ算出
        float cellSize = Mathf.Min(panelH * 0.75f, panelW * 0.28f);
        float cellY = panelRect.y + (panelRect.height - cellSize) / 2f;
        float spacing = (panelRect.width - cellSize * 3) / 4f;
        
        // ベゼル描画用の太さ
        float th = 3.5f;

        // --- サンプルマス1: 通常マス (cellColorNormal + 立体ベゼル + 通常文字) ---
        Rect cell1 = new Rect(panelRect.x + spacing, cellY, cellSize, cellSize);
        EditorGUI.DrawRect(cell1, customCellNormalColor);
        
        // ベゼル枠 (明るいハイライト色 と 暗いシャドウ色)
        EditorGUI.DrawRect(new Rect(cell1.x, cell1.y, cell1.width, th), customHighlightColor); // 上
        EditorGUI.DrawRect(new Rect(cell1.x, cell1.y, th, cell1.height), customHighlightColor); // 左
        EditorGUI.DrawRect(new Rect(cell1.x, cell1.yMax - th, cell1.width, th), customShadowColor); // 下
        EditorGUI.DrawRect(new Rect(cell1.xMax - th, cell1.y, th, cell1.height), customShadowColor); // 右
        
        // ダミー文字 (textColor)
        GUIStyle cellTextStyle = new GUIStyle();
        cellTextStyle.normal.textColor = customTextColor;
        cellTextStyle.alignment = TextAnchor.MiddleCenter;
        cellTextStyle.fontSize = Mathf.RoundToInt(cellSize * 0.55f);
        cellTextStyle.fontStyle = FontStyle.Bold;
        GUI.Label(cell1, "5", cellTextStyle);
        
        // --- サンプルマス2: 空白（ブランク）マス (originalSpriteBgColor + 立体ベゼル) ---
        Rect cell2 = new Rect(cell1.xMax + spacing, cellY, cellSize, cellSize);
        EditorGUI.DrawRect(cell2, customBezelBaseColor); // オリーブ色などのブランク色
        
        // ベゼル枠
        EditorGUI.DrawRect(new Rect(cell2.x, cell2.y, cell2.width, th), customHighlightColor);
        EditorGUI.DrawRect(new Rect(cell2.x, cell2.y, th, cell2.height), customHighlightColor);
        EditorGUI.DrawRect(new Rect(cell2.x, cell2.yMax - th, cell2.width, th), customShadowColor);
        EditorGUI.DrawRect(new Rect(cell2.xMax - th, cell2.y, th, cell2.height), customShadowColor);

        // --- サンプルマス3: 初期マス (cellColorFixed + 立体ベゼル + 固定文字) ---
        Rect cell3 = new Rect(cell2.xMax + spacing, cellY, cellSize, cellSize);
        EditorGUI.DrawRect(cell3, customCellFixedColor);
        
        // ベゼル枠
        EditorGUI.DrawRect(new Rect(cell3.x, cell3.y, cell3.width, th), customHighlightColor);
        EditorGUI.DrawRect(new Rect(cell3.x, cell3.y, th, cell3.height), customHighlightColor);
        EditorGUI.DrawRect(new Rect(cell3.x, cell3.yMax - th, cell3.width, th), customShadowColor);
        EditorGUI.DrawRect(new Rect(cell3.xMax - th, cell3.y, th, cell3.height), customShadowColor);
        
        // 固定マスのダミー文字 (青みがかったダミー色で表現)
        GUIStyle fixedTextStyle = new GUIStyle(cellTextStyle);
        fixedTextStyle.normal.textColor = Color.Lerp(customTextColor, Color.cyan, 0.45f);
        GUI.Label(cell3, "9", fixedTextStyle);
    }

    /// <summary>
    /// エディタウィンドウ上にテクスチャとスライス赤枠をプレビュー描画します。
    /// </summary>
    private void DrawTexturePreview(Rect rect) {
        if (sourceTexture == null) return;

        float texWidth = sourceTexture.width;
        float texHeight = sourceTexture.height;
        float aspect = texHeight / texWidth;

        float drawWidth = rect.width;
        float drawHeight = drawWidth * aspect;
        if (drawHeight > rect.height) {
            drawHeight = rect.height;
            drawWidth = drawHeight / aspect;
        }

        // 中央配置のためのRect計算
        Rect drawRect = new Rect(rect.x + (rect.width - drawWidth) / 2, rect.y + (rect.height - drawHeight) / 2, drawWidth, drawHeight);

        // テクスチャそのものを背景に描画
        GUI.DrawTexture(drawRect, sourceTexture, ScaleMode.ScaleToFit);

        // 各マスのスライス枠と操作
        Handles.BeginGUI();
        float scaleX = drawWidth / texWidth;
        float scaleY = drawHeight / texHeight;

        Event e = Event.current;
        Vector2 mousePos = e.mousePosition;

        // ドラッグ中の全体的な移動処理
        if (draggingIndex != -1) {
            if (e.type == EventType.MouseDrag) {
                Vector2 mouseDelta = mousePos - dragStartMousePos;
                float deltaX = mouseDelta.x / scaleX;
                float deltaY = mouseDelta.y / scaleY;

                individualOffsets[draggingIndex] = new Vector2(
                    Mathf.Round(dragStartOffset.x + deltaX),
                    Mathf.Round(dragStartOffset.y + deltaY)
                );

                Repaint();
                e.Use();
            }
            else if (e.type == EventType.MouseUp || e.type == EventType.Ignore) {
                draggingIndex = -1;
                Repaint();
            }
        }

        for (int r = 0; r < rows; r++) {
            for (int c = 0; c < cols; c++) {
                int index = r * cols + c;
                if (index >= 11) continue;

                // 基準位置の計算
                float x = startX + c * (cellWidth + paddingX);
                float y = startY + r * (cellHeight + paddingY);

                // 個別オフセットの適用
                if (index < individualOffsets.Length) {
                    x += individualOffsets[index].x;
                    y += individualOffsets[index].y;
                }

                // スケール変更（中心を維持した拡縮）
                float currentWidth = cellWidth * scaleFactor;
                float currentHeight = cellHeight * scaleFactor;

                // 空白（ブランク）の場合は10x10の小さな枠としてプレビュー描画する
                if (index == 10) {
                    currentWidth = 10f;
                    currentHeight = 10f;
                }

                float diffW = (cellWidth - currentWidth) / 2f;
                float diffH = (cellHeight - currentHeight) / 2f;
                x += diffW;
                y += diffH;

                // 描画座標への変換
                Rect cellRect = new Rect(
                    drawRect.x + x * scaleX,
                    drawRect.y + y * scaleY,
                    currentWidth * scaleX,
                    currentHeight * scaleY
                );

                // ホバーおよびクリック判定
                bool isMouseOver = cellRect.Contains(mousePos);

                if (isMouseOver && draggingIndex == -1) {
                    hoveredIndex = index;
                } else if (hoveredIndex == index && !isMouseOver) {
                    hoveredIndex = -1;
                }

                if (e.type == EventType.MouseDown && isMouseOver && e.button == 0) {
                    draggingIndex = index;
                    dragStartMousePos = mousePos;
                    dragStartOffset = individualOffsets[index];
                    e.Use();
                }

                // 枠の表示色の決定 (プレミアムな視覚フィードバック)
                Color outlineColor = Color.red;
                float outlineThickness = 1f;

                if (draggingIndex == index) {
                    outlineColor = new Color(0.2f, 0.9f, 0.3f, 1f); // 鮮やかな黄緑色
                    outlineThickness = 2.5f;
                } else if (hoveredIndex == index) {
                    outlineColor = Color.yellow; // ホバー時は黄色
                    outlineThickness = 2f;
                }

                // 枠の描画
                if (outlineThickness > 1f) {
                    Vector3[] corners = new Vector3[] {
                        new Vector3(cellRect.x, cellRect.y, 0),
                        new Vector3(cellRect.xMax, cellRect.y, 0),
                        new Vector3(cellRect.xMax, cellRect.yMax, 0),
                        new Vector3(cellRect.x, cellRect.yMax, 0),
                        new Vector3(cellRect.x, cellRect.y, 0)
                    };
                    Handles.color = outlineColor;
                    Handles.DrawAAPolyLine(outlineThickness * 2f, corners);
                } else {
                    Handles.DrawSolidRectangleWithOutline(cellRect, new Color(0, 0, 0, 0), outlineColor);
                }
                
                // インデックス番号を右上に描画
                GUIStyle style = new GUIStyle();
                style.normal.textColor = (hoveredIndex == index || draggingIndex == index) ? Color.cyan : Color.yellow;
                style.fontStyle = FontStyle.Bold;

                string labelStr;
                if (index == 10) {
                    labelStr = "B"; // Blank
                } else if (layoutType == DigitLayoutType.OneToZero) {
                    labelStr = (index == 9) ? "0" : (index + 1).ToString();
                } else {
                    labelStr = index.ToString();
                }

                Handles.Label(new Vector3(cellRect.x + 4, cellRect.y + 4, 0), labelStr, style);
            }
        }

        Handles.EndGUI();
    }

    /// <summary>
    /// 実際にテクスチャをMultipleモードでスライス設定し、SudokuDataに登録/更新を行います。
    /// </summary>
    private void ExecuteSliceAndAddTheme(bool showDialog = true) {
        if (sourceTexture == null || sudokuData == null) {
            if (showDialog) {
                EditorUtility.DisplayDialog("エラー", "元画像テクスチャとSudokuDataアセットを正しく割り当ててください。", "OK");
            }
            return;
        }

        string assetPath = AssetDatabase.GetAssetPath(sourceTexture);
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return;

        // スキャンで検出したオリーブ色背景を保持する一時変数
        Color detectedBlankBgColor = Color.white;

        // 元画像テクスチャを確実にReadableに設定し、最新参照をリロード
        PrepareTextureForRead();

        // 1. テクスチャインポーターをMultipleかつSpriteに設定
        bool needsReimport = false;
        if (importer.textureType != TextureImporterType.Sprite) {
            importer.textureType = TextureImporterType.Sprite;
            needsReimport = true;
        }
        if (importer.spriteImportMode != SpriteImportMode.Multiple) {
            importer.spriteImportMode = SpriteImportMode.Multiple;
            needsReimport = true;
        }

        if (needsReimport) {
            importer.SaveAndReimport();
            AssetDatabase.ImportAsset(assetPath);
            sourceTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        List<SpriteMetaData> metaDataList = new List<SpriteMetaData>();
        int texHeight = sourceTexture.height;

        for (int r = 0; r < rows; r++) {
            for (int c = 0; c < cols; c++) {
                int index = r * cols + c;

                // 切り取るのは11個（0〜10）だけ
                if (index >= 11) continue;

                float x = startX + c * (cellWidth + paddingX);
                float yTop = startY + r * (cellHeight + paddingY);

                // 個別微調整
                if (index < individualOffsets.Length) {
                    x += individualOffsets[index].x;
                    yTop += individualOffsets[index].y;
                }

                // スケール拡縮を適用
                float currentWidth = cellWidth * scaleFactor;
                float currentHeight = cellHeight * scaleFactor;
                float diffW = (cellWidth - currentWidth) / 2f;
                float diffH = (cellHeight - currentHeight) / 2f;
                x += diffW;
                yTop += diffH;

                // Unityのテクスチャ座標は「左下」が原点であるため、Y軸を反転させる
                float yBottom = texHeight - (yTop + currentHeight);

                // 🌟 インデックス 10（ブランク）の時、中心の10x10の領域から平均色を取得し、110x110の単色PNGを自動生成する
                if (index == 10) {
                    float centerX = x + currentWidth / 2f;
                    float centerYBottom = yBottom + currentHeight / 2f;

                    int startPxVal = Mathf.RoundToInt(centerX - 5f);
                    int startPyVal = Mathf.RoundToInt(centerYBottom - 5f);

                    // 10x10の領域のピクセル平均色を算出
                    float rSum = 0, gSum = 0, bSum = 0, aSum = 0;
                    int pCount = 0;

                    for (int py = startPyVal; py < startPyVal + 10; py++) {
                        for (int px = startPxVal; px < startPxVal + 10; px++) {
                            int cx = Mathf.Clamp(px, 0, sourceTexture.width - 1);
                            int cy = Mathf.Clamp(py, 0, sourceTexture.height - 1);
                            Color cColor = sourceTexture.GetPixel(cx, cy);
                            rSum += cColor.r;
                            gSum += cColor.g;
                            bSum += cColor.b;
                            aSum += cColor.a;
                            pCount++;
                        }
                    }

                    Color bgColor = (pCount > 0) 
                        ? new Color(rSum / pCount, gSum / pCount, bSum / pCount, aSum / pCount)
                        : Color.black;

                    // 検出したオリーブ色を一時保存
                    detectedBlankBgColor = bgColor;

                    // 110x110の単色アセットを自動生成（検出した背景色で塗りつぶす）
                    string folderPath = Path.GetDirectoryName(assetPath);
                    string blankAssetPath = Path.Combine(folderPath, $"{sourceTexture.name}_Blank.png").Replace("\\", "/");

                    Texture2D blankTex = new Texture2D(110, 110);
                    Color[] fill = new Color[110 * 110];
                    for (int f = 0; f < fill.Length; f++) fill[f] = detectedBlankBgColor;
                    blankTex.SetPixels(fill);
                    blankTex.Apply();

                    byte[] pngBytes = blankTex.EncodeToPNG();
                    File.WriteAllBytes(blankAssetPath, pngBytes);
                    DestroyImmediate(blankTex);

                    AssetDatabase.ImportAsset(blankAssetPath);

                    // Spriteとしてインポート設定
                    TextureImporter blankImporter = AssetImporter.GetAtPath(blankAssetPath) as TextureImporter;
                    if (blankImporter != null) {
                        blankImporter.textureType = TextureImporterType.Sprite;
                        blankImporter.spriteImportMode = SpriteImportMode.Single;
                        blankImporter.isReadable = true;
                        blankImporter.SaveAndReimport();
                    }

                    // 10番目のスプライトは別ファイルにしたので、メイン画像のスライス一覧からは除外します
                    continue;
                }

                SpriteMetaData meta = new SpriteMetaData();

                // layoutType に応じたスプライト末尾サフィックスの決定
                string suffix = index.ToString();
                if (layoutType == DigitLayoutType.OneToZero) {
                    if (index == 9) {
                        suffix = "0";
                    } else {
                        suffix = (index + 1).ToString();
                    }
                }

                meta.name = $"{themeName}_{suffix}";
                meta.rect = new Rect(x, yBottom, currentWidth, currentHeight);
                meta.alignment = (int)SpriteAlignment.Center;
                meta.pivot = new Vector2(0.5f, 0.5f);

                metaDataList.Add(meta);
            }
        }

        // スライス情報をテクスチャに書き込み
        importer.spritesheet = metaDataList.ToArray();
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();

        // 2. スライスされたSpriteアセットを取得・ソート
        Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        List<Sprite> slicedSprites = new List<Sprite>();
        foreach (var sub in subAssets) {
            if (sub is Sprite sprite) {
                // 万一 _Blank が元のテクスチャにも含まれていたら除外（ないはずですが安全のため）
                if (sprite.name.EndsWith("_Blank", System.StringComparison.OrdinalIgnoreCase)) continue;
                slicedSprites.Add(sprite);
            }
        }

        // インデックス順に名前の末尾（_0, _1...）で正しくソート
        slicedSprites.Sort((a, b) => {
            int idxA = GetIndexFromName(a.name);
            int idxB = GetIndexFromName(b.name);
            return idxA.CompareTo(idxB);
        });

        // 🌟 自動生成された完全単一色のブランクスプライトを10番目に差し込む！
        string folder = Path.GetDirectoryName(assetPath);
        string generatedBlankPath = Path.Combine(folder, $"{sourceTexture.name}_Blank.png").Replace("\\", "/");
        Sprite blankSprite = AssetDatabase.LoadAssetAtPath<Sprite>(generatedBlankPath);
        if (blankSprite != null) {
            slicedSprites.Add(blankSprite);
        }

        // 3. SudokuData 内のテーマ情報の追加または上書き
        int existingIndex = -1;
        for (int i = 0; i < sudokuData.themes.Length; i++) {
            if (sudokuData.themes[i].themeName == themeName) {
                existingIndex = i;
                break;
            }
        }

        SudokuData.SudokuTheme targetTheme = (existingIndex != -1) 
            ? sudokuData.themes[existingIndex] 
            : new SudokuData.SudokuTheme();

        targetTheme.themeName = themeName;
        targetTheme.displayType = displayType;
        targetTheme.sprites = slicedSprites.ToArray();
        targetTheme.allSprites = slicedSprites.ToArray();

        // 常に最新スキャンで検出された背景色とフラグをセット
        targetTheme.originalSpriteBgColor = detectedBlankBgColor;
        targetTheme.useOriginalSpriteColor = true;

        // ツールで調整された配色パラメータをテーマに適用
        targetTheme.backgroundColor = customBgColor;
        targetTheme.textColor = customTextColor;
        targetTheme.panelColor = customPanelColor;
        targetTheme.cellColorNormal = customCellNormalColor;
        targetTheme.cellColorFixed = customCellFixedColor;
        targetTheme.lineColor = customLineColor;
        targetTheme.highlightColor = customHighlightColor;
        targetTheme.shadowColor = customShadowColor;

        // 新規登録時のベゼル等の規定値設定
        if (existingIndex == -1) {
            targetTheme.showBezel = true;
            targetTheme.bevelWidth = 4f;
            targetTheme.correctMarkColor = Color.green;
            targetTheme.errorMarkColor = Color.red;
        }

        // データアセットの配列更新
        List<SudokuData.SudokuTheme> themeList = new List<SudokuData.SudokuTheme>(sudokuData.themes);
        if (existingIndex != -1) {
            themeList[existingIndex] = targetTheme;
        } else {
            themeList.Add(targetTheme);
        }
        sudokuData.themes = themeList.ToArray();

        EditorUtility.SetDirty(sudokuData);
        AssetDatabase.SaveAssets();

        if (showDialog) {
            EditorUtility.DisplayDialog("成功", $"テーマ「{themeName}」のスライスおよびSudokuDataへの登録が完了しました！", "OK");
        }
    }

    /// <summary>
    /// アセット名からスライス番号インデックスを抽出します。
    /// </summary>
    private int GetIndexFromName(string name) {
        int lastUnder = name.LastIndexOf('_');
        if (lastUnder != -1 && int.TryParse(name.Substring(lastUnder + 1), out int idx)) {
            return idx;
        }
        return 0;
    }

    /// <summary>
    /// 各セルの画像ピクセルを解析し、数字の描画領域の中心に 110x110 のスライス枠を自動で合わせます。
    /// </summary>
    private void AutoAlignCellsToCharacterCenter(bool showDialog = true) {
        if (sourceTexture == null) {
            if (showDialog) {
                EditorUtility.DisplayDialog("エラー", "元画像テクスチャを割り当ててください。", "OK");
            }
            return;
        }

        // アライメント試行回数の履歴をリセット
        System.Array.Clear(individualAlignAttempts, 0, individualAlignAttempts.Length);

        // 元画像テクスチャを確実にReadableに設定し、最新参照をリロード
        if (!PrepareTextureForRead()) return;

        int texHeight = sourceTexture.height;
        int totalCells = rows * cols;

        for (int i = 0; i < totalCells && i < individualOffsets.Length; i++) {
            int r = i / cols;
            int c = i % cols;

            // 文字をスキャンして探すための「広い等分割エリア」（テクスチャ下原点）
            float scanW = (float)sourceTexture.width / cols;
            float scanH = (float)sourceTexture.height / rows;
            float scanX = c * scanW;
            float scanYTop = r * scanH;
            float scanYBottom = texHeight - (scanYTop + scanH);

            int startPx = Mathf.RoundToInt(scanX);
            int startPy = Mathf.RoundToInt(scanYBottom);
            int w = Mathf.RoundToInt(scanW);
            int h = Mathf.RoundToInt(scanH);

            // 境界外アクセスを防ぐ安全ガード
            if (startPx < 0 || startPy < 0 || startPx + w > sourceTexture.width || startPy + h > sourceTexture.height) {
                individualOffsets[i] = Vector2.zero;
                continue;
            }

            // セル内のピクセルを取得
            Color[] pixels = sourceTexture.GetPixels(startPx, startPy, w, h);

            // 有意なピクセル（文字部分）の境界ボックスを検出
            int minX = w, maxX = 0;
            int minY = h, maxY = 0;
            bool foundCharacter = false;

            // セル内の輝度の最大値と最小値を求めて動的にしきい値を決める
            float maxL = 0f;
            float minL = 1f;
            foreach (var pixel in pixels) {
                float l = pixel.grayscale;
                if (l > maxL) maxL = l;
                if (l < minL) minL = l;
            }

            // コントラストが極端に低い場合は、文字がない（空白セル）とみなす
            if (maxL - minL > 0.1f) {
                float threshold = minL + (maxL - minL) * 0.4f; // 下から40%の輝度をしきい値にする

                for (int y = 0; y < h; y++) {
                    for (int x = 0; x < w; x++) {
                        Color p = pixels[y * w + x];
                        if (p.grayscale > threshold) {
                            if (x < minX) minX = x;
                            if (x > maxX) maxX = x;
                            if (y < minY) minY = y;
                            if (y > maxY) maxY = y;
                            foundCharacter = true;
                        }
                    }
                }
            }

            if (foundCharacter) {
                // スキャン領域内で検出した「文字の正確な中心座標」
                float charCenterX = scanX + (minX + maxX) / 2f;
                float charCenterY = scanYBottom + (minY + maxY) / 2f;

                // デフォルトの 110x110 セルの中心座標（開始位置などを加味）
                float defaultX = startX + c * (cellWidth + paddingX);
                float defaultYTop = startY + r * (cellHeight + paddingY);
                float defaultYBottom = texHeight - (defaultYTop + cellHeight);

                float defaultCenterX = defaultX + cellWidth / 2f;
                float defaultCenterY = defaultYBottom + cellHeight / 2f;

                // ズレをオフセットとして設定 (110x110の枠が文字の中心を囲むように配置されます)
                float offsetX = charCenterX - defaultCenterX;
                float offsetY = defaultCenterY - charCenterY;

                individualOffsets[i] = new Vector2(Mathf.Round(offsetX), Mathf.Round(offsetY));
            } else {
                // 文字が検出されなかった（空枠）の場合はオフセットをゼロにする
                individualOffsets[i] = Vector2.zero;
            }
        }

        // 同一段（同一行）の縦座標（Yオフセット）を平均値に揃えてガタツキを補正します
        for (int r = 0; r < rows; r++) {
            float ySum = 0f;
            int count = 0;

            // まずその行に属する文字検出済みセルのYオフセットを集計します
            for (int c = 0; c < cols; c++) {
                int index = r * cols + c;
                if (index >= 11 || index == 10) continue; // スライスは0〜9（数字）のみを対象にします

                // 個別オフセットが Vector2.zero ではない（文字が検出され調整された）セルを対象にします
                if (individualOffsets[index] != Vector2.zero) {
                    ySum += individualOffsets[index].y;
                    count++;
                }
            }

            // 調整対象のセルが複数ある場合、その平均値を算出して同一行の全対象セルに適用します
            if (count > 0) {
                float yAverage = Mathf.Round(ySum / count);
                for (int c = 0; c < cols; c++) {
                    int index = r * cols + c;
                    if (index >= 11 || index == 10) continue;

                    if (individualOffsets[index] != Vector2.zero) {
                        individualOffsets[index].y = yAverage;
                    }
                }
            }
        }

        if (showDialog) {
            EditorUtility.DisplayDialog("アライメント完了", "文字の中心を軸にした110x110のスライス枠を自動調整しました！（同一段の縦座標も揃えました）", "OK");
        }
    }

    /// <summary>
    /// 現在のオフセット座標（ユーザーが手動で近づけた位置）の周辺をスキャンし、
    /// 最も近い数字の中心に110x110の枠を吸着させます。連打でしきい値を切り替えて次の候補に補正します。
    /// </summary>
    private void AlignSingleCell(int i) {
        if (sourceTexture == null) return;

        // 元画像テクスチャを確実にReadableに設定し、最新参照をリロード
        if (!PrepareTextureForRead()) return;

        int texHeight = sourceTexture.height;
        int r = i / cols;
        int c = i % cols;

        // 1. デフォルト位置（オフセットなし）の計算
        float defaultX = startX + c * (cellWidth + paddingX);
        float defaultYTop = startY + r * (cellHeight + paddingY);
        float defaultYBottom = texHeight - (defaultYTop + cellHeight);

        float defaultCenterX = defaultX + cellWidth / 2f;
        float defaultCenterY = defaultYBottom + cellHeight / 2f;

        // 2. ユーザーが現在手動で動かした「現在の赤枠の中心座標」
        float currentCenterX = defaultCenterX + individualOffsets[i].x;
        float currentCenterY = defaultCenterY - individualOffsets[i].y; // Y軸は上下反転

        // 3. 現在の枠の周辺（110x110より少し広い135x135エリア）を検索窓に設定
        int scanSize = 135;
        int startPx = Mathf.Max(0, Mathf.RoundToInt(currentCenterX - scanSize / 2f));
        int startPy = Mathf.Max(0, Mathf.RoundToInt(currentCenterY - scanSize / 2f));
        int w = Mathf.Min(scanSize, sourceTexture.width - startPx);
        int h = Mathf.Min(scanSize, sourceTexture.height - startPy);

        if (w > 0 && h > 0) {
            Color[] pixels = sourceTexture.GetPixels(startPx, startPy, w, h);
            int minX = w, maxX = 0;
            int minY = h, maxY = 0;
            bool foundCharacter = false;

            float maxL = 0f;
            float minL = 1f;
            foreach (var pixel in pixels) {
                float l = pixel.grayscale;
                if (l > maxL) maxL = l;
                if (l < minL) minL = l;
            }

            if (maxL - minL > 0.1f) {
                // 連打で感度を切り替え（5パターン）
                int attempt = individualAlignAttempts[i] % 5;
                float thresholdFactor = 0.40f;
                switch (attempt) {
                    case 0: thresholdFactor = 0.40f; break; // 標準
                    case 1: thresholdFactor = 0.25f; break; // 広め（薄い部分も拾う）
                    case 2: thresholdFactor = 0.55f; break; // タイト（濃い部分だけ）
                    case 3: thresholdFactor = 0.15f; break; // 超広め
                    case 4: thresholdFactor = 0.70f; break; // 超タイト
                }

                float threshold = minL + (maxL - minL) * thresholdFactor;

                for (int y = 0; y < h; y++) {
                    for (int x = 0; x < w; x++) {
                        Color p = pixels[y * w + x];
                        if (p.grayscale > threshold) {
                            if (x < minX) minX = x;
                            if (x > maxX) maxX = x;
                            if (y < minY) minY = y;
                            if (y > maxY) maxY = y;
                            foundCharacter = true;
                        }
                    }
                }

                // 次回のためにカウントアップ
                individualAlignAttempts[i]++;
            }

            if (foundCharacter) {
                // 検索窓内での文字の中心座標（テクスチャ座標系）
                float charCenterX = startPx + (minX + maxX) / 2f;
                float charCenterY = startPy + (minY + maxY) / 2f;

                // デフォルト中心からのズレを新しいオフセットとして計算
                float offsetX = charCenterX - defaultCenterX;
                float offsetY = defaultCenterY - charCenterY;

                float targetOffsetY = Mathf.Round(offsetY);
                float otherYSum = 0f;
                int otherYCount = 0;

                // 同一の段（同じ行）の他の調整済みセルのYオフセットの平均値を調べます
                for (int col = 0; col < cols; col++) {
                    int index = r * cols + col;
                    if (index >= 11 || index == 10 || index == i) continue; // 自身・範囲外・ブランクは除外

                    if (individualOffsets[index] != Vector2.zero) {
                        otherYSum += individualOffsets[index].y;
                        otherYCount++;
                    }
                }

                // 同一の段で他のセルがすでに調整されている場合、差が±20px以内であればその平均値にスナップ（吸着）させます
                if (otherYCount > 0) {
                    float otherYAverage = Mathf.Round(otherYSum / otherYCount);
                    if (Mathf.Abs(targetOffsetY - otherYAverage) <= 20f) {
                        targetOffsetY = otherYAverage;
                    }
                }

                individualOffsets[i] = new Vector2(Mathf.Round(offsetX), targetOffsetY);
            }
            // 文字が見つからなかった場合は、ユーザーが手動で合わせた現在の位置をそのままキープします（勝手にリセットしません）
        }

        Repaint();
    }

    /// <summary>
    /// 元画像テクスチャを確実にReadable（読み書き可能）に設定し、参照を最新のものにリロードします。
    /// </summary>
    private bool PrepareTextureForRead() {
        if (sourceTexture == null) return false;

        string assetPath = AssetDatabase.GetAssetPath(sourceTexture);
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return false;

        // インポーターの設定、またはメモリ上のテクスチャが非Readableな場合に強制インポートし直す
        bool needsReimport = !importer.isReadable || !sourceTexture.isReadable;

        if (needsReimport) {
            importer.isReadable = true;
            importer.SaveAndReimport();
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            
            // 同期インポートを走らせるためにアセットデータベースをリフレッシュ
            AssetDatabase.Refresh();
        }

        // 最新のReadable化されたテクスチャインスタンスをロードし直して同期
        sourceTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        return true;
    }
}
