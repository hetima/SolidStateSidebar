# モジュール分割リファクタリング

---

## Phase 1: Settings データモデル構造化 (完了)

Settings クラスの `MonitorConfig[]` 配列を、構造化された `Dictionary<string, IModuleData> Modules` に置き換えた。
各モジュールの設定値（Params）を専用 Data クラスの直接プロパティに展開し、`ConfigParam` / `ParamKey` の間接参照を排除した。

### Phase 1-A: IModuleData インターフェース + Data クラス

- **新規 `src/Core/IModuleData.cs`** — `IModuleData` インターフェース
  - プロパティ: `Name` (読取専用, UI用), `Enabled`, `Order`, `Hardware[]`, `Metrics[]`, `HardwareOC?`, `Clone()`
- **6つの Data クラス** (`src/Module/{ModuleName}/Data.cs`)
  - `SSS.Module.CpuMonitor.Data` — ShowHardwareNames, RoundAll, AllCoreClocks, UseGHz, UseFahrenheit, TempAlert
  - `SSS.Module.RamMonitor.Data` — RoundAll
  - `SSS.Module.GpuMonitor.Data` — ShowHardwareNames, RoundAll, UseGHz, UseFahrenheit, TempAlert
  - `SSS.Module.HdMonitor.Data` — RoundAll, UsedSpaceAlert
  - `SSS.Module.NetworkMonitor.Data` — ShowHardwareNames, RoundAll, UseBytes, BandwidthInAlert, BandwidthOutAlert
  - `SSS.Module.TimeMonitor.Data` — Clock24HR, DateFormat
  - すべて `JsonObject(MemberSerialization.OptIn)` + `[JsonProperty]` でシリアライズ
  - `IModuleData IModuleData.Clone() => Clone();` の明示的インターフェース実装が必要（共変戻り値がC#で機能しないため）
  - `Name` プロパティは `[JsonIgnore]` で `Strings.CPU` 等を返す（UI専用）

### Phase 1-B: Settings クラス リファクタ

- **`src/Core/Settings.cs`**
  - `MonitorConfig[] Monitors` → `Dictionary<string, IModuleData>? Modules`
  - `MonitorConfig.CheckConfig(...)` → `Settings.CheckModules(...)` 静的メソッド
  - `CheckModules`: 全6モジュールエントリの存在確認、欠損補完、Metrics 配列マージ
- **`src/Module/ModuleDataConverter.cs`** — `JsonConverter<Dictionary<string, IModuleData>>`
  - キー文字列 → 具体的な Data 型にマッピングしてデシリアライズ
  - `GetDefaults()` でデフォルト値を生成
- **`src/App.xaml.cs`** — 初期化呼び出しを `CheckModules` に変更
- **`src/Models/SidebarModel.cs`** — `MonitorManager` 初期化引数を `Modules` に変更

### Phase 1-C: MonitorManager + Monitor クラス群

- **`src/Core/MonitorManager.cs`** — コンストラクタ引数を `Dictionary<string, IModuleData>` に変更
  - `using` エイリアスで型解決: `using CpuData = SSS.Module.CpuMonitor.Data;` 等
  - モジュールごとのファクトリメソッド: `CpuPanel()`, `RamPanel()`, `GpuPanel()`, `HdPanel()`, `NetworkPanel()`, `TimePanel()`
  - RAM ハードウェア重複排除ロジック（`modules["RamMonitor"]` を参照）
  - `OrderByDescending(m => m.Value.Order)` でソートしてパネル作成
- **`src/Core/OHMMonitor.cs`** — `GetInstances` 引数から `ConfigParam[]` を除去し、個別パラメータに
- **`src/Core/DriveMonitor.cs`** — 同上
- **`src/Core/NetworkMonitor.cs`** — 同上
- **`src/Core/ClockMonitor.cs`** — 同上

### Phase 1-D: SettingsModel 全面書き換え

- **`src/Models/SettingsModel.cs`**
  - `ObservableCollection<MonitorConfig> MonitorConfig` → `ObservableCollection<IModuleData> Modules`
  - `MonitorConfig? SelectedMonitor` → `IModuleData? SelectedModule`
  - コンストラクタ: `Core.Settings.Instance.Modules` から Clone、パターンマッチングで `MonitorType` 判定して `HardwareOC` 構築
  - `Save()`: HardwareOC → Hardware 配列に正規化、Order 再採番、パターンマッチングで Dictionary に変換

### Phase 1-E: Settings UI 更新

- **`src/Converters/MonitorConfigTemplateSelector.cs`** — `MonitorConfig.Type` → 具体的な Data 型でマッチング
- **`src/Views/Settings/SettingsMonitorsView.xaml`**
  - `MonitorConfig` → `Modules`, `SelectedMonitor` → `SelectedModule`
  - `DataType="{x:Type monitor:IModuleData}"` に変更
- **`src/Views/Settings/SettingsMonitorsView.xaml.cs`** — ドロップ処理を `IModuleData` に変更
- **6つの SettingPanel.xaml** — 汎用 `ItemsControl ItemsSource="{Binding Params}"` を個別プロパティの直接バインディングに置換
  - Boolean → `CheckBox`、Int32 → `TextBox` + `IntConverter`
  - TimeMonitor の DateFormat は `ComboBox`（`DateFormatConverter` / `DateFormatDisplayConverter` 保持）

### Phase 1-F: クリーンアップ

**削除したもの:**
- `src/Core/MonitorConfig.cs` — `MonitorConfig` クラス全体
- `src/Core/ConfigParam.cs` — `ConfigParam` クラス全体
- `src/Core/MonitoringExtensions.cs` — `GetValue<T>(this ConfigParam[], ParamKey)` 拡張メソッド
- `src/Core/MonitoringEnums.cs` — `ParamKey` enum

**分離・作成したもの:**
- `src/Core/HardwareConfig.cs` — `HardwareConfig` クラス（元 MonitorConfig.cs から分離）
- `src/Core/MetricConfig.cs` — `MetricConfig` クラス（元 MonitorConfig.cs から分離）

### 既知の問題

- 設定「適用」後に設定ウィンドウの Hardware 一覧が消える（保存は正常、開き直せば表示される）
  - 原因: `Save()` で `HardwareOC` の内容を `Hardware` 配列に正規化する際の WPF バインディング切れ
  - 対応: 未定（後回し）

### 設計上のポイント

- `HardwareConfig` / `MetricConfig` はすべてのモジュールで共有（クラス変更なし）
- シリアライズ形式は JSON キー → Data 型マッピング（`ModuleDataConverter`）
- 設定UIは `DataTemplateSelector` で各モジュールの専用 SettingPanel を選択
- Data クラスは `src/Module/{ModuleName}/Data.cs` に配置（Phase 2 の Section 分離に備えた配置）

---

## Phase 2: Sidebar セクション分割 (完了)

サイドバーの各モジュールを個別の Section.xaml ファイルに分離した。

### Phase 2-A: Section UserControl 作成

- **6つの Section.xaml** (`src/Module/{ModuleName}/Section.xaml` + `.xaml.cs`)
  - `SSS.Module.CpuMonitor.Section` — BaseMonitor DataType + ShowName Trigger
  - `SSS.Module.RamMonitor.Section` — 同上
  - `SSS.Module.GpuMonitor.Section` — 同上
  - `SSS.Module.HdMonitor.Section` — DriveMonitor DataType + LoadBar（Stacked/Inline）表示ロジック
  - `SSS.Module.NetworkMonitor.Section` — BaseMonitor DataType + ShowName Trigger
  - `SSS.Module.TimeMonitor.Section` — BaseMonitor DataType（ShowName Trigger なし）
  - 各 Section は `DataContext` に `MonitorPanel` を想定、`{Binding SvgImageSource}` / `{Binding Title}` / `{Binding Monitors}` でバインド

### Phase 2-B: MonitorPanelTemplateSelector 拡張

- **`src/Converters/MonitorPanelTemplateSelector.cs`**
  - `DefaultTemplate` / `TimeTemplate` → `CpuTemplate` / `RamTemplate` / `GpuTemplate` / `HdTemplate` / `NetworkTemplate` / `TimeTemplate` に変更
  - `MonitorPanel.Type` で switch 式で各テンプレートにルーティング

### Phase 2-C: Sidebar.xaml 簡素化

- **`src/Views/Sidebar.xaml`**
  - ~200 行のインライン DataTemplate（DefaultMonitorPanelTemplate / TimeMonitorPanelTemplate）を削除
  - 6つの Section 参照 DataTemplate に置換（`<cpuSection:Section />` 等）
  - 各モジュールの xmlns エイリアスを追加（`cpuSection`, `ramSection`, `gpuSection`, `hdSection`, `netSection`, `timeSection`）
  - `MonitorPanelTemplateSelector` へのテンプレート割り当てを6モジュール対応に更新

### 設計上のポイント

- `MonitorPanel` オブジェクトの生成ロジック（`MonitorManager` のファクトリメソッド）は変更なし
- 各 Section.xaml 内で `DataType="{x:Type core:BaseMonitor}"` 等を使用し、WPF の暗黙的テンプレート解決を利用
- `MetricLabelConverter` 等の App.xaml リソースは Section.xaml からも参照可能（アプリケーションリソースとして解決）

### 前提条件
- Phase 1 完了済み
