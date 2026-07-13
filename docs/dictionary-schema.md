# 辞書ファイル仕様（Data/*.json）

最終更新: 2026-07-13

`src/ShortcutMaster/Data/` 配下の JSON 1 ファイルが 1 アプリ分の辞書です。

## ルート構造

```json
{
  "app": "windows",
  "displayName": "Windows",
  "processNames": [],
  "entries": []
}
```

| フィールド | 説明 |
|---|---|
| `app` | 辞書の識別子（英小文字） |
| `displayName` | UI に表示する名前 |
| `processNames` | 対象プロセス名（拡張子なし・大文字小文字無視）。**空配列 = フォールバック辞書**（どのアプリにも一致しない時に使う。windows.json のみ空にする） |
| `entries` | ショートカット定義の配列 |

## entry 構造

```json
{
  "id": "win.clipboard-history",
  "keys": "Win+V",
  "action": "クリップボード履歴を開く",
  "note": "コピーした複数の項目から選んで貼り付けできる",
  "category": "クリップボード・入力",
  "priority": 92,
  "send": ["Win+V"]
}
```

| フィールド | 必須 | 説明 |
|---|---|---|
| `id` | ○ | ファイル内で一意。プレフィックス: `win.` / `cur.` / `xls.` |
| `keys` | ○ | 表示用文字列。同時押しは `Ctrl+Shift+V`、順押しは `Alt, H, B`。送信不可の表示専用エントリは自由表記可（例: `Ctrl+ホイール`） |
| `action` | ○ | 簡潔な日本語説明（体言止め推奨。例: `クリップボード履歴を開く`） |
| `note` | — | 補足が必要なときだけ 1 行。省略可 |
| `category` | ○ | 各辞書で定義したカテゴリ名（UI のグループ見出しになる。ファイル内の初出順に表示される） |
| `priority` | ○ | 0〜100 の整数 |
| `send` | — | 送信するキー手順の配列。**省略 = 表示のみ**（実行ボタンなし） |

## send のトークン規則

- 同時押し: `"Ctrl+Shift+V"`（`+` 区切り。先頭側は修飾キーのみ、最後の 1 つがキー）
- 修飾キー: `Ctrl` `Shift` `Alt` `Win` のみ
- 順押し（シーケンス）: 配列の要素を順に送る。例: `["Alt", "H", "B"]`（`Alt` 単体はタップ）
- 2 段コード: `["Ctrl+K", "Ctrl+S"]`

### 使用可能なキー名（これ以外は使用禁止）

```
A-Z  0-9  F1-F24
Esc Tab Enter Space Backspace Delete Insert Home End
PageUp PageDown Up Down Left Right
PrintScreen Pause CapsLock Apps
Comma Period Semicolon Slash Backslash Minus Plus Grave
LeftBracket RightBracket Quote
Ctrl Shift Alt Win
```

## 表示のみ（send 省略）にするもの

- 注入できないもの（`Ctrl+Alt+Del` などのシステム専用操作）
- 長押し・押しっぱなしが前提のもの
- マウス併用のもの（`Ctrl+ホイール` など）
- 日本語配列でキー位置が変わり誤送信の恐れがあるもの（`;` `:` `@` などの記号を含む組み合わせで確信が持てない場合）

## priority の目安

| 帯 | 意味 |
|---|---|
| 95-100 | 毎日使う定番（コピー・貼り付け・保存など） |
| 85-94 | 知ると得する宝（Win+V、Win+Shift+S など） |
| 70-84 | よく使う |
| 50-69 | 便利 |
| 30-49 | 状況次第 |
| 10-29 | ニッチ |

## 文体・公開物ルール

- `action` / `note` は中立・簡潔な日本語（体言止め可）。口語・絵文字・個人情報は禁止
- トグル系（Win+D など）は「もう一度押すと戻る」等を note に書く
- JSON は UTF-8。コメント・末尾カンマなし
