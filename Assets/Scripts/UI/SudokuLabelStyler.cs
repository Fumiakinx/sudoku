using UnityEngine;
using TMPro;

/// <summary>
/// UI内の各種テキストラベルのスタイル（色など）をテーマに合わせて適用します。
/// 文字列の内容やサイズを名前で強制的に上書きするレガシーなロジックを廃止しました。
/// </summary>
public class SudokuLabelStyler : MonoBehaviour
{
    /// <summary>
    /// テーマに基づいてラベルの見た目を更新します。
    /// </summary>
    public static void ApplyStyle(TextMeshProUGUI label, SudokuData.SudokuTheme theme) {
        if (label == null || string.IsNullOrEmpty(theme.themeName)) return;
        
        // 基本の色適用
        label.color = theme.textColor;
        


        // label.enabled = true; // 静的な設定を尊重するため、強制的な有効化を廃止

        // 注意：かつてここで "TitleText" を "SUDOKU" に書き換える処理がありましたが、
        // インスペクターの設定を優先するため廃止しました。
        // サイズ設定も基本はPrefab/Sceneの設定に従いますが、必要があればここに追加します。
    }
}
