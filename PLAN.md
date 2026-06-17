# SolidStateSidebar リファクタリング計画

SidebarDiagnostics フォーク由来の技術的負債を段階的に解消するための計画。
各フェーズは独立して完結・ビルド確認・コミットできる粒度にし、ビッグバン書き換えは行わない。

---

## 現状の問題点（調査結果）

### A. レガシーパターンの蔓延（SidebarDiagnostics 由来）

2. **INotifyPropertyChanged 実装の重複と不統一**
   - `BaseMonitor` / `BaseMetric` / `MonitorPanel` / `SidebarModel` / `MonitorManager` / `SettingsModel` / 各 Data.cs がそれぞれ独自に `NotifyPropertyChanged` を実装。
   - 文字列指定（`nameof(X)` 手書き）と `[CallerMemberName]`（Data.cs 系）が混在。
   - getter/setter が1プロパティあたり10行前後のボイラープレートになっている（`MetricBase.cs` は336行中200行超がこれ）。


4. **命名規約違反**
   - `iMonitor` / `iMetric` / `iConverter` → .NET 規約では `IMonitor` / `IMetric` / `IConverter`。
   - `nValue` / `nAppend`（`MetricBase.cs`）。
   - `String` と `string` の混在（`MonitorPanel.cs:90-92`）。
   - typo: `pRocessicon`（`WindowMonitor.cs:302`）、`lengthRetuen`（`WindowMonitor.cs:444`）。

### B. God ファイル

| ファイル | 行数 | 内容 |
|---|---|---|
| `src/Utilities/Windows.cs` | 1672 | `NativeMethods` / `WindowHelper` / `ShowDesktop` / `Devices` / `Hotkey` / `WorkArea` / `Monitor` / `DPIAwareWindow` / `AppBarWindow` など約15クラスが同居 |
| `src/Models/SettingsModel.cs` | 1108 | 設定画面の全 ViewModel + `DockItem` 等の補助クラス |
| `src/Core/Settings.cs` | 828 | 設定シングルトン + `SectionHeaderStyle` enum + その他 |

### C. コード重複

1. **ClaudeMonitor / CodexMonitor がほぼコピペ**
   `Data.cs` は名前以外ほぼ同一（123行 × 2）。`Section.xaml(.cs)` / `SettingPanel.xaml` も SVG 名以外同一。`StartAutoRefresh` / `FormatWindow` / `FormatAbsolute` / `FormatCountdown` / タイマー管理が両モニターに重複。
2. **MonitorManager の Create*Panel 群**（`MonitorManager.cs:194-320`）が同型コードの繰り返し。
3. **モジュール Data.cs の共通部**（Enabled / Order / Hardware / SectionHeaderStyle / Clone）が全モジュールにコピペされている。

### D. 層の依存関係違反（MVVM 崩れ）

1. **Model → View の逆依存:** `WindowMonitor.IsPointerOnSidebar()`（`WindowMonitor.cs:393-406`）が `App.Current.Sidebar?.IsMouseOver` を直接参照。Core モジュールが View に依存している。
2. **Metric に UI ロジック:** `BaseMetric.IsAlert` setter（`MetricBase.cs:275-302`）が `DispatcherTimer` を生成し点滅色 `AlertColor` を管理。表示の関心がデータモデルに混入し、`App.Current.Dispatcher` 依存も発生。
3. **MonitorPanel が leaky abstraction:** 汎用コンテナのはずが `ShortResetDisplay` / `LongResetDisplay` / `AutoRefresh`（Claude/Codex 専用）、`FontSize` / `FontName`（Window 専用）を持つ（`MonitorPanel.cs:155-187`）。モジュールが増えるたびに肥大する構造。
4. **Section.xaml.cs の DataContextChanged でビジネスロジック起動:** Claude/Codex の `StartAutoRefresh` が View のライフサイクルに結合（`ClaudeMonitor/Section.xaml.cs:22-28`）。

### E. その他

1. **誤解を招く名前:** `MonitorManager.UpdateWindowMonitor()` は実際には**全モニター**を更新する（`MonitorManager.cs:128-135`）。
2. **デッドコード:** 空の `WindowItem_PropertyChanged`（`WindowMonitor.cs:64-66`）とその購読処理一式、`_applications` の「//いらない」コメント（`WindowMonitor.cs:19`）、`App.StartApp` 内の未使用 `_vstring`（`App.xaml.cs:91-92`）。
3. **握りつぶし `catch { }`:** Claude/Codex の通信・JSON 解析・トークン書き戻しが全て無言で失敗する。デバッグ不能。
4. **リロード方式:** 設定変更のたびにウィンドウを `Close()` → static フラグ `App._reloading` 経由で再生成（`Sidebar.xaml.cs:264-276`）。状態管理が追いにくい。
5. **JSON ライブラリ混在:** 設定は Newtonsoft.Json、Claude/Codex は System.Text.Json。
6. **モジュール追加コストが高い:** 1モジュール追加に 9 箇所の手作業（CLAUDE.md「新モジュール追加の手順」参照）。
7. **テストが皆無:** ソリューションにテストプロジェクトが存在しない。

---

## 基本方針

- **settings.json の後方互換を絶対に壊さない。** `[JsonProperty("...")]` のキー名と `ModuleDataConverter` のキー（`"CpuMonitor"` 等）は変更しない。
- **`src/WPFDevelopers/` はサードパーティ扱いで触らない。**
- **挙動変更とリファクタリングを同一コミットに混ぜない。**
- 各フェーズ完了時に `dotnet build` が警告増なしで通ること。リリース前には実機での目視確認（サイドバー表示・設定変更・リロード・Claude/Codex 取得）を行う。
- フェーズは番号順が推奨だが、1〜3 は独立しており順不同で着手可能。

---

## Phase 0: 安全網の整備（半日）

リファクタリング前の足場固め。挙動変更なし。

- [ ] `.editorconfig` を追加し、現状コードベースの実態に合わせた最小ルールを定義（`string` 統一、フィールド命名 `_camelCase` 等）。一括フォーマットは**しない**（diff 汚染防止）。
- [ ] `dotnet build -warnaserror` ではなく現状の警告数を記録し、ベースラインとする（以後のフェーズで増やさない）。
- [ ] 現在の `settings.json`（全モジュール有効状態）をテストデータとして `docs/` または手元に保全。各フェーズ後に「旧 settings.json を読み込んで設定が失われない」ことを確認する手順を README コメント等に明文化。
- [ ] リファクタリング用ブランチ `refactor/phase-N` 運用を決める。

**完了条件:** ビルド成功。挙動差ゼロ。

---

## Phase 1: デッドコード除去と機械的修正（半日・低リスク）

意味を変えない削除とリネームのみ。レビューしやすいよう小さいコミットに分ける。

- [x] `WindowMonitor.cs`: 空の `WindowItem_PropertyChanged` とその purchase/購読・解除コード（`Windows` setter 内、`Dispose` 内）を削除。
- [x] `WindowMonitor.cs:19`: `_applications` の「//いらない」を解決する（`_applicationNames` 生成後は実際に `Update`/`UpdateFromHook` の null/Length チェックでしか使っていない → チェックをコンストラクタに移して削除、または保持するならコメント除去）。
- [x] typo 修正: `pRocessicon` → `processIcon`、`lengthRetuen` → `lengthReturn`。
- [x] `App.xaml.cs` `StartApp()`: 未使用の `_version` / `_vstring` を削除。
- [x] `MonitorManager.UpdateWindowMonitor()` → `UpdateAllMonitors()` にリネーム（呼び出し元は `Update()` のみ）。
- [x] `String` → `string` 統一（`MonitorPanel.cs` 等、出現箇所のみ）。
- [x] 使われていない using の削除（自分が触ったファイルのみ）。

**完了条件:** ビルド成功・警告数ベースライン維持。挙動差ゼロ。

---

## Phase 2: INotifyPropertyChanged 基盤の統一（1〜2日）

最も diff が大きいが機械的なフェーズ。**プロパティ名（= バインディングパス）と JSON キーは一切変えない。**

- [x] `src/Core/ObservableObject.cs` を新設:
  ```csharp
  public abstract class ObservableObject : INotifyPropertyChanged
  {
      public event PropertyChangedEventHandler? PropertyChanged;
      public virtual void NotifyPropertyChanged([CallerMemberName] string? name = null) ...
      protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null) ...
  }
  ```
  既存の呼び出しが `nameof(X)` 明示と引数なしの両方を使うため、`[CallerMemberName]` で両対応。`NotifyPropertyChanged` は `public`（外部呼び出しはなかったが既存互換のため）かつ `virtual`（`SettingsModel` の `IsChanged` 連動を override で実現するため）。
- [x] 適用対象（1コミット = 1〜2クラス）:
  - `BaseMonitor` / `BaseMetric` / `MonitorPanel`
  - `SidebarModel` / `MonitorManager`
  - 各モジュール `Data.cs`（7+2個）— 既存の `[CallerMemberName]` 実装を基底クラスに置換
  - `SettingsModel` / `Settings` / `WindowItem` / `HardwareConfig` / `MetricConfig`
- [x] 単純な転送プロパティは `SetProperty` で 3 行に圧縮。`IsAlert` のような副作用付き setter はロジックを変えずそのまま移植（副作用の整理は Phase 6）。

**検証:** ビルド警告0で完了。**実機確認は未実施**（リリース前に要: サイドバー全セクションの値更新・設定画面の双方向バインディング・アラート点滅の目視確認）。バインディングエラーは VS 出力ウィンドウで確認。

**リスク:** バインディング切れ（プロパティ名 typo）。対策: リネームは IDE のリファクタリング機能のみ使用し、手書き文字列の `nameof` 化を優先。

---

## Phase 4: God ファイルの分割（1〜2日）

**ファイル移動のみ。コード変更を混ぜない**（git の rename 追跡を効かせる）。

- [x] `src/Utilities/Windows.cs`（1672行）→ `src/Platform/` フォルダに分割:
  - `NativeMethods.cs`（P/Invoke 宣言を集約）
  - `OS.cs` / `WindowHelper.cs` / `ShowDesktop.cs` / `Devices.cs` / `Hotkey.cs`
  - `Monitor.cs`（`Monitor` / `WorkArea` / `RECT` / `MonitorExtensions`）
  - `DPIAwareWindow.cs` / `AppBarWindow.cs`（`DockEdge` は使用側の `AppBarWindow.cs` に同梱）
  - 名前空間 `SSS.Windows` は維持（参照箇所の変更ゼロで済む）。フォルダ名は `Window` モジュールとの混同を避け `Windows` ではなく `Platform` を採用。
- [ ] `src/Models/SettingsModel.cs`（1108行）→ `DockItem` / `ScreenItem` / `TextAlignItem` を別ファイルへ。`SettingsModel` 本体はタブ単位の partial 分割を検討（無理に分けない。明確な境界がなければ補助クラスの分離だけで止める）。
- [ ] `src/Core/Settings.cs` → `SectionHeaderStyle` 等の enum と付随クラスを `MonitoringEnums.cs` または専用ファイルへ移動。
- [ ] `src/Core/Converters.cs`（センサー値の単位変換 `iConverter`）→ `UnitConverters.cs` にリネームし、WPF の `src/Converters/Converters.cs` との混同を解消。

**検証:** ビルドのみで十分（移動だけのため）。

---

## Phase 5: Claude/Codex モニターの共通化（1日）

最も新しいコードだが最も重複が濃い。今後この種の「外部 API 使用量」モジュールが増える可能性が高いため早めに統合する。

- [ ] `src/Modules/UsageMonitor/`（または `Core/`）に共通基盤を抽出:
  - `UsageMonitorBase : BaseMonitor` — タイマー管理（`StartAutoRefresh` / `Dispose`）、`ManualRefresh` の Loading→取得→SetText フロー、`FormatAbsolute` / `FormatCountdown` / 「util% (reset)」整形。派生クラスは「トークン取得」と「JSON→(使用率, リセット時刻) 抽出」だけを実装する。
  - `UsageMetric`（現 `ClaudeUsageMetric` / `CodexUsageMetric` を統合）。
  - `UsageModuleData` — 共通の `AutoRefresh` / `ShortResetDisplay` / `LongResetDisplay` / `SectionHeaderStyle` を持つ基底 Data。**JSON キーと設定キー（`"ClaudeMonitor"` / `"CodexMonitor"`）は変えない。**
- [ ] `Section.xaml(.cs)` を共通 UserControl 化（SVG ファイル名とモニター型をパラメータ化）。
- [ ] あわせて `catch { }` を `catch (Exception ex)` + `Debug.WriteLine` に変更し、メトリクスの "Error" 表示にカテゴリを付けられる余地を作る（例: 認証ファイルなし→ "No auth"、HTTP 失敗→ "Error"）。表示文言の変更は最小限に。

**検証:** ビルド + 実機で Claude/Codex 両方の取得・手動更新・自動更新間隔変更・Countdown/Absolute 切替を確認。旧 settings.json の読み込み互換も確認。

**リスク:** ClaudeMonitor のトークンリフレッシュ（credentials.json 書き戻し）は Claude 固有なので基底に持ち込まない。

---

## Phase 6: 層の依存関係是正（2日・要設計判断）

挙動を変えずに依存の向きを直す。一番「リファクタリングらしい」フェーズ。

- [ ] **WindowMonitor の View 依存を除去:**
  `IsPointerOnSidebar()` の `App.Current.Sidebar` 直参照をやめ、`Func<bool>`（または値）をコンストラクタ/メソッド引数で注入する。呼び出し階層は `Sidebar` → `SidebarModel` → `MonitorManager` → `WindowMonitor` と既に通っているため、ここに乗せる。
- [ ] **BaseMetric から点滅 UI を分離:**
  `IsAlert`（bool 通知）まではモデルの責務として残し、`AlertColor` + `DispatcherTimer` 点滅は View 側（Converter または添付ビヘイビア＋ XAML の `DataTrigger`/`Storyboard`）に移す。これで `BaseMetric` の `App.Current.Dispatcher` 依存が消える。
  ※ XAML 側の変更を伴うため、全セクションのアラート表示を実機確認すること。
- [ ] **MonitorPanel のモジュール固有プロパティを解消:**
  案A（推奨・小）: `MonitorPanel` に `IModuleData` への参照を持たせ、各 Section が自分の Data 型にキャストして読む。固有プロパティ 5 個を削除。
  案B（大）: パネルをモジュールごとに派生させる。→ 過剰なので採らない。
- [ ] **Claude/Codex の起動タイミングを View から分離:**
  `Section.DataContextChanged` での `StartAutoRefresh` 呼び出しを、`MonitorManager` のパネル生成直後（または `SidebarModel.Start()`）へ移動。View はボタンクリック→`ManualRefresh` の転送のみ。

**検証:** 実機確認必須（アラート点滅、Window モニターのマウスオーバー時更新停止、Claude/Codex 自動更新、リロード後の再開）。

---

## Phase 7: モジュール登録の一元化（1日・任意）

新モジュール追加コスト（現在 9 箇所）を下げる。効果はあるが必須ではないので、今後モジュールを増やす予定がなければスキップ可。

- [ ] `ModuleDescriptor`（キー文字列、`MonitorType`、Data 型、`Data.Default` ファクトリ、`MonitorPanel` ファクトリ、Section/SettingPanel の DataTemplate キー）を定義し、`src/Core/ModuleRegistry.cs` に全モジュールを1箇所で列挙。
- [ ] `ModuleDataConverter` の 2 つの辞書、`MonitorManager` の switch、`MonitorPanelTemplateSelector` / `MonitorConfigTemplateSelector` を Registry 参照に置換。
- [ ] CLAUDE.md の「新モジュール追加の手順」を更新（9 ステップ → 「モジュールフォルダ作成 + Registry 登録 + enum 追加」程度になる想定）。

**検証:** ビルド + 全モジュールの表示・設定タブ・有効/無効切替・並べ替えを実機確認。旧 settings.json 互換確認。

---

## Phase 8: 信頼性の改善（1日）

リファクタリングというより既知の懸念（CLAUDE.md「既知の懸念・潜在的なバグ」）への対処。挙動が変わるため最後に分離。

- [ ] **ClaudeMonitor のトークン書き戻し安全化:** 書き戻し失敗時に無視せず、リトライ（次回更新時に再試行できるようメモリ上に新トークンを保持）+ `Debug.WriteLine`。ファイル書き込みは tmp ファイル → `File.Replace` のアトミック置換にする。
- [ ] **Countdown 表示の鮮度:** Claude/Codex の残り時間を、API 再取得なしで 1 分ごとに再計算して表示更新する（取得済み `resets_at` を保持すれば可能）。`AutoRefresh = Manual` でもカウントダウンが進むようになる。
- [ ] **HttpClient:** `SocketsHttpHandler { PooledConnectionLifetime = 15min }` を共通基盤（Phase 5 の `UsageMonitorBase`）に設定。
- [ ] **スクロール切替の起点:** `TryScrollSwitch` で `_lastSwitchedHwnd` 不在時にフォアグラウンドウィンドウを起点として探す（`GetForegroundWindow` と照合）。

**検証:** 実機で長時間動作（タイマー跨ぎ）、トークン期限切れ前後の動作、Claude Code 併用時に credentials.json が壊れないこと。

---

## やらないこと（明示的に対象外）

- **DI コンテナ / MVVM フレームワーク（CommunityToolkit.Mvvm 等）の導入** — このアプリの規模では `ObservableObject` 自作 + コンストラクタ注入で十分。依存追加のコストが利益を上回る。
- **Newtonsoft.Json → System.Text.Json への設定移行** — `ModuleDataConverter` の挙動互換リスクが高い割に利益が薄い。混在は許容する（Claude/Codex の API 読み取りは STJ のままで良い）。
- **`src/WPFDevelopers/` の改修** — サードパーティコード。
- **`"HdMonitor"` / `MonitorType.HD` 等の名前統一** — settings.json キーと連動しており互換を壊す。フォルダ名 `DriveMonitor` と名前空間 `HdMonitor` の不一致は CLAUDE.md の注意書きで運用する。
- **テストプロジェクトの新規整備** — UI 中心 + ハードウェア依存でユニットテストの費用対効果が低い。Phase 5 で抽出する整形ロジック（`FormatCountdown` 等）が増えてきたら、その純粋関数群に限定して導入を再検討する。
- **リロード方式（ウィンドウ再生成）の刷新** — 動いており、差し替えのリスクが高い。`App._reloading` static フラグの整理程度に留める（Phase 1 で扱わず、必要になったときに別途計画）。

## 進め方の目安

| フェーズ | 工数目安 | リスク | 依存 |
|---|---|---|---|
| 0 安全網 | 0.5日 | なし | — |
| 1 デッドコード | 0.5日 | 低 | — |
| 2 INPC 統一 | 1〜2日 | 中（バインディング切れ） | — |
| 3 ファイナライザ | 0.5日 | 低 | 2 推奨 |
| 4 ファイル分割 | 1〜2日 | 低 | — |
| 5 Claude/Codex 共通化 | 1日 | 中 | 2 |
| 6 依存関係是正 | 2日 | 中〜高（UI 確認必須） | 2, 5 |
| 7 モジュール登録一元化 | 1日 | 中 | 5 推奨 |
| 8 信頼性改善 | 1日 | 中 | 5 |

合計 8〜10 日相当。1〜4 だけでもコードベースの可読性は大きく改善するため、5 以降は効果を見て取捨選択してよい。
