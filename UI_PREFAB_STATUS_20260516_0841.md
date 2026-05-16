# Sudoku UI プレハブ利用・状況レポート (2026-05-16 更新)

| プレハブ名 | 状態 | 役割 / 注意点 |
| :--- | :--- | :--- |
| `GameUI_Stable.prefab` | **正常 (ネスト済)** | ゲーム画面のメインUI。セルもネスト済み。 |
| `MenuUI_Stable.prefab` | **正常** | メインメニューUI。 |
| `ResultUI_Stable.prefab` | **正常** | リザルト画面のメインUI。新規作成し、シーンへ適用済み。 |
| `SudokuCell.prefab` | **正常 (マスター)** | セルのマスタープレハブ。 |

## 完了した修正 (2026/05/16)
1. **SudokuCell の再ネスト**: `GameUI_Stable` 内の81個のセルを `SudokuCell.prefab` のインスタンスとして再リンクしました。これにより、マスタープレハブの変更が全セルに反映されるようになりました。
2. **イベント自動修復 (ULTRA REPAIR)**: `SudokuPermanentFix.UltraRepairAllUIEvents()` を実行し、すべてのセルボタンと入力ボタンの `onClick` イベントを静的に紐付けました。
3. **レガシー資産の隔離**: 旧 `GameUI`, `MenuUI`, `SudokuUI` 等を `Assets/Legacy_OldUI/` へ移動し、開発中の誤用を防止しました。
4. **安全なバックアップ**: 修正前の `GameUI_Stable` を `Assets/Legacy_OldUI/GameUI_Stable_Backup_BeforeRefactoring.prefab` として保存済みです。

## 技術的詳細
*   **静的構成の遵守**: 本プロジェクトでは、実行時のUI生成を避け、すべてInspector上での静的な紐付けを基本としています。
*   **修復ツール**: `SudokuPermanentFix.cs` を使用することで、シーン遷移やドメインリロードによるイベント消失をいつでも一括修復可能です。
