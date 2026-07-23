<!--
DEVELOPMENT.md の「実装前の確認」「作業中に見つけたことの扱い」「実装後の確認」に沿って作成してください。
不要な節は削除して構いません。

CLI で PR を作成する場合、`gh pr create --body` はこのテンプレートを迂回します。
その場合もこのファイルを読み、下記の項目を本文に反映してください（issue #50）。
-->

## 概要

<!-- 何を、なぜ変更したか -->

## 関連Issue

<!-- Closes #000 -->

## 設計判断

<!--
複数の選択肢がありえた箇所と、選んだ理由。

未解決の判断・方針確認が必要な事項や、作業中に気づいた別の問題は、本文やコメントに書かず
独立した Issue として起票してください（ルールと理由は DEVELOPMENT.md「作業中に見つけたことの扱い」）。
未解決の判断がある場合、その Issue が解決するまで本 PR をマージしないでください。
-->

## ドキュメントの更新

変更後の挙動と一致しているドキュメントにチェックを入れてください。**`fix:` / `refactor:` でも対象です。**

<!--
判断基準（何が仕様変更か・更新が不要なケース・各ドキュメントの守備範囲）は複製しません。
正典は DEVELOPMENT.md です。以下を参照してください。
- 実装後の確認: https://github.com/momoandbanana22/IchiPos/blob/main/DEVELOPMENT.md#実装後の確認
- 情報の正典（どこに何を書くか）: https://github.com/momoandbanana22/IchiPos/blob/main/DEVELOPMENT.md#情報の正典どこに何を書くか
- ドキュメント更新が不要なケース: https://github.com/momoandbanana22/IchiPos/blob/main/DEVELOPMENT.md#ドキュメント更新が不要なケース
-->

- [ ] `doc/01_IchiPos_基本仕様書.md`
- [ ] `doc/02_IchiPos_機能設計書.md`
- [ ] `doc/03_IchiPos_構造設計書.md`
- [ ] `doc/04_IchiPos_GUI仕様書.md`
- [ ] **`README.md`**（忘れやすい）
- [ ] **`config/config.yaml.example`**（忘れやすい）

いずれかが不要な場合は、その理由をここに書いてください。

## テスト

<!--
TDDで追加・変更したテストの内容。
PR時のテストはCI（.github/workflows/ci.yml）で自動実行されますが、
手元でも確認したうえで件数を記載してください。
-->

- [ ] `dotnet test` が全件パスすることを確認した（件数: ）

## 動作確認

<!-- 実際にアプリを起動して確認した内容。GUIの変更ならスクリーンショットがあると望ましい -->
