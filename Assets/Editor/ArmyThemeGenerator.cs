using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

/// <summary>
/// ミリタリーステンシル調テーマ「ARMY RUST」用の数字テクスチャシートを、
/// 元画像から重心（中心）を検出して完全にセンタリングした状態で自動生成するエディタツールです。
/// </summary>
public class ArmyThemeGenerator : EditorWindow
{
    private const string SourcePath = "SamplePic/army.png";
    private const string DestPath = "Assets/Textures/ArmySheet.png";
    private const string MetaPath = "Assets/Textures/ArmySheet.png.meta";

    [MenuItem("Tools/Generate Army Theme Texture")]
    public static void GenerateTextureMenu()
    {
        GenerateTexture(true);
    }

    public static void GenerateTexture(bool showDialog = false)
    {
        // 1. ソーステクスチャの存在チェック
        if (!File.Exists(SourcePath))
        {
            string errorMsg = $"ソーステクスチャ ({SourcePath}) が見つかりません。";
            if (showDialog) EditorUtility.DisplayDialog("Error", errorMsg, "OK");
            else Debug.LogError(errorMsg);
            return;
        }

        // 直接 File.ReadAllBytes でロードします。
        byte[] fileBytes = File.ReadAllBytes(SourcePath);
        Texture2D srcTex = new Texture2D(2, 2);
        if (!srcTex.LoadImage(fileBytes))
        {
            string errorMsg = "ソース画像のロードに失敗しました。";
            if (showDialog) EditorUtility.DisplayDialog("Error", errorMsg, "OK");
            else Debug.LogError(errorMsg);
            return;
        }

        // 2. 出力テクスチャの設定 (256x256 のマスが11個並ぶ 2816x256)
        int cellW = 256;
        int cellH = 256;
        int numCells = 11; // 0〜9 の数字と 10 (Blank)
        Texture2D destTex = new Texture2D(cellW * numCells, cellH, TextureFormat.RGBA32, false);

        // 背景オリーブドラブ色 (R: 0.294, G: 0.282, B: 0.204)
        Color oliveDrab = new Color(0.294f, 0.282f, 0.204f, 1.0f);

        // 出力テクスチャ全体をオリーブドラブ色で初期化
        Color[] initColors = new Color[destTex.width * destTex.height];
        for (int i = 0; i < initColors.Length; i++) initColors[i] = oliveDrab;
        destTex.SetPixels(initColors);

        // 3. 各マスの切り出しと重心センタリング処理
        // 元画像サイズ: Width: 722, Height: 542
        float srcCellW = 722f / 5f;  // 144.4
        float srcCellH = 542f / 2f;  // 271.0

        for (int i = 0; i < 10; i++)
        {
            int col = i % 5;
            int row = i / 5; // 0 = 上半分, 1 = 下半分

            // 元画像での対象セルのピクセル座標範囲
            int xMin = Mathf.RoundToInt(col * srcCellW);
            int xMax = Mathf.RoundToInt((col + 1) * srcCellW);
            int yMin = Mathf.RoundToInt((1 - row) * srcCellH); // UnityのY座標は下から上
            int yMax = Mathf.RoundToInt((2 - row) * srcCellH);

            // 安全のための境界クランプ
            xMin = Mathf.Clamp(xMin, 0, srcTex.width);
            xMax = Mathf.Clamp(xMax, 0, srcTex.width);
            yMin = Mathf.Clamp(yMin, 0, srcTex.height);
            yMax = Mathf.Clamp(yMax, 0, srcTex.height);

            // 文字領域（オリーブドラブ以外の白に近い領域）のバウンディングボックスを検出
            int charMinX = xMax, charMaxX = xMin;
            int charMinY = yMax, charMaxY = yMin;
            bool foundChar = false;

            for (int y = yMin; y < yMax; y++)
            {
                for (int x = xMin; x < xMax; x++)
                {
                    Color pixel = srcTex.GetPixel(x, y);
                    // R値が0.45以上のものを文字ピクセルと判定 (背景オリーブドラブは 0.294 程度)
                    if (pixel.r > 0.45f)
                    {
                        if (x < charMinX) charMinX = x;
                        if (x > charMaxX) charMaxX = x;
                        if (y < charMinY) charMinY = y;
                        if (y > charMaxY) charMaxY = y;
                        foundChar = true;
                    }
                }
            }

            // 文字の中心（重心）を計算
            float cx, cy;
            if (foundChar)
            {
                cx = (charMinX + charMaxX) / 2f;
                cy = (charMinY + charMaxY) / 2f;
                Debug.Log($"[ARMY-SLICER] 数字 [{i}] 検出完了: 重心 ({cx:F1}, {cy:F1}), 範囲: {charMaxX - charMinX}x{charMaxY - charMinY}");
            }
            else
            {
                cx = (xMin + xMax) / 2f;
                cy = (yMin + yMax) / 2f;
                Debug.LogWarning($"[ARMY-SLICER] 数字 [{i}] の文字領域を検出できませんでした。デフォルト中心を使用します: ({cx}, {cy})");
            }

            // 新しいセルの中央 (128, 128) に文字の重心を合わせてピクセルをコピー
            int destStartX = i * cellW;
            for (int dy = 0; dy < cellH; dy++)
            {
                for (int dx = 0; dx < cellW; dx++)
                {
                    // 中心 (128, 128) からの差分を重心に加算してサンプリング座標を算出
                    int sx = Mathf.RoundToInt(cx + (dx - 128));
                    int sy = Mathf.RoundToInt(cy + (dy - 128));

                    // 元画像のセル境界内にある場合のみピクセルをサンプリング
                    if (sx >= xMin && sx < xMax && sy >= yMin && sy < yMax)
                    {
                        Color sc = srcTex.GetPixel(sx, sy);
                        destTex.SetPixel(destStartX + dx, dy, sc);
                    }
                }
            }
        }

        // 11番目のセル (Blank, インデックス10) はオリーブドラブのままにします。
        destTex.Apply();

        // 4. 新規テクスチャのPNG保存
        byte[] pngBytes = destTex.EncodeToPNG();
        string directory = Path.GetDirectoryName(DestPath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        File.WriteAllBytes(DestPath, pngBytes);
        Debug.Log($"Successfully saved Army texture at: {DestPath}");

        // 5. メタデータ (ArmySheet.png.meta) ファイルの直接手動生成
        // 環境によるメタデータ書き換えの失敗を防ぐため、100%確実に動作する直接書き換えを行います。
        string guid = "cdab743c1231ba9459cf4ae04635fabc";
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("fileFormatVersion: 2");
        sb.AppendLine($"guid: {guid}");
        sb.AppendLine("TextureImporter:");
        sb.AppendLine("  internalIDToNameTable:");
        for (int i = 0; i < numCells; i++)
        {
            string sName = i == 10 ? "Army_Blank" : $"Army_{i}";
            long internalId = 2130000000000000000L + i;
            sb.AppendLine("  - first:");
            sb.AppendLine($"      213: {internalId}");
            sb.AppendLine($"    second: {sName}");
        }
        sb.AppendLine("  externalObjects: {}");
        sb.AppendLine("  serializedVersion: 13");
        sb.AppendLine("  mipmaps:");
        sb.AppendLine("    mipMapMode: 0");
        sb.AppendLine("    enableMipMap: 0");
        sb.AppendLine("    sRGBTexture: 1");
        sb.AppendLine("    linearTexture: 0");
        sb.AppendLine("    fadeOut: 0");
        sb.AppendLine("    borderMipMap: 0");
        sb.AppendLine("    mipMapsPreserveCoverage: 0");
        sb.AppendLine("    alphaTestReferenceValue: 0.5");
        sb.AppendLine("    mipMapFadeDistanceStart: 1");
        sb.AppendLine("    mipMapFadeDistanceEnd: 3");
        sb.AppendLine("  bumpmap:");
        sb.AppendLine("    convertToNormalMap: 0");
        sb.AppendLine("    externalNormalMap: 0");
        sb.AppendLine("    heightScale: 0.25");
        sb.AppendLine("    normalMapFilter: 0");
        sb.AppendLine("    flipGreenChannel: 0");
        sb.AppendLine("  isReadable: 0");
        sb.AppendLine("  streamingMipmaps: 0");
        sb.AppendLine("  streamingMipmapsPriority: 0");
        sb.AppendLine("  vTOnly: 0");
        sb.AppendLine("  ignoreMipmapLimit: 0");
        sb.AppendLine("  grayScaleToAlpha: 0");
        sb.AppendLine("  generateCubemap: 6");
        sb.AppendLine("  cubemapConvolution: 0");
        sb.AppendLine("  seamlessCubemap: 0");
        sb.AppendLine("  textureFormat: 1");
        sb.AppendLine("  maxTextureSize: 2048");
        sb.AppendLine("  textureSettings:");
        sb.AppendLine("    serializedVersion: 2");
        sb.AppendLine("    filterMode: 1");
        sb.AppendLine("    aniso: 1");
        sb.AppendLine("    mipBias: 0");
        sb.AppendLine("    wrapU: 1");
        sb.AppendLine("    wrapV: 1");
        sb.AppendLine("    wrapW: 1");
        sb.AppendLine("  nPOTScale: 0");
        sb.AppendLine("  lightmap: 0");
        sb.AppendLine("  compressionQuality: 50");
        sb.AppendLine("  spriteMode: 2");
        sb.AppendLine("  spriteExtrude: 1");
        sb.AppendLine("  spriteMeshType: 1");
        sb.AppendLine("  alignment: 0");
        sb.AppendLine("  spritePivot: {x: 0.5, y: 0.5}");
        sb.AppendLine("  spritePixelsToUnits: 100");
        sb.AppendLine("  spriteBorder: {x: 0, y: 0, z: 0, w: 0}");
        sb.AppendLine("  spriteGenerateFallbackPhysicsShape: 1");
        sb.AppendLine("  alphaUsage: 1");
        sb.AppendLine("  alphaIsTransparency: 1");
        sb.AppendLine("  spriteTessellationDetail: -1");
        sb.AppendLine("  textureType: 8");
        sb.AppendLine("  textureShape: 1");
        sb.AppendLine("  singleChannelComponent: 0");
        sb.AppendLine("  flipbookRows: 1");
        sb.AppendLine("  flipbookColumns: 1");
        sb.AppendLine("  maxTextureSizeSet: 0");
        sb.AppendLine("  compressionQualitySet: 0");
        sb.AppendLine("  textureFormatSet: 0");
        sb.AppendLine("  ignorePngGamma: 0");
        sb.AppendLine("  applyGammaDecoding: 0");
        sb.AppendLine("  swizzle: 50462976");
        sb.AppendLine("  cookieLightType: 0");
        sb.AppendLine("  platformSettings:");
        sb.AppendLine("  - serializedVersion: 4");
        sb.AppendLine("    buildTarget: DefaultTexturePlatform");
        sb.AppendLine("    maxTextureSize: 2048");
        sb.AppendLine("    resizeAlgorithm: 0");
        sb.AppendLine("    textureFormat: -1");
        sb.AppendLine("    textureCompression: 1");
        sb.AppendLine("    compressionQuality: 50");
        sb.AppendLine("    allowsAlphaSplitting: 0");
        sb.AppendLine("    overridden: 0");
        sb.AppendLine("  spriteSheet:");
        sb.AppendLine("    serializedVersion: 2");
        sb.AppendLine("    sprites:");

        for (int i = 0; i < numCells; i++)
        {
            string sName = i == 10 ? "Army_Blank" : $"Army_{i}";
            long internalId = 2130000000000000000L + i;
            string sId = $"cdab743c1231ba9459cf4ae04635fa{i:x2}00";

            sb.AppendLine("    - serializedVersion: 2");
            sb.AppendLine($"      name: {sName}");
            sb.AppendLine("      rect:");
            sb.AppendLine("        serializedVersion: 2");
            sb.AppendLine($"        x: {i * cellW}");
            sb.AppendLine("        y: 0");
            sb.AppendLine($"        width: {cellW}");
            sb.AppendLine($"        height: {cellH}");
            sb.AppendLine("      alignment: 0");
            sb.AppendLine("      pivot: {x: 0.5, y: 0.5}");
            sb.AppendLine("      border: {x: 0, y: 0, z: 0, w: 0}");
            sb.AppendLine("      customData: ");
            sb.AppendLine("      outline: []");
            sb.AppendLine("      physicsShape: []");
            sb.AppendLine("      tessellationDetail: -1");
            sb.AppendLine("      bones: []");
            sb.AppendLine($"      spriteID: {sId}");
            sb.AppendLine($"      internalID: {internalId}");
            sb.AppendLine("      vertices: []");
            sb.AppendLine("      indices: ");
            sb.AppendLine("      edges: []");
            sb.AppendLine("      weights: []");
        }

        sb.AppendLine("    outline: []");
        sb.AppendLine("    customData: ");
        sb.AppendLine("    physicsShape: []");
        sb.AppendLine("    bones: []");
        sb.AppendLine("    spriteID: ");
        sb.AppendLine("    internalID: 0");
        sb.AppendLine("    vertices: []");
        sb.AppendLine("    indices: ");
        sb.AppendLine("    edges: []");
        sb.AppendLine("    weights: []");
        sb.AppendLine("    secondaryTextures: []");
        sb.AppendLine("    spriteCustomMetadata:");
        sb.AppendLine("      entries: []");
        sb.AppendLine("    nameFileIdTable:");
        for (int i = 0; i < numCells; i++)
        {
            string sName = i == 10 ? "Army_Blank" : $"Army_{i}";
            long internalId = 2130000000000000000L + i;
            sb.AppendLine($"      {sName}: {internalId}");
        }
        sb.AppendLine("  mipmapLimitGroupName: ");
        sb.AppendLine("  pSDRemoveMatte: 0");
        sb.AppendLine("  userData: ");
        sb.AppendLine("  assetBundleName: ");
        sb.AppendLine("  assetBundleVariant: ");

        File.WriteAllText(MetaPath, sb.ToString());
        Debug.Log($"[ARMY-SLICER] Hand-crafted meta file generated and saved at: {MetaPath}");

        // 6. Unity にアセットの再インポートを通知して変更を適用
        AssetDatabase.ImportAsset(DestPath);
        AssetDatabase.Refresh();

        if (showDialog)
        {
            EditorUtility.DisplayDialog("Success", "ARMY RUSTテーマの数字スライス画像シートおよびメタデータを手動生成し、Unityでの11マス分割スプライト設定を完全に適用しました！\n\n保存先: " + DestPath, "OK");
        }
    }
}
