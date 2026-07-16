<div id="top"></div>

<h1 align="center">
  <img src="docs/images/shortcutmaster-icon.png" alt="ShortcutMaster" width="64" height="64"><br>
  ShortcutMaster
</h1>

<p align="center"><strong>Windows 向け常駐ショートカット支援ツール — 使用中のアプリに合わせた一覧表示と 1 クリック実行（クラウド API キー不要）</strong></p>

<p align="center">
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="License"></a>
  <a href="https://github.com/sin99999/ShortcutMaster/releases"><img src="https://img.shields.io/github/v/release/sin99999/ShortcutMaster?label=release" alt="Release"></a>
</p>

<p align="center">
  <a href="https://github.com/sin99999/ShortcutMaster/releases">ダウンロード（Releases）</a> ·
  <a href="docs/DESIGN.md">設計メモ</a> ·
  <a href="docs/dictionary-schema.md">辞書仕様</a>
</p>

## 使用技術一覧

<p>
  <img src="https://img.shields.io/badge/-C%23-239120.svg?logo=csharp&style=for-the-badge&logoColor=white" alt="C#">
  <img src="https://img.shields.io/badge/-.NET_10-512BD4.svg?logo=dotnet&style=for-the-badge" alt=".NET 10">
  <img src="https://img.shields.io/badge/-WPF-512BD4.svg?logo=windows&style=for-the-badge" alt="WPF">
  <img src="https://img.shields.io/badge/-Win32-0078D4.svg?logo=windows&style=for-the-badge" alt="Win32">
  <img src="https://img.shields.io/badge/-xUnit-2D9CDB.svg?style=for-the-badge&logoColor=white" alt="xUnit">
</p>

## 目次

1. [ShortcutMaster とは](#shortcutmaster-とは)
2. [動作環境](#動作環境)
3. [フォルダ構成（配布 ZIP）](#フォルダ構成配布-zip)
4. [起動と終了](#起動と終了)
5. [主な機能](#主な機能)
6. [収録辞書](#収録辞書)
7. [データの保存場所](#データの保存場所)
8. [開発者向け（ビルド・テスト）](#開発者向けビルドテスト)
9. [よくある質問](#よくある質問)
10. [困ったとき](#困ったとき)
11. [ライセンス・ドキュメント](#ライセンスドキュメント)

## ShortcutMaster とは

Windows 向けの常駐型ショートカット支援ツールです。**いま画面の手前で操作しているアプリ**（Windows 本体 / Cursor / Excel など）に合わせて、利用できるキーボードショートカットを画面右下の小さな表示（チップ）から示し、**クリック 1 回でそのアプリへキー操作を送ります**。

会話の記録・キー入力の外部送信・クラウド API は使用しません。処理は **お使いの PC 内だけ** で完結します。

ソースの改変・fork は [LICENSE](LICENSE)（MIT）の範囲で自由です。

<p align="right">(<a href="#top">トップへ</a>)</p>

## 動作環境

| 項目 | 要件 |
|------|------|
| OS | Windows 10 / 11（64bit） |
| ランタイム | [.NET 10 Desktop Runtime（x64）](https://dotnet.microsoft.com/download/dotnet/10.0)（未導入の場合は 1 回インストール） |
| ネットワーク | **不要**（起動・実行・辞書参照はすべてオフライン） |
| ビルド（開発時） | [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)（`global.json` で SDK 10 を指定） |

<p align="right">(<a href="#top">トップへ</a>)</p>

## フォルダ構成（配布 ZIP）

ZIP を展開すると、おおむね次の構成になります。

| パス | 内容 |
|------|------|
| `ShortcutMaster.exe` | アプリ本体 |
| `ShortcutMaster.dll` | 実行に必要なライブラリ |
| `Data\` | ショートカット辞書（`windows.json` / `cursor.json` / `excel.json`） |
| `docs\` | 設計メモ・辞書仕様（リポジトリ版のみ） |

詳しい辞書の書式は [docs/dictionary-schema.md](docs/dictionary-schema.md)、出典は [docs/sources.md](docs/sources.md) をご覧ください。

<p align="right">(<a href="#top">トップへ</a>)</p>

## 起動と終了

### 起動

1. 配布フォルダ内の **`ShortcutMaster.exe`** をダブルクリックします。
2. 画面右下に **「◯◯ のショートカット」** と書かれた小さな表示が出ます（◯◯ は、いま操作中のアプリ名です）。

開発用にビルドした場合:

```powershell
.\scripts\run.ps1
```

### 操作

1. 右下の表示をクリックすると、上方向にショートカット一覧が開きます。
2. 実行したい行をクリックすると、**直前まで操作していたアプリ**へキー操作が送られます。
3. 一覧を閉じるには、**−**・**✕**・右下表示の再クリックのほか、**一覧の外側（他のアプリ上など）をクリック**しても、右下の小さな表示だけに戻ります（起動中は右下に常駐します）。

### 終了

右下表示の右端の **✕**、または通知領域（タスクトレイ）のアイコンを右クリックして **終了** を選びます。表示が見えないときは、トレイの **右下のチップを表示** から復帰できます。終了時は、画面上の表示とトレイアイコンを**先に消してから**プロセスを終了します。

<p align="right">(<a href="#top">トップへ</a>)</p>

## 主な機能

| 機能 | 説明 |
|------|------|
| 右下への常駐表示 | 画面右下に小さな 1 行表示を常駐させます。OCR・画面切り取りの実行中は一時的に隠れ、終了後に戻ります。デスクトップ表示などの直後も、表示が手前に戻るよう再調整します |
| 使用中アプリとの連動 | プロセス名に応じて辞書を自動で切り替えます（Windows / Cursor / Excel） |
| 1 クリック実行 | 一覧で選んだショートカットを、使用中のアプリへキー操作として送ります |
| 入力先を奪わない設計 | 一覧を操作しても、文字入力の対象が元のアプリから移りにくいようにしています。一覧表示中に外側をクリックすると一覧だけ閉じます |
| おすすめ表示 | 優先度と使用回数をもとに、よく使う項目を上位に表示します |
| 表示のみの項目 | システム専用・取り消せない操作・マウスとの併用など、自動実行できない項目は説明のみ表示します |

### 一覧の並び

| 区分 | 説明 |
|------|------|
| **おすすめ** | 優先度と使用回数を加味した上位項目 |
| **カテゴリ** | 基本・移動・編集など、辞書ごとの分類 |
| **Windows 共通** | Cursor / Excel 利用時に、OS 共通のショートカットを一部表示します |

<p align="right">(<a href="#top">トップへ</a>)</p>

## 収録辞書

| 辞書 | 対象 | 件数（目安） |
|------|------|-------------|
| `windows.json` | Windows 本体（一致しないアプリのとき） | 104 |
| `cursor.json` | Cursor エディタ | 89 |
| `excel.json` | Microsoft Excel（日本語 UI 前提） | 103 |

辞書は `src/ShortcutMaster/Data/`（ビルド時に `Data\` へ同梱）にあります。公式ドキュメントを主とし、矛盾する場合は公式を優先して整備しています。

<p align="right">(<a href="#top">トップへ</a>)</p>

## データの保存場所

| ファイル | 内容 |
|----------|------|
| `%APPDATA%\ShortcutMaster\usage.json` | 各ショートカット ID の**使用回数のみ**（キー入力の内容は記録しません） |

**プライバシー:** キー入力の記録・外部送信は行いません。保存するのは「どのショートカットを何回使ったか」という集計のみです。

<p align="right">(<a href="#top">トップへ</a>)</p>

## 開発者向け（ビルド・テスト）

リポジトリ直下で次を実行します。

```powershell
dotnet build ShortcutMaster.slnx -c Release
dotnet test ShortcutMaster.slnx
```

配布用 ZIP の作成（作業ツリーが clean であること、およびテスト成功が前提です）:

```powershell
.\scripts\package-release.ps1 -Configuration Release
```

| プロジェクト | 役割 |
|-------------|------|
| `src/ShortcutMaster` | WPF 常駐画面・Win32 連携 |
| `src/ShortcutMaster.Core` | 辞書モデル・キー解釈・使用回数 |
| `tests/ShortcutMaster.Core.Tests` | 辞書検証・キー解釈・並び替えの単体テスト |

<p align="right">(<a href="#top">トップへ</a>)</p>

## よくある質問

**Q. キー入力はインターネットに送信されますか。**  
A. いいえ。すべてお使いの PC 内で処理します。外部への通信は行いません。

**Q. コピー（Ctrl+C）を一覧から実行できますか。**  
A. 入力先を奪わない設計のため、使用中のアプリへ届きやすくなっています。ただし日本語入力の変換中や、管理者権限で動いているアプリでは送れない場合があります。

**Q. 管理者権限で動いているアプリには使えますか。**  
A. セキュリティ上、通常権限の ShortcutMaster からは送信できません。その旨の案内が表示されます。

**Q. ゲーム中でも使えますか。**  
A. 入力を独占するアプリは対象外です。通常のデスクトップアプリ向けです。

**Q. 自分用のショートカットを追加できますか。**  
A. 現状は同梱辞書の編集（`Data\*.json`）で対応します。画面からの編集は今後の予定です。

<p align="right">(<a href="#top">トップへ</a>)</p>

## 困ったとき

| 症状 | まず確認すること |
|------|------------------|
| 起動しない | [.NET 10 Desktop Runtime（x64）](https://dotnet.microsoft.com/download/dotnet/10.0) が入っているか |
| 「既に起動しています」と出る | 通知領域（タスクトレイ）に残っていないか。タスクマネージャーで `ShortcutMaster` を終了 |
| 送信されない | 対象アプリが管理者権限か。日本語入力の変換中でないか |
| 一覧が空 | `Data\` フォルダに辞書 JSON があるか。exe と同じフォルダ構成か |
| 右下の表示が見えない | トレイの **右下のチップを表示** を試す。OCR・画面切り取り直後は一時的に隠れます |

### 既知の制限

| 項目 | 内容 |
|------|------|
| 表示のみ | `Ctrl+Alt+Del`、ロック（`Win+L`）、終了（`Alt+F4`）、完全削除（`Shift+Delete`）など |
| 日本語入力 | 変換中は送信しません |
| ディスプレイ | 右下表示は、主に使うモニター右下に固定です |
| 対応アプリ | 初版は Windows / Cursor / Excel の 3 辞書です |

<p align="right">(<a href="#top">トップへ</a>)</p>

## ライセンス・ドキュメント

| ドキュメント | 内容 |
|--------------|------|
| [LICENSE](LICENSE) | 本リポジトリのライセンス（MIT） |
| [docs/DESIGN.md](docs/DESIGN.md) | 設計メモ・技術方針 |
| [docs/dictionary-schema.md](docs/dictionary-schema.md) | 辞書 JSON の書式 |
| [docs/sources.md](docs/sources.md) | 辞書の参照元一覧 |

<p align="right">(<a href="#top">トップへ</a>)</p>
