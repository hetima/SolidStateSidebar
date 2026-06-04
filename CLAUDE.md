# SolidStateSidebar — CLAUDE.md

## プロジェクト概要

Windows 10/11 向けのデスクトップサイドバーアプリ。CPU・RAM・GPU・ネットワーク・ドライブ・時計・アプリウィンドウをリアルタイム監視して表示する。SidebarDiagnostics の後継。

- **言語/FW:** C# / .NET 10 / WPF
- **バージョン:** 0.9.3 (開発中)
- **ライセンス:** GPLv3

## ビルド

```
dotnet build SolidStateSidebar.sln
dotnet build -c Release
```

実行ファイルは `bin/Debug(Release)/net10.0-windows/` に生成。インストーラなし・ポータブル。

## アーキテクチャ

### 全体構造

```
src/
  Core/          # モニタリング基盤・設定・共通インフラ
  Modules/       # ハードウェア別モジュール（各フォルダ独立）
  Views/         # WPF ビュー（XAML + コードビハインド）
  Models/        # ViewModel 相当のモデル
  Converters/    # WPF バインディング用コンバーター
  Utilities/     # アイコン取得・ウィンドウ操作ユーティリティ
  WPFDevelopers/ # サードパーティ WPF コンポーネント（ColorPicker 等）
IconTheme/       # アイコンテーマ定義 JSON
```

### Core 層

| ファイル | 役割 |
|---|---|
| `MonitorBase.cs` | `iMonitor` インターフェース・`BaseMonitor` 基底クラス |
| `MetricBase.cs` | `iMetric` インターフェース（個別センサー値） |
| `OHMMonitorBase.cs` | LibreHardwareMonitor 統合用抽象基底 |
| `MonitorManager.cs` | 全モニターインスタンスを管理するファクトリ |
| `Settings.cs` | シングルトン設定（JSON保存） |
| `MetricConfig.cs` | メトリクスごとの設定（有効/無効、アラート閾値） |
| `HardwareConfig.cs` | ハードウェアアイテムの有効/順序/名前設定 |
| `MonitoringEnums.cs` | `MonitorType`・`MetricKey`・`DataType` 列挙型 |

### モジュール構造（各 Modules/ サブフォルダ）

各モジュールは統一構造を持つ：

```
Modules/XxxMonitor/
  XxxMonitor.cs       # ハードウェア監視ロジック
  Data.cs             # モジュール固有設定モデル（IModuleData 実装）
  Section.xaml(.cs)   # サイドバー上の表示コンポーネント
  SettingPanel.xaml   # 設定ダイアログのタブ
```

モジュール一覧：CPU / RAM / GPU / Network / Drive / Time / Window

### MVVM パターン

- `SidebarModel` がモニターコレクションと更新ループを管理
- ビューは `{Binding}` で Data クラスにバインド
- `INotifyPropertyChanged` で反応的 UI 更新

### 設定の永続化

- 保存先: `%LocalAppData%\SolidStateSidebar\settings.json`
- `Settings` シングルトン（Newtonsoft.Json でシリアライズ）
- 各モジュールの `Data.cs` が `IModuleData` を実装

## 主要依存ライブラリ

| ライブラリ | 用途 |
|---|---|
| `LibreHardwareMonitorLib 0.9.6` | CPU/RAM/GPU センサー取得 |
| `Hardcodet.NotifyIcon.Wpf 2.0.1` | システムトレイアイコン |
| `gong-wpf-dragdrop 4.0.0` | UI のドラッグ&ドロップ |
| `DotNetProjects.SVGImage 5.2.13` | SVG アイコンレンダリング |
| `TaskScheduler 2.12.2` | Windows タスクスケジューラ（自動起動） |
| `Newtonsoft.Json 13.0.4` | 設定 JSON のシリアライズ |

## 新モジュール追加の手順

1. `Modules/XxxMonitor/` フォルダを作成
2. `BaseMonitor` または `OHMMonitorBase` を継承した `XxxMonitor.cs` を実装
3. `IModuleData` を実装した `Data.cs` を作成
4. `Section.xaml` / `SettingPanel.xaml` を作成
5. `MonitoringEnums.cs` に `MonitorType` 値を追加
6. `MonitorManager.cs` にファクトリ登録
7. `MonitorPanelTemplateSelector` / `MonitorConfigTemplateSelector` にテンプレート追加

## 注意事項

- **プラットフォーム:** Windows 専用（Win32 P/Invoke 使用、`net10.0-windows`）
- **実行権限:** 一部センサー取得に管理者権限が必要な場合あり（app.manifest 参照）
- **名前空間:** ルートは `SSS`（`SolidStateSidebar` の略）
- **国際化:** 11言語対応（`src/Resources/Strings.*.resx`）
- **アイコンテーマ:** `%LocalAppData%\SolidStateSidebar\IconThemes\` または埋め込み JSON
