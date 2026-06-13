# SolidStateSidebar — CLAUDE.md

## プロジェクト概要

Windows 10/11 向けのデスクトップサイドバーアプリ。CPU・RAM・GPU・ネットワーク・ドライブ・時計・アプリウィンドウに加え、Claude / Codex の有料プラン使用量をリアルタイム監視して表示する。SidebarDiagnostics の後継。

- **言語/FW:** C# / .NET 10 / WPF
- **バージョン:** 0.9.5 (開発中)
- **ライセンス:** GPLv3
- **配布:** GitHub Releases（ポータブル zip）/ Scoop（`hetima/scoop-bucket`）

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

モジュール一覧：CPU / RAM / GPU / Network / Drive / Time / Window / Claude / Codex

**命名の注意:** ドライブモジュールはフォルダ名が `Modules/DriveMonitor/` だが、名前空間は `SSS.Module.HdMonitor`、設定キーは `"HdMonitor"`、列挙値は `MonitorType.HD`。混在しているので grep 時に注意。

#### 特殊モジュール

- **WindowMonitor:** 指定アプリのウィンドウ一覧を `EnumWindows` で取得・表示。固定スロット方式（`MaxDisplayCount` 個の `WindowItem` を使い回す）。プロセス名/アイコンを 30 分ごとに世代交代キャッシュ。フォアグラウンド変更フック（`UpdateFromHook`）と定期更新を併用。マウスポインタがサイドバー上にある間は一覧更新を停止（`IsPointerOnSidebar`）。`ScrollToSwitch` 有効時はサイドバー上のホイール操作でウィンドウを順送り切替（`TryScrollSwitch`、`Sidebar.xaml.cs` の `PreviewMouseWheel` から `MonitorManager.TryHandleWindowScrollSwitch` 経由）。
- **ClaudeMonitor:** `~/.claude/.credentials.json` の OAuth トークンで `api.anthropic.com/api/oauth/usage` を叩き、5h/1w の使用率とリセット時刻を表示。トークン期限切れ時は自動リフレッシュし credentials.json に書き戻す。
- **CodexMonitor:** `~/.codex/auth.json` の access_token で `chatgpt.com/backend-api/wham/usage` を叩く。トークンリフレッシュは未実装（読み取りのみ）。
- 両者とも `AutoRefreshInterval`（Manual/1min/5min/10min）のタイマー駆動で、ハードウェアセンサー系の更新ループとは独立。表示は `ResetTimeDisplay`（Countdown/Absolute）で切替。セクションアイコンは埋め込み SVG（`claude.svg`/`codex.svg`）を `EmbeddedSvg.Extract` で LocalAppData に展開して使用。

### MVVM パターン

- `SidebarModel` がモニターコレクションと更新ループを管理
- ビューは `{Binding}` で Data クラスにバインド
- `INotifyPropertyChanged` で反応的 UI 更新

### 設定の永続化

- 保存先: `%LocalAppData%\SolidStateSidebar\settings.json`
- `Settings` シングルトン（Newtonsoft.Json でシリアライズ）
- 各モジュールの `Data.cs` が `IModuleData` を実装
- モジュール設定は `ModuleDataConverter.cs` のキー→型マップ（`"CpuMonitor"` 〜 `"CodexMonitor"`）でデシリアライズされる。新モジュール追加時はここにも登録が必要
- セクションヘッダー表示は `SectionHeaderStyle`（Default / Small / None / NoIcon）

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
5. `MonitoringEnums.cs` に `MonitorType` 値・必要なら `MetricKey` 値を追加
6. `MonitorManager.cs` にファクトリ登録
7. `ModuleDataConverter.cs` のキー→型マップと `Default` 辞書に登録
8. `MonitorPanelTemplateSelector` / `MonitorConfigTemplateSelector` にテンプレート追加
9. `SettingsMonitorsView.xaml` に設定タブを追加

参考実装：外部 API 系モジュールは `Modules/ClaudeMonitor/`、ハードウェア系は `Modules/CpuMonitor/` が雛形になる。

## 注意事項

- **プラットフォーム:** Windows 専用（Win32 P/Invoke 使用、`net10.0-windows`）
- **実行権限:** 一部センサー取得に管理者権限が必要な場合あり（app.manifest 参照）。全ハードウェア情報の取得には PawnIO が必要
- **名前空間:** ルートは `SSS`（`SolidStateSidebar` の略）
- **国際化:** 11言語以上対応（`src/Resources/Strings.*.resx`）。文字列追加時は全 resx への反映が必要
- **アイコンテーマ:** `%LocalAppData%\SolidStateSidebar\IconThemes\` または埋め込み JSON
- **git 管理外:** `var/` フォルダは .gitignore 済み（参考用の別プロジェクト等が置かれている）

## 既知の懸念・潜在的なバグ

- **ClaudeMonitor のトークン書き戻し競合:** `~/.claude/.credentials.json` は Claude Code 本体も読み書きするファイル。本アプリがリフレッシュ後に書き戻す際、Claude Code 側の更新と競合する可能性がある。特にリフレッシュ成功後の書き戻しが失敗した場合（catch で無視している）、サーバー側で旧 refresh_token が無効化されていると Claude Code のログインが切れる恐れがある（[ClaudeMonitor.cs:212-223](src/Modules/ClaudeMonitor/ClaudeMonitor.cs#L212-L223)）
- **CodexMonitor はトークンリフレッシュ未実装:** `auth.json` の access_token が期限切れになると Codex CLI で再認証するまで "Error" 表示が続く
- **カウントダウン表示の鮮度:** Claude/Codex の残り時間表示（Countdown）は API 取得時にしか再計算されない。`AutoRefresh = Manual` だと表示が古いまま残る
- **バックグラウンドスレッドからの SetText:** Claude/Codex の `ManualRefresh` は `Task.Run` 内から `Text` を更新し PropertyChanged を発火する。単純なスカラーバインディングなので WPF が自動マーシャリングするが、コレクション変更を伴う拡張をする場合は Dispatcher 経由にすること
- **WindowMonitor のスクロール切替の起点:** `_lastSwitchedHwnd` が一覧に見つからない場合 index 0 起点になるため、最前面ウィンドウと切替起点がずれることがある（[WindowMonitor.cs:126-130](src/Modules/WindowMonitor/WindowMonitor.cs#L126-L130)）
- **`_lastSwitchedHwnd` の破棄位置:** `RefreshWindows` 内のコメント（[WindowMonitor.cs:230](src/Modules/WindowMonitor/WindowMonitor.cs#L230)）にある通り、破棄タイミングは「対象アプリ設定を空にした直後」等の分岐では再検討の余地がある
- **static HttpClient:** Claude/Codex の `HttpClient` は `PooledConnectionLifetime` 未設定のため、長時間稼働中に DNS 変更を拾わない可能性（常駐アプリなので留意）
