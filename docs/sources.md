# 辞書ソース一覧（実装時参照）

最終更新: 2026-07-13  
方針: 各カテゴリ公式を主、残りは照合。矛盾したら公式優先。

## Windows
1. https://support.microsoft.com/en-us/windows/keyboard-shortcuts-in-windows-dcc61a57-8ff0-cffe-9796-cb9706c75eec （公式・主）
2. https://support.microsoft.com/en-us/windows/keyboard-shortcuts-in-apps-139014e7-177b-d1f3-eb2e-7298b2599a34 （公式・アプリ／追加）
3. 二次まとめは実装時に1つ固定（版差の照合用。公式と違う場合は公式）

## Excel
1. https://support.microsoft.com/en-us/office/keyboard-shortcuts-in-excel-1798d9d5-842a-42b8-9c99-9b7213f0040f （公式・主）
2. https://support.microsoft.com/en-us/office/use-the-keyboard-to-work-with-the-ribbon-954cd3f7-2f77-4983-978d-c09b20e31f0e （公式・Alt／リボン）
3. https://exceljet.net/shortcuts （実用まとめ・照合用）

## Cursor
1. https://cursor.com/docs/reference/keyboard-shortcuts （Cursor公式）
2. https://code.visualstudio.com/docs/configure/keybindings （VS Code ベース公式）
3. Cursor / VS Code の Default Keyboard Shortcuts（JSON）＋必要なら二次比較記事で Cursor 固有差分を確認

## メモ
- 日本語 UI（特に Excel Alt 系）はロケール差に注意
- ユーザー独自 `keybindings.json` の自動読取は v1.1 以降
