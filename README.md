# FFGViewer

構造物の荷重−変位ヒステリシス曲線（`.ffg` 形式）を可視化する WPF デスクトップアプリケーションです。

## 機能

| 機能 | 説明 |
|------|------|
| **ファイル読み込み** | `.ffg` ファイルをメニュー・ドラッグ＆ドロップで複数同時読み込み |
| **グラフ表示** | シリーズごとに色分けされた折れ線グラフ（ピーク点マーカー付き）、凡例表示、パン・ズーム対応 |
| **ピークデータ表示** | 各シリーズの最大/最小荷重・変位をテーブルに色分け表示 |
| **クリップボードコピー** | ピークデータをタブ区切りでクリップボードへコピー |
| **CSV エクスポート** | 全シリーズを列形式（Disp_[名前], Load_[名前]）で 1 ファイル出力 |
| **Excel エクスポート** | PeakData シート・データシート・XY 散布図グラフシートを含む `.xlsx` 出力 |
| **軸タイトル編集** | X 軸・Y 軸のタイトルをリアルタイム変更 |

## スクリーンショット

```
┌─────────────────────────────────────────────────────────────────┐
│ [ファイルを開く] [グラフクリア]  ファイル: path/to/last.ffg        │  ← ツールバー
├──────────────────────────────────────────┬──────────────────────┤
│                                          │ ピークデータ           │
│           CartesianChart                 │ ┌─────┬──┬──────┬──┐ │
│     (荷重−変位 ヒステリシス曲線)            │ │シリーズ│符│荷重  │変位│ │
│    ← ドラッグでパン、ホイールでズーム →     │ │S1   │(+│ 20.00│2.00│ │
│                                          │ │S1   │(-│  0.00│0.00│ │
│                                  [凡例]  │ └─────┴──┴──────┴──┘ │
│                                          │ ─────────────────── │
│                                          │ 軸タイトル             │
│                                          │ X軸: [変位 (mm)      ] │
│                                          │ Y軸: [荷重 (kN)      ] │
│                                          │ [ピークデータをコピー]   │
│                                          │ [Excel エクスポート]    │
│                                          │ [CSV エクスポート]      │
├──────────────────────────────────────────┴──────────────────────┤
│ 読み込み完了: S1                                                   │  ← ステータスバー
└─────────────────────────────────────────────────────────────────┘
```

## .ffg ファイル形式

```
（空行）
タイトル行（最大5文字、超過分は切り詰め）
変位1 荷重1
変位2 荷重2
...
-999.0 -999.0    ← フッター（データ終端）
```

**エンコーディング**: Shift-JIS
**区切り文字**: 半角スペース

## 技術スタック

| 項目 | 内容 |
|------|------|
| **フレームワーク** | WPF (.NET 8.0-windows) |
| **アーキテクチャ** | MVVM |
| **グラフ描画** | [LiveChartsCore.SkiaSharpView.WPF](https://livecharts.dev/) 2.0.0-rc6.1 |
| **バインディング** | [ReactiveProperty](https://github.com/runceel/ReactiveProperty) 9.x |
| **Excel 出力** | [ClosedXML](https://github.com/ClosedXML/ClosedXML) 0.105.0 + DocumentFormat.OpenXml |
| **DI コンテナ** | Microsoft.Extensions.DependencyInjection |
| **テスト** | xUnit + Moq + FluentAssertions |

## プロジェクト構成

```
FFGViewer/
├── FFGViewer/                     # メインプロジェクト
│   ├── Models/
│   │   ├── DataPoint.cs           # 変位・荷重の1点データ
│   │   ├── PeakData.cs            # ピーク値（最大/最小荷重）
│   │   └── FfgData.cs             # シリーズ全体データ
│   ├── Services/
│   │   ├── FfgFileService.cs      # .ffg ファイル読み込み（Shift-JIS）
│   │   ├── CsvExportService.cs    # CSV エクスポート
│   │   └── ExcelExportService.cs  # Excel エクスポート（XY散布図付き）
│   ├── ViewModels/
│   │   ├── MainViewModel.cs       # メインビューモデル（ReactiveProperty）
│   │   └── PeakDataRow.cs         # ピークデータ行（色付き）
│   ├── Resources/
│   │   ├── Colors.xaml            # カラーパレット（Material Blue）
│   │   ├── Styles/ButtonStyles.xaml
│   │   ├── Styles/DataGridStyles.xaml
│   │   └── Converters/ColorToBrushConverter.cs
│   ├── MainWindow.xaml / .cs
│   └── App.xaml / .cs             # DI 初期化・起動引数処理
├── FFGViewer.Tests/               # テストプロジェクト
│   ├── Services/                  # Services 単体テスト (23件)
│   ├── ViewModels/                # ViewModel テスト (13件)
│   ├── Helpers/FfgDataBuilder.cs  # テスト用ビルダー
│   └── TestData/*.ffg             # Shift-JIS テストデータ (5件)
└── docs/
    ├── SPEC.md                    # 仕様書
    ├── CHANGELOG.md               # 変更履歴
    └── plans/completed/           # 実装計画（完了済み）
```

## ビルド・実行

### 前提条件

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) 以上
- Windows 10/11

### ビルド

```bash
dotnet build FFGViewer/FFGViewer.csproj
```

### 実行

```bash
dotnet run --project FFGViewer/FFGViewer.csproj
```

コマンドライン引数として `.ffg` ファイルパスを渡すと起動時に読み込まれます。

```bash
dotnet run --project FFGViewer/FFGViewer.csproj -- path/to/data.ffg
```

### テスト

```bash
dotnet test FFGViewer.Tests/FFGViewer.Tests.csproj
```

**テスト結果**: 37件 / 37件 合格

## ライセンス

MIT
