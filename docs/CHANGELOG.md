# CHANGELOG

## [Unreleased] - 2026-02-24

### Added

- **Phase 1**: WPF (.NET 8) + xUnit ソリューション構築、NuGet パッケージ追加
  - `LiveChartsCore.SkiaSharpView.WPF` (rc6.1), `ReactiveProperty`, `ClosedXML`, `System.Text.Encoding.CodePages`, `Microsoft.Extensions.DependencyInjection`
  - テスト: `xunit`, `Moq`, `FluentAssertions`
- **Phase 2**: Models 実装 (`DataPoint`, `PeakData`, `FfgData` - C# record)
- **Phase 3**: Services 実装
  - `FfgFileService`: Shift-JIS 読み込み、フッター検出、タイトル5文字切り詰め、ピーク計算
  - `CsvExportService`: 全シリーズ横並び列出力
  - `ExcelExportService`: PeakData シート + Data_[name] シート + Graph シート (OpenXml XY 散布図)
  - テストデータ 5 ファイル作成（Shift-JIS エンコード）
- **Phase 4**: Services 単体テスト (11 + 6 + 6 = 23 件)
- **Phase 5**: ViewModels 実装 + テスト
  - `PeakDataRow`: シリーズ色付き行データ
  - `MainViewModel`: ReactiveProperty/Command, 6 色パレット, シリーズ名重複解決, 軸自動調整
  - `MainViewModelTests`: 13 件テスト → 全合格
- **Phase 6**: Resources (Colors.xaml, ButtonStyles.xaml, DataGridStyles.xaml, ColorToBrushConverter)
- **Phase 7**: MainWindow.xaml (ToolBar, CartesianChart[コードビハインド注入], ピークデータグリッド, ステータスバー)
- **Phase 8**: App.xaml.cs (DI コンテナ, Shift-JIS 登録, 起動引数 D&D 対応)

### Test Results

```
合格: 37 / 37 (0 失敗, 0 スキップ)
```
