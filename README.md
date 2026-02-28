# FFGViewer

株式会社大林組開発の「FINAL」で出力される `.ffg` ファイルの内容を表示します。

履歴ファイルに記述されている解析モデルの荷重−変位関係（`.ffg` 形式）を可視化する WPF デスクトップアプリケーションです。

## 機能

| 機能 | 説明 |
|------|------|
| **ファイル読み込み** | `.ffg` ファイルをメニュー・ドラッグ＆ドロップで複数同時読み込み |
| **グラフ表示** | シリーズごとに色分けされた折れ線グラフ（ピーク点マーカー付き）、凡例表示、パン・ズーム対応 |
| **ピーク色区別** | ピーク点はシリーズ線色と補色関係の専用カラーで表示（6シリーズまで重複なし） |
| **ピークデータ表示** | 各シリーズの最大/最小荷重・変位をテーブルに色分け表示 |
| **クリップボードコピー** | ピークデータをタブ区切りでクリップボードへコピー |
| **CSV エクスポート** | 全シリーズを列形式（Disp_[名前], Load_[名前]）で 1 ファイル出力 |
| **Excel エクスポート** | PeakData シート・データシート・XY 散布図グラフシートを含む `.xlsx` 出力 |
| **軸タイトル編集** | X 軸・Y 軸のタイトルをリアルタイム変更 |
| **間引き表示** | 大量データ（15,000点超）をサイクル認識型 LTTB アルゴリズムで間引いて高速描画。チェックボックスで切り替え可能（ピーク計算・エクスポートは元データを使用） |

## ダウンロード・起動方法

### ダウンロード

1. [GitHub Releases](https://github.com/JavaSakluivert/FFGViewer/releases) を開く
2. 最新バージョンの **Assets** から `FFGViewer-vX.X.X-win-x64.zip` をダウンロード
3. ZIP を任意のフォルダに展開する
4. `FFGViewer.exe` を実行する

> **動作環境**: Windows 10 / Windows 11 専用です。macOS・Linux では動作しません。
>
> **注意**: 本アプリはコード署名なし（自己ビルド）のため、.NET ランタイムのインストールは不要ですが、
> Windows SmartScreen の警告が表示されることがあります。

---

### Windows SmartScreen の警告が出た場合

コード署名が行われていない実行ファイルを初めて起動する際、以下の画面が表示される場合があります。

```
┌──────────────────────────────────────────────┐
│  Windows によって PC が保護されました          │
│                                              │
│  Microsoft Defender SmartScreen は、         │
│  認識されていないアプリの起動を停止しました。  │
│  このアプリを実行すると、PC に問題が          │
│  起きる可能性があります。                     │
│                                              │
│  アプリ: FFGViewer.exe                       │
│  発行元: 不明                                 │
│                                              │
│  [実行しない]          [詳細情報 ▼]          │
└──────────────────────────────────────────────┘
```

**手順:**

1. **「詳細情報」** をクリックする
   ↓ 画面が展開され、アプリ名と発行元が表示される

   ```
   ┌──────────────────────────────────────────────┐
   │  Windows によって PC が保護されました          │
   │                                              │
   │  アプリ: FFGViewer.exe                       │
   │  発行元: 不明な発行元                         │
   │                                              │
   │  [実行しない]              [実行]            │
   └──────────────────────────────────────────────┘
   ```

2. **「実行」** をクリックする
   → アプリが起動します。2回目以降は警告は表示されません。

---

## スクリーンショット

```
┌─────────────────────────────────────────────────────────────────────┐
│ [ファイルを開く] [グラフクリア]  ファイル: path/to/last.ffg              │  ← ツールバー
├────────────────────────────────────────────┬────────────────────────┤
│                                            │ ピークデータ             │
│           CartesianChart                   │ ┌──────┬──┬──────┬────┐ │
│     (荷重−変位 ヒステリシス曲線)              │ │シリーズ│符│荷重  │変位 │ │
│    ← ドラッグでパン、ホイールでズーム →       │ │S1    │(+│ 20.00│2.00│ │
│                                            │ │S1    │(-│  0.00│0.00│ │
│                                    [凡例]  │ └──────┴──┴──────┴────┘ │
│                                            │ ──────────────────────  │
│                                            │ □ 間引き表示             │
│                                            │ ──────────────────────  │
│                                            │ 軸タイトル               │
│                                            │ X軸: [Disp. (mm)      ] │
│                                            │ Y軸: [Load (kN)       ] │
│                                            │ [ピークデータをコピー]    │
│                                            │ [Excel エクスポート]     │
│                                            │ [CSV エクスポート]       │
├────────────────────────────────────────────┴────────────────────────┤
│ 読み込み完了: S1                                                       │  ← ステータスバー
└─────────────────────────────────────────────────────────────────────┘
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
| **フレームワーク** | WPF (.NET 10.0-windows) |
| **アーキテクチャ** | MVVM |
| **グラフ描画** | [LiveChartsCore.SkiaSharpView.WPF](https://livecharts.dev/) 2.0.0-rc6.1 |
| **MVVM 基盤** | [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) 8.4.0 |
| **リアクティブバインディング** | [ReactiveProperty](https://github.com/runceel/ReactiveProperty) 9.x |
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
│   │   ├── ExcelExportService.cs  # Excel エクスポート（XY散布図付き）
│   │   └── DataDecimator.cs       # サイクル認識型 LTTB 間引きアルゴリズム
│   ├── ViewModels/
│   │   ├── MainViewModel.cs       # メインビューモデル（CommunityToolkit.Mvvm + ReactiveProperty）
│   │   └── PeakDataRow.cs         # ピークデータ行（色付き）
│   ├── Resources/
│   │   ├── Colors.xaml            # カラーパレット（Material Blue）
│   │   ├── Styles/ButtonStyles.xaml
│   │   ├── Styles/DataGridStyles.xaml
│   │   └── Converters/ColorToBrushConverter.cs
│   ├── MainWindow.xaml / .cs
│   └── App.xaml / .cs             # DI 初期化・起動引数処理
├── FFGViewer.Tests/               # テストプロジェクト
│   ├── Services/                  # Services 単体テスト（FfgFile・Csv・Excel・DataDecimator）
│   ├── ViewModels/                # ViewModel テスト
│   ├── Helpers/FfgDataBuilder.cs  # テスト用ビルダー
│   └── TestData/*.ffg             # Shift-JIS テストデータ (5件)
└── docs/
    ├── SPEC.md                    # 仕様書
    ├── CHANGELOG.md               # 変更履歴
    └── plans/completed/           # 実装計画（完了済み）
```

## ビルド・実行

### 前提条件

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
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

**テスト結果**: 55件 / 55件 合格

## ライセンス

MIT
