# 設計メモ

---

## DataTemplateSelector 導入によるモジュール別カスタムレイアウト (2026-05-17)

### 概要
MonitorPanel に MonitorType を付与し、DataTemplateSelector でモジュールごとに異なる XAML レイアウトを適用できるようにした。

### 変更ファイル
- `src/Core/MonitorPanel.cs` — `MonitorType Type` プロパティ追加、コンストラクタ引数に `type` を先頭に追加
- `src/Core/MonitorManager.cs` — OHMPanel/DrivePanel/NetworkPanel/TimePanel の4箇所で `new MonitorPanel(type, ...)` に修正
- `src/Converters/MonitorPanelTemplateSelector.cs` — **新規作成**。`DataTemplateSelector` で Type==Time → TimeTemplate、それ以外 → DefaultTemplate
- `src/Converters/Converters.cs` — `FontSizeAddConverter` 追加（ベースフォント + ConverterParameter 加算）
- `src/App.xaml` — `FontSizeAddConverter` をグローバルリソースに登録
- `src/Views/Sidebar.xaml` — `ItemTemplate` → `ItemTemplateSelector` に変更。`DefaultMonitorPanelTemplate`（既存）と `TimeMonitorPanelTemplate`（日付フォント +2）をリソース定義（これはうまく行ってない）

### 設計上のポイント
- `MonitorPanel.Type` はシリアライズ対象外（実行時プロパティ）。シリアライズは `MonitorConfig.Type` が担当
- Time 用テンプレートは BaseMonitor 用 DataType テンプレート（DriveMonitor 等含む）を含まないシンプル構成
- 将来的に新モジュール専用レイアウト追加時は: MonitorPanelTemplateSelector に新プロパティ追加 + XAML DataTemplate 追加のみ
- 設定パネルとサイドバーセクションを機能ごとに個別のビューにできるようにするのが最終目標
