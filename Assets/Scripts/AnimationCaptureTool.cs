using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class AnimationCaptureTool : MonoBehaviour
{
    [Header("Settings")]
    public int captureFrames = 24;
    public float captureInterval = 0.05f;

    public static AnimationCaptureTool Instance;

    private bool isCapturing = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    [ContextMenu("Capture Sequence")]
    public void TriggerCapture()
    {
        // 動作テスト用（一番最初のセルを無理やり回す場合）
        if (Application.isPlaying)
        {
            FlipFlapDisplay targetDigit = FindAnyObjectByType<FlipFlapDisplay>();
            if (targetDigit != null)
            {
                int current = targetDigit.TargetValue;
                int next = current == 9 ? 1 : current + 1;
                targetDigit.SetValue(next, false);
                // キャプチャは FlipFlapDisplay 側から自動で呼ばれるようになります
            }
        }
    }

    public void StartCaptureForCell(RectTransform targetCell)
    {
        if (!isCapturing && gameObject.activeInHierarchy)
        {
            StartCoroutine(CaptureAnimationSequence(targetCell));
        }
    }

    private IEnumerator CaptureAnimationSequence(RectTransform targetCell)
    {
        isCapturing = true;
        Debug.Log("キャプチャを開始します...");

        List<Texture2D> frames = new List<Texture2D>();

        // レンダリングが安定するまで1フレーム待つ
        yield return new WaitForEndOfFrame();

        // キャプチャループ
        for (int i = 0; i < captureFrames; i++)
        {
            yield return new WaitForEndOfFrame(); // レンダリング完了を待つ

            // 画面全体をキャプチャ
            Texture2D screenTex = ScreenCapture.CaptureScreenshotAsTexture();

            // 対象セルのスクリーン上の矩形座標を計算
            Vector3[] corners = new Vector3[4];
            targetCell.GetWorldCorners(corners);

            Camera cam = targetCell.GetComponentInParent<Canvas>().worldCamera;
            if (cam == null) cam = Camera.main;

            Vector2 min = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);

            int x = Mathf.FloorToInt(min.x);
            int y = Mathf.FloorToInt(min.y);
            int width = Mathf.CeilToInt(max.x - min.x);
            int height = Mathf.CeilToInt(max.y - min.y);

            // 画面外にはみ出さないようクランプ
            x = Mathf.Clamp(x, 0, screenTex.width);
            y = Mathf.Clamp(y, 0, screenTex.height);
            width = Mathf.Clamp(width, 0, screenTex.width - x);
            height = Mathf.Clamp(height, 0, screenTex.height - y);

            if (width > 0 && height > 0)
            {
                // セルの部分だけを切り抜いたテクスチャを作成
                Texture2D cellTex = new Texture2D(width, height, TextureFormat.RGB24, false);
                cellTex.SetPixels(screenTex.GetPixels(x, y, width, height));
                cellTex.Apply();
                frames.Add(cellTex);
            }

            Destroy(screenTex);

            // 次のコマまで待機
            yield return new WaitForSeconds(captureInterval);
        }

        // キャプチャしたフレームを横に連結する
        if (frames.Count > 0)
        {
            int totalWidth = 0;
            int h = frames[0].height;
            foreach (var tex in frames) totalWidth += tex.width;

            Texture2D finalTex = new Texture2D(totalWidth, h, TextureFormat.RGB24, false);
            int currentX = 0;
            foreach (var tex in frames)
            {
                finalTex.SetPixels(currentX, 0, tex.width, h, tex.GetPixels());
                currentX += tex.width;
                Destroy(tex);
            }
            finalTex.Apply();

            // PNGとして保存
            byte[] bytes = finalTex.EncodeToPNG();
            string dirPath = Path.Combine(Application.dataPath, "Screenshots");
            Directory.CreateDirectory(dirPath);
            string path = Path.Combine(dirPath, "FlipSequence_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png");
            File.WriteAllBytes(path, bytes);

            Debug.Log("アニメーションの時系列画像を保存しました: " + path);
            Destroy(finalTex);
        }

        isCapturing = false;
    }
}
