using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class SudokuWebGLBuilder
{
    [MenuItem("Sudoku/Build WebGL (Portrait)")]
    public static void BuildWebGLAndProcess()
    {
        string buildDirectory = "WebGLBuilds/Sudoku";
        string zipPath = "WebGLBuilds/connectwebgl.zip";

        Debug.Log("WebGLビルドプロセスを開始します...");

        // 1. シーンの確認
        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            Debug.LogError("ビルド対象のシーンがEditor Build Settingsに登録されていません。");
            return;
        }

        Debug.Log($"ビルド対象シーン: {string.Join(", ", scenes)}");

        // 2. 出力ディレクトリのクリーンアップ
        if (Directory.Exists(buildDirectory))
        {
            try
            {
                Directory.Delete(buildDirectory, true);
                Debug.Log($"既存のビルドディレクトリをクリーンアップしました: {buildDirectory}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"既存のビルドディレクトリの削除に失敗しました: {ex.Message}");
            }
        }
        Directory.CreateDirectory(buildDirectory);

        // 3. WebGLビルドの実行
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = buildDirectory,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        Debug.Log("Unity BuildPipeline を実行中...");
        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        var summary = report.summary;

        if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"ビルドが正常に完了しました！ 出力先: {buildDirectory} (サイズ: {summary.totalSize} バイト)");

            // 4. 縦画面用CSSハックの適用
            ApplyPortraitCSSHack(buildDirectory);

            // 5. ZIP圧縮の実行
            CreateZipArchive(buildDirectory, zipPath);

            Debug.Log("全てのビルドおよび後処理プロセスが完了しました！");
            
            // エクスプローラーでフォルダを開く
            EditorUtility.RevealInFinder(zipPath);
        }
        else if (summary.result == UnityEditor.Build.Reporting.BuildResult.Failed)
        {
            Debug.LogError($"ビルドが失敗しました。エラー数: {summary.totalErrors}");
        }
    }

    private static void ApplyPortraitCSSHack(string buildDirectory)
    {
        string htmlPath = Path.Combine(buildDirectory, "index.html");
        if (!File.Exists(htmlPath))
        {
            Debug.LogError($"index.html が見つかりません: {htmlPath}");
            return;
        }

        try
        {
            Debug.Log("縦画面アスペクト比（9:16）維持のCSSハックを index.html に適用中...");
            string html = File.ReadAllText(htmlPath);

            // 1. 既存の <body style="..."> タグ内のインラインスタイルを正規表現で除去し、シンプルな <body> にする
            html = Regex.Replace(html, @"<body\s+[^>]*>", "<body>");

            // 2. JS側の動的モバイル判定およびスタイル変更コードブロックを完全に削除（CSSハックと重複して無駄なため）
            string mobileJsPattern = @"if\s*\(/iPhone\|iPad\|iPod\|Android/i\.test\(navigator\.userAgent\)\)\s*\{[\s\S]*?\}";
            html = Regex.Replace(html, mobileJsPattern, "");

            // 3. </head> の直前に、静的な viewport メタタグと縦画面用CSSスタイルを挿入
            string viewportMeta = "\n    <meta name=\"viewport\" content=\"width=device-width, height=device-height, initial-scale=1.0, user-scalable=no, shrink-to-fit=yes\">";
            string cssHack = @"
    <style>
      html, body {
        width: 100% !important;
        height: 100% !important;
        overflow: hidden !important;
        background: #231F20 !important;
        margin: 0 !important;
        padding: 0 !important;
        display: flex !important;
        justify-content: center !important;
        align-items: center !important;
      }
      #unity-canvas {
        width: 100% !important;
        height: 100% !important;
        max-width: 56.25vh !important; /* 9:16 アスペクト比制限: 9/16 = 56.25vh */
        max-height: 177.78vw !important; /* 16:9 アスペクト比制限: 16/9 = 177.78vw */
        aspect-ratio: 9 / 16 !important;
        margin: auto !important;
        display: block !important;
      }
    </style>
";
            html = html.Replace("</head>", viewportMeta + cssHack + "\n  </head>");

            File.WriteAllText(htmlPath, html);
            Debug.Log("index.html の無駄な動的JSロジック排除と静的縦画面CSSハック適用が完了しました。");
        }
        catch (Exception ex)
        {
            Debug.LogError($"index.html のCSS置換中にエラーが発生しました: {ex.Message}");
        }
    }

    private static void CreateZipArchive(string sourceDirectory, string zipPath)
    {
        try
        {
            Debug.Log($"WebGLビルド成果物をZIP圧縮中: {zipPath}");

            // 既存のZIPファイルがあれば削除
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }

            // ZIP出力先フォルダの存在確認と作成
            string zipDir = Path.GetDirectoryName(zipPath);
            if (!string.IsNullOrEmpty(zipDir) && !Directory.Exists(zipDir))
            {
                Directory.CreateDirectory(zipDir);
            }

            // ZIPアーカイブの作成
            ZipFile.CreateFromDirectory(sourceDirectory, zipPath);
            Debug.Log($"ZIPの作成が完了しました！ 保存先: {zipPath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"ZIP圧縮中にエラーが発生しました: {ex.Message}");
        }
    }
}
