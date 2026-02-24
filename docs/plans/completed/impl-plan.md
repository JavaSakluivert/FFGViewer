# FFGViewer 実装計画

> 作成: 2026-02-24
> ステータス: 📋 承認待ち
> 参照仕様書: docs/SPEC.md

---

## 実装フェーズ概要

```
Phase 1  プロジェクト基盤構築
    │
Phase 2  Models（データ構造定義）
    │
Phase 3  Services 実装 ＋ テストデータ作成
    │
Phase 4  Services 単体テスト
    │
Phase 5  ViewModels 実装 ＋ 単体テスト
    │
Phase 6  Resources（スタイル・コンバータ）
    │
Phase 7  Views（XAML・コードビハインド）
    │
Phase 8  App 起動処理（アイコン D&D 対応）
    │
Phase 9  動作確認・コミット
```

---

## Phase 1: プロジェクト基盤構築

### 1-1. ソリューション・プロジェクト作成

```
作業内容:
  ① dotnet new wpf -n FFGViewer --framework net8.0-windows
  ② dotnet new xunit -n FFGViewer.Tests --framework net8.0
  ③ dotnet new sln -n FFGViewer
  ④ dotnet sln add FFGViewer/FFGViewer.csproj
  ⑤ dotnet sln add FFGViewer.Tests/FFGViewer.Tests.csproj
  ⑥ FFGViewer.Tests に FFGViewer プロジェクト参照を追加
```

### 1-2. NuGet パッケージ追加

**FFGViewer 本体:**

| パッケージ                                        | 用途              |
|---------------------------------------------------|-------------------|
| `LiveChartsCore.SkiaSharpView.WPF`                | グラフ描画        |
| `ReactiveProperty`                                | MVVM バインディング|
| `ClosedXML`                                       | Excel 出力        |
| `System.Text.Encoding.CodePages`                  | Shift-JIS 読み込み|
| `Microsoft.Extensions.DependencyInjection`        | DI コンテナ       |

**FFGViewer.Tests:**

| パッケージ                        | 用途                  |
|-----------------------------------|-----------------------|
| `xunit`                           | テストフレームワーク  |
| `xunit.runner.visualstudio`       | VS 統合               |
| `Moq`                             | モック作成            |
| `FluentAssertions`                | アサーション          |
| `Microsoft.NET.Test.Sdk`          | テスト実行基盤        |

### 1-3. フォルダ構成作成

```
FFGViewer/
  Models/ ViewModels/ Views/ Services/ Resources/Styles/ Resources/Converters/
FFGViewer.Tests/
  Services/ ViewModels/ TestData/ Helpers/
```

---

## Phase 2: Models

**作成ファイル（ロジックなし・純粋データ構造）:**

| ファイル                    | 内容                                             |
|-----------------------------|--------------------------------------------------|
| `Models/DataPoint.cs`       | `{ double Displacement; double Load; }`          |
| `Models/PeakData.cs`        | `{ MaxLoad, MaxLoadDisp, MinLoad, MinLoadDisp }` |
| `Models/FfgData.cs`         | `{ Title, FilePath, DataPoints, PeakData }`      |

---

## Phase 3: Services 実装

### 3-1. テストデータ .ffg ファイル作成

| ファイル                            | 内容                    |
|-------------------------------------|-------------------------|
| `TestData/normal_short_title.ffg`   | タイトル4文字・正常データ|
| `TestData/long_title.ffg`           | タイトル10文字以上      |
| `TestData/empty_data.ffg`           | データ0件（フッターのみ）|
| `TestData/single_point.ffg`         | データ1点のみ           |
| `TestData/invalid_format.ffg`       | 数値以外の行を含む      |

### 3-2. FfgFileService 実装

```
実装内容:
  - IFfgFileService インターフェース定義
  - Shift-JIS で行読み込み（CodePagesEncodingProvider 登録）
  - 1行目スキップ（空白行）
  - 2行目タイトル取得
  - 3行目以降: "変位 荷重" をパース
  - "-999.0" ペアで終端検出
  - タイトル切り詰め処理（5文字超え → 先頭5文字）
  ※ シリーズ名重複チェックは MainViewModel 側で行う
```

### 3-3. CsvExportService 実装

```
実装内容:
  - ICsvExportService インターフェース定義
  - 全シリーズを列として横並び出力
  - ヘッダー: Disp_[SeriesName], Load_[SeriesName]
  - 点数不一致時: 短い方を空文字で埋める
  - ファイル保存ダイアログは ViewModel 側で処理
```

### 3-4. ExcelExportService 実装

```
実装内容:
  - IExcelExportService インターフェース定義
  - ClosedXML で .xlsx 生成
  - シート: PeakData / Data_[シリーズ名] × n / Graph
  - Graph シート: XY 散布図（XYScatterLines）で全シリーズ含む
  - ファイル保存ダイアログは ViewModel 側で処理
```

---

## Phase 4: Services 単体テスト

SPEC.md セクション 13.5 のテストケース一覧に従い実装。

| テストクラス                  | テスト数（目安） |
|-------------------------------|-----------------|
| `FfgFileServiceTests`         | 11 件           |
| `CsvExportServiceTests`       | 6 件            |
| `ExcelExportServiceTests`     | 6 件            |

**テスト実行:** `dotnet test` で全テスト グリーン を確認してから次フェーズへ。

---

## Phase 5: ViewModels 実装 ＋ テスト

### 5-1. PeakDataRow（表示専用モデル）

```csharp
// ViewModels/PeakDataRow.cs
public class PeakDataRow
{
    public string SeriesName { get; init; }
    public string Sign       { get; init; }   // "(+)" or "(-)"
    public double Load       { get; init; }
    public double Displacement { get; init; }
    public Color  SeriesColor { get; init; }  // Name列テキスト色
}
```

### 5-2. MainViewModel 実装

```
実装内容:
  ReactiveProperty:
    - XAxisTitle, YAxisTitle（デフォルト値あり）
    - StatusMessage, CurrentFilePath

  ObservableCollection:
    - Series（LiveCharts2 ISeries）
    - XAxes, YAxes
    - PeakDataItems（PeakDataRow）

  ReactiveCommand:
    - OpenFileCommand   → FfgFileService.Load → AddSeries
    - ClearGraphCommand → Series/PeakDataItems を Clear
    - CopyPeakDataCommand → タブ区切りでクリップボードへ
    - ExportExcelCommand  → 保存ダイアログ → ExcelExportService
    - ExportCsvCommand    → 保存ダイアログ → CsvExportService
    - ShowAboutCommand  → About ダイアログ

  シリーズ追加ロジック:
    - シリーズ名重複チェック・連番付与
    - カラーパレット循環割り当て
    - LineSeries + ScatterSeries（ピーク点）を同色で追加
    - 軸範囲の自動調整（±10% 余白）
```

### 5-3. MainViewModelTests

SPEC.md セクション 13.5 の 13 件テストを実装・グリーン確認。

---

## Phase 6: Resources

| ファイル                              | 内容                                     |
|---------------------------------------|------------------------------------------|
| `Resources/Colors.xaml`               | カラー定数定義（#1976D2, #F5F5F5 等）    |
| `Resources/Styles/ButtonStyles.xaml`  | ボタンスタイル（マテリアルブルー系）     |
| `Resources/Styles/DataGridStyles.xaml`| DataGrid・Name列の色付きテキストスタイル |
| `Resources/Converters/ColorToBrushConverter.cs` | `Color → SolidColorBrush`    |

---

## Phase 7: Views（XAML）

### MainWindow.xaml 実装内容

```
レイアウト（Grid ベース）:
  Row 0（Auto） ツールバー: [Open File] [パス表示] [Clear Graph]
  Row 1（*）    メインエリア:
    Col 0（*）  CartesianChart（パン・ズーム・凡例内表示）
    Col 1（280）右パネル: DataGrid + ボタン群 + 軸タイトル入力
  Row 2（Auto） ステータスバー

D&D 設定:
  AllowDrop="True"
  DragOver / Drop イベントハンドラ（コードビハインド最小限）
```

---

## Phase 8: App 起動処理

```csharp
// App.xaml.cs
protected override void OnStartup(StartupEventArgs e)
{
    // Shift-JIS 有効化
    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    // DI コンテナ構築

    // 起動引数があれば D&D として読み込み
    if (e.Args.Length > 0)
        // MainViewModel.LoadFilesAsync(e.Args) を呼び出し
}
```

---

## Phase 9: 動作確認・コミット

### 確認項目

| 確認内容                              | 方法              |
|---------------------------------------|-------------------|
| `dotnet test` 全テストグリーン        | CLI               |
| .ffg ファイルをダイアログで開ける     | 手動              |
| GUI への D&D でグラフが描画される     | 手動              |
| アイコンへの D&D で起動＋描画される   | 手動              |
| 複数ファイル重ね書きで色分け表示      | 手動              |
| ウィンドウリサイズでグラフが伸縮する  | 手動              |
| Excel エクスポートで散布図が含まれる  | 手動              |
| CSV エクスポートで列並びが正しい      | 手動              |

### コミット方針（Conventional Commits）

```
feat: プロジェクト基盤構築・パッケージ追加
feat(models): データ構造定義
feat(services): FfgFileService 実装
test(services): FfgFileService 単体テスト
feat(services): CsvExportService / ExcelExportService 実装
test(services): エクスポートサービス単体テスト
feat(viewmodels): MainViewModel 実装
test(viewmodels): MainViewModel 単体テスト
feat(ui): Resources スタイル・コンバータ実装
feat(ui): MainWindow XAML 実装
feat(app): 起動処理・D&D 対応
```

---

## 作業ファイル一覧（全体）

```
FFGViewer/
├── App.xaml / App.xaml.cs
├── Models/
│   ├── DataPoint.cs
│   ├── PeakData.cs
│   └── FfgData.cs
├── ViewModels/
│   ├── MainViewModel.cs
│   └── PeakDataRow.cs
├── Views/
│   ├── MainWindow.xaml
│   └── MainWindow.xaml.cs
├── Services/
│   ├── IFfgFileService.cs / FfgFileService.cs
│   ├── ICsvExportService.cs / CsvExportService.cs
│   └── IExcelExportService.cs / ExcelExportService.cs
└── Resources/
    ├── Colors.xaml
    ├── Styles/
    │   ├── ButtonStyles.xaml
    │   └── DataGridStyles.xaml
    └── Converters/
        └── ColorToBrushConverter.cs

FFGViewer.Tests/
├── Services/
│   ├── FfgFileServiceTests.cs
│   ├── CsvExportServiceTests.cs
│   └── ExcelExportServiceTests.cs
├── ViewModels/
│   └── MainViewModelTests.cs
├── TestData/
│   ├── normal_short_title.ffg
│   ├── long_title.ffg
│   ├── empty_data.ffg
│   ├── single_point.ffg
│   └── invalid_format.ffg
└── Helpers/
    └── FfgDataBuilder.cs
```

---

*作成: Claude Code (claude-sonnet-4-6)*
