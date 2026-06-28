# IchiPos

IchiPos は、Windows 11 上で動作するコマンドラインアプリケーションです。

Misskey をメイン投稿先、X をサブ投稿先として、投稿テキストを同時に投稿準備します。

## 機能

- 投稿テキストを取得する（文字列または `.txt` ファイル）
- Misskey に投稿する（画像添付対応）
- X の投稿画面をブラウザで開く
- 画像がある場合、1枚目をクリップボードにコピーする（X への貼り付け用）
- 投稿後に添付画像を削除する（確認あり）

## 動作環境

- OS：Windows 11
- 言語：C#
- フレームワーク：.NET 10
- アプリ形式：コンソールアプリケーション

## 前提条件

.NET 10 ランタイムが必要です。Microsoft の公式サイトから **.NET 10** をインストールしてください。

インストール確認：

```text
dotnet --version
```

`10.x.x` と表示されれば準備完了です。

## ビルド

```text
git clone <このリポジトリのURL>
cd IchiPos
dotnet build -c Release
```

ビルド後の実行ファイルは以下に生成されます。

```text
IchiPos.Core\bin\Release\net10.0-windows\IchiPos.exe
```

実行時は `config/` ディレクトリを `IchiPos.exe` と同じ場所に置いてください。

## 設定ファイル

設定ファイルは YAML 形式で、実行ファイルと同じディレクトリの `./config/config.yaml` に配置します。

テンプレートをコピーして作成してください。

```text
cp config/config.yaml.example config/config.yaml
```

各フィールドの説明は [config/config.yaml.example](config/config.yaml.example) を参照してください。

設定ファイルが存在しない場合、または必須フィールドが未設定の場合はエラーとなります。

### Misskey アクセストークンの取得

1. Misskey インスタンスにログインする
2. 「設定」→「API」を開く
3. 「アクセストークンの発行」から新しいトークンを発行する
4. 必要な権限：**ノートを作成する**、**ドライブを操作する**
5. 発行されたトークンを `config.yaml` の `access_token` に設定する

## 実行形式

```text
IchiPos.exe <content> [--image-path <folder>]
```

| 引数 | 必須 | 内容 |
|---|---|---|
| `<content>` | 必須 | 投稿テキスト、または `.txt` ファイルパス |
| `--image-path <folder>` | 任意 | 添付画像を格納したフォルダパス |

### 実行例

```text
# テキストをそのまま投稿
IchiPos.exe "今日のランチ"

# テキストファイルから投稿
IchiPos.exe post.txt

# 画像フォルダを指定して投稿
IchiPos.exe post.txt --image-path C:\Pictures\today
```

### 画像付き投稿について

`--image-path` を指定すると以下の動作になります。

- **Misskey**：フォルダ内の画像をすべて添付して投稿します。
- **X**：X の Intent URL では画像を渡せないため、X 投稿画面を開いた後、  
  **1枚目の画像を自動的にクリップボードにコピー**します。  
  X 下書き画面で **Ctrl+V** を押すと画像を貼り付けられます。

> 画像が複数枚ある場合、2枚目以降は手動で X に添付してください。

### 投稿後の画像削除

投稿が完了すると、添付画像がある場合にユーザーへ確認したうえで画像を削除します。

```text
画像を削除してよいですか？（N枚）(y/n):
```

| 入力 | 動作 |
|---|---|
| `y` または `Y` | フォルダ内の添付画像をすべて削除する |
| それ以外 | 削除しない |

## ライセンス

[MIT License](LICENSE)
