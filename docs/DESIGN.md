# ShortcutMaster — 設計メモ（会話確定版）

最終更新: 2026-07-13  
状態: **v1.0.0 リリース**（WPF + .NET 10、`ShortcutMaster.slnx`）。辞書: Windows 104 / Cursor 89 / Excel 103 件。

このファイルは壁打ちの結論です。実装・辞書追加・仕様変更のたびに参照してください。

---

## 1. 一言で何か

**今フォーカスしているアプリ／OSで使えるショートカットを、右下の極小チップから出して、1クリックで前面アプリへ差し込む常駐相棒**です。

辞典アプリではなく、「今の手札」アプリです。

---

## 2. ユーザー体験（確定）

### 平時
- 画面右下に、Cursor のステータスバー右端（`Cursor Tab` 付近）と同程度の **極小1行チップ**
- 文言例: `今使えるshortcut`
- 薄い・邪魔しない・でも気づける

### クリック後
- 上方向に一覧を展開
- 並び: **上が優先度順**、必要なら **下にカテゴリ**
- 各行: キー表示 + **簡潔な日本語説明**（わかりにくいものだけ補足1行）
- 各行に **実行ボタン** → さっきまで触っていた前面アプリ／OSへショートカットを差し込む

### 説明文ルール
- 基本は短く意味が通る（1寄り2）
- わかりにくい子だけ補足（3）

---

## 3. 初回対象アプリ

| 対象 | 方針 |
|------|------|
| Windows | 公開ショートカットを厚く辞書化 |
| Cursor | 同上（VS Code 系ベース + Cursor 固有） |
| Excel | 同上（日本語 UI 前提で意味あるものを厚く） |

将来は他アプリへ拡張可能。最初から全アプリは狙わない。

---

## 4. 頭脳（AI なし）

- 辞書データ
- 前面プロセス検知（どの辞書を出すか）
- ルール優先度（状況加点）
- 使用回数（よく押したものを上げる）

AI（クラウド／ローカル）は **将来の拡張枠**。v1 必須ではない。

---

## 5. 辞書ソース方針

公式ドキュメントを主、二次まとめは照合用として辞書を構築します。矛盾したら **公式優先**。

### Windows（候補3）
1. [Keyboard shortcuts in Windows - Microsoft Support](https://support.microsoft.com/en-us/windows/keyboard-shortcuts-in-windows-dcc61a57-8ff0-cffe-9796-cb9706c75eec)（公式・主）
2. Microsoft Support の関連ページ（File Explorer / ダイアログ等、公式内の追加一覧）
3. 信頼できる二次まとめ（例: 版差確認用。公式と食い違う場合は公式）

### Excel（候補3）
1. [Keyboard shortcuts in Excel - Microsoft Support](https://support.microsoft.com/en-us/office/keyboard-shortcuts-in-excel-1798d9d5-842a-42b8-9c99-9b7213f0040f)（公式・主）
2. [Use the keyboard to work with the ribbon](https://support.microsoft.com/en-us/office/use-the-keyboard-to-work-with-the-ribbon-954cd3f7-2f77-4983-978d-c09b20e31f0e)（Alt 系）
3. 実用まとめ（例: Exceljet 等。公式照合用）

### Cursor（候補3）
1. [Cursor Keyboard shortcuts](https://cursor.com/docs/reference/keyboard-shortcuts)（公式）
2. [VS Code Key Bindings](https://code.visualstudio.com/docs/configure/keybindings)（ベース公式）
3. Default Keyboard Shortcuts（JSON）＋ Cursor 固有差分の照合

ユーザー実機の `keybindings.json` は **v1.1 以降**（v1 は静的キュレーション優先）。  
具体 URL は `docs/sources.md` に固定済みです。

### 辞書についての注意
- 「サイト3つずつ入れれば実行まわりも全部大丈夫」では **ありません**
- 辞書の厚み・正しさは大きく上がります
- アプリ全体の完成度は、下記「技術リスク」の消化率にも依存します

---

## 6. v1.0.0 の成功定義

次の体験が安定して再現できることです。

- 右下チップから一覧を開き、直前のアプリへショートカットを送信できる
- Windows / Cursor / Excel で辞書が切り替わる
- コピー・貼り付けなど日常操作が前面アプリに届く
- 失敗時に短い日本語で理由が分かる

### v1.1 以降
- 真のタスクバー埋め込み（Deskband 級）
- 管理者権限アプリへの注入保証
- ゲーム／アンチチート環境
- Cursor `when` 句の完全評価
- Excel 全リボン KeyTips の100%保証
- ユーザー辞書編集 UI（必要なら後続）

---

## 7. 技術メモ

### 致命リスク
1. クリックでフォーカスが飛ぶ → Ctrl+C 等が事故る  
2. 管理者アプリとの権限差（UIPI）で SendInput が届かない  
3. 修飾キー残留で意図しない合成  
4. 「さっきの前面」誤認  
5. WinUI3 での非アクティブ・常駐オーバーレイが重い  

### 推奨スタック（v1）
- **WPF + .NET 10 + Win32（`WS_EX_NOACTIVATE` 等）** を第一候補
- WinUI3 は見た目優先の v2 候補

### 実行まわりの方針
- チップはヒットを取るが、極力アクティベートしない
- 展開時／実行時に前面 HWND を確認
- 注入前に修飾キー状態をクリーン
- IME 変換中は送信しない（案内を表示）
- 単発ショートカットを先に固める（Alt 連打・二段コードは段階導入）

### 完成度の目安（厳選〜厚め辞書前提）
| 軸 | 目安 |
|----|------|
| コア体験 | 約 70% |
| 辞書（ソース寄せ＋整理） | 約 70%〜（全部寄せは継続メンテ） |
| 実行信頼性 | 約 55〜60%（重点テストで上げる） |
| 総合 | v1.0.0 リリース済み |

辞書ソースを公式中心に厚くすると、**辞書軸はさらに上がります**。  
全体はフォーカス／注入の工学が天井になります。

---

## 8. 差別化（既存との差）

- PowerToys Shortcut Guide / CheatKeys 等は **参照が強い**
- ShortcutMaster の勝ち筋は **常駐極小チップ + 説明つき1クリック実行**
- 辞書の多さだけで公式製品に勝たない。体験で勝つ

---

## 9. 開発方針

- 公開物・README はです・ます調。秘密・個人情報を入れない
- スコープは本ファイルに沿って拡張する
