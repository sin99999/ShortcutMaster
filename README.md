<div id="top"></div>

<h1 align="center">ShortcutMaster</h1>

<p align="center"><strong>Windows 向け常駐ショートカット相棒 — 前面アプリに合わせた一覧表示と 1 クリック実行（クラウド API キー不要）</strong></p>

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

<p style="display: inline">
  <img src="https://img.shields.io/badge/-C%23-239120.svg?logo=csharp&style=for-the-badge&logoColor=white">
  <img src="https://img.shields.io/badge/-.NET_10-512BD4.svg?logo=dotnet&style=for-the-badge">
  <img src="https://img.shields.io/badge/-WPF-512BD4.svg?logo=windows&style=for-the-badge">
  <img src="https://img.shields.io/badge/-Win32-0078D4.svg?logo=windows&style=for-the-badge">
  <img src="https://img.shields.io/badge/-xUnit-2D9CDB.svg?logo=xunit&style=for-the-badge&logoColor=white">
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

Windows 向けの常駐型ショートカット支援ツールです。**いま前面で使っているアプリ**（Windows 本体 / Cursor / Excel など）に合わせて、利用可能なキーボードショートカットを画面右下の小さなチップから表示し、**クリック 1 回で直前のアプリへ送信**します。

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

くわしい辞書の書式は [docs/dictionary-schema.md](docs/dictionary-schema.md)、出典は [docs/sources.md](docs/sources.md) をご覧ください。

<p align="right">(<a href="#top">トップへ</a>)</p>

## 起動と終了

### 起動

1. 配布フォルダ内の **`ShortcutMaster.exe`** をダブルクリックします。
2. 画面右下に **「◯◯ のショートカット」** チップが表示されます（◯◯ は前面アプリ名）。

開発用にビルドした場合:

```powershell
.\scripts\run.ps1
```

### 操作

1. チップをクリックすると、上方向にショートカット一覧が開きます。
2. 実行したい行をクリックすると、**直前まで操作していたアプリ**へキーが送信されます。
3. 一覧を閉じるには、**−（最小化）**・**✕**・チップの再クリックのいずれかで、右下の 1 行チップだけに戻ります。

### 終了

タスクトレイのアイコンを右クリックし、**終了** を選択します。ウィンドウは先に閉じ、その後バックグラウンドで後片付けが行われます。

<p align="right">(<a href="#top">トップへ</a>)</p>

## 主な機能

| 機能 | 説明 |
|------|------|
| 常駐チップ | 画面右下に極小の 1 行チップとして常駐。作業を邪魔しにくい表示 |
| 前面アプリ連動 | プロセス名に応じて辞書を自動切替（Windows / Cursor / Excel） |
| 1 クリック実行 | 一覧から選んだショートカットを `SendInput` で前面アプリへ送信 |
| フォーカス非奪取 | `WS_EX_NOACTIVATE` 設計により、パネル操作後もコピー・貼り付け等が元アプリに届きやすい |
| おすすめ表示 | 優先度と使用回数に基づき、よく使う項目を上位に表示 |
| 表示のみ項目 | システム専用・破壊的操作・マウス併用など、自動実行できない項目は説明のみ表示 |

### 一覧の並び

| 区分 | 説明 |
|------|------|
| **おすすめ** | 優先度と使用回数を加味した上位項目 |
| **カテゴリ** | 基本・移動・編集など、辞書ごとの分類 |
| **Windows 共通** | Cursor / Excel 利用時に、OS 共通のショートカットを一部表示 |

<p align="right">(<a href="#top">トップへ</a>)</p>

## 収録辞書

| 辞書 | 対象 | 件数（目安） |
|------|------|-------------|
| `windows.json` | Windows 本体（フォールバック） | 104 |
| `cursor.json` | Cursor エディタ | 89 |
| `excel.json` | Microsoft Excel（日本語 UI 前提） | 103 |

辞書は `src/ShortcutMaster/Data/`（ビルド時に `Data\` へ同梱）にあります。公式ドキュメントを主とし、矛盾する場合は公式を優先して整備しています。

<p align="right">(<a href="#top">トップへ</a>)</p>

## データの保存場所

| ファイル | 内容 |
|----------|------|
| `%APPDATA%\ShortcutMaster\usage.json` | 各ショートカット ID の**使用回数のみ**（キー入力内容は記録しません） |

**プライバシー:** キー入力の記録・外部送信は行いません。保存されるのは「どのショートカットを何回使ったか」という集計のみです。

<p align="right">(<a href="#top">トップへ</a>)</p>

## 開発者向け（ビルド・テスト）

リポジトリ直下で次を実行します。

```powershell
dotnet build ShortcutMaster.slnx -c Release
dotnet test ShortcutMaster.slnx
```

配布用 ZIP の作成:

```powershell
.\scripts\package-release.ps1 -Configuration Release
```

| プロジェクト | 役割 |
|-------------|------|
| `src/ShortcutMaster` | WPF 常駐 UI・Win32 連携 |
| `src/ShortcutMaster.Core` | 辞書モデル・パーサ・使用回数 |
| `tests/ShortcutMaster.Core.Tests` | 辞書検証・パーサ・スコアリングの単体テスト |

<p align="right">(<a href="#top">トップへ</a>)</p>

## よくある質問

**Q. キー入力はインターネットに送信されますか。**  
A. いいえ。すべてローカルで処理します。外部への通信は行いません。

**Q. コピー（Ctrl+C）をパネルから実行できますか。**  
A. フォーカスを奪わない設計のため、前面アプリに届きやすくなっています。ただし IME 変換中や管理者権限アプリでは送信できない場合があります。

**Q. 管理者権限で動いているアプリには使えますか。**  
A. セキュリティ上、非昇格プロセスからは送信できません。案内トーストが表示されます。

**Q. ゲーム中でも使えますか。**  
A. 入力を専有するアプリは対象外です。通常のデスクトップアプリ向けです。

**Q. 自分用のショートカットを追加できますか。**  
A. v1.0.0 では同梱辞書の編集（`Data\*.json`）で対応します。GUI による編集は今後の予定です。

<p align="right">(<a href="#top">トップへ</a>)</p>

## 困ったとき

| 症状 | まず確認すること |
|------|------------------|
| 起動しない | [.NET 10 Desktop Runtime（x64）](https://dotnet.microsoft.com/download/dotnet/10.0) が入っているか |
| 「既に起動しています」と出る | タスクトレイに残っていないか。タスクマネージャーで `ShortcutMaster` を終了 |
| 送信されない | 対象アプリが管理者権限か。IME で変換中でないか |
| 一覧が空 | `Data\` フォルダに辞書 JSON があるか。exe と同じフォルダ構成か |

### 既知の制限（v1.0.0）

| 項目 | 内容 |
|------|------|
| 表示のみ | `Ctrl+Alt+Del`、ロック（`Win+L`）、終了（`Alt+F4`）、完全削除（`Shift+Delete`）など |
| IME | 日本語入力の変換中は送信しません |
| モニタ | チップはプライマリモニタ右下に固定 |
| マルチアプリ | 初版は Windows / Cursor / Excel の 3 辞書 |

<p align="right">(<a href="#top">トップへ</a>)</p>

## ライセンス・ドキュメント

| ドキュメント | 内容 |
|--------------|------|
| [LICENSE](LICENSE) | 本リポジトリのライセンス（MIT） |
| [docs/DESIGN.md](docs/DESIGN.md) | 設計メモ・技術方針 |
| [docs/dictionary-schema.md](docs/dictionary-schema.md) | 辞書 JSON の書式 |
| [docs/sources.md](docs/sources.md) | 辞書の参照元一覧 |

<p align="right">(<a href="#top">トップへ</a>)</p>
