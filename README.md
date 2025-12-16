# EF Core Web App - 商品管理システム

[![CI/CD Pipeline](https://github.com/wolingzong/trae-efcore-webapp/actions/workflows/ci-cd.yml/badge.svg)](https://github.com/wolingzong/trae-efcore-webapp/actions/workflows/ci-cd.yml)

ASP.NET Core と Entity Framework Core を使用した商品管理システムです。完全な自動テストとレポート生成機能を備えています。

## 🚀 機能

- **商品管理**: 商品の一覧表示、追加、編集、削除
- **データベース**: SQL Server / SQLite 対応
- **自動テスト**: BDD スタイルのテストケース
- **レポート生成**: PDF、Excel、CSV 形式の自動レポート
- **スクリーンショット**: 真のブラウザスクリーンショット機能
- **CI/CD**: GitHub Actions による自動ビルド・テスト・デプロイ

## 🛠️ 技術スタック

- **Backend**: ASP.NET Core 9.0
- **Database**: Entity Framework Core, SQL Server
- **Testing**: xUnit, PuppeteerSharp, EPPlus
- **CI/CD**: GitHub Actions, Docker
- **Reports**: PDF (SkiaSharp), Excel (EPPlus), CSV

## 📋 前提条件

- .NET 9.0 SDK
- Docker & Docker Compose
- SQL Server (または SQLite)

## 🚀 クイックスタート

### 開発環境セットアップ

```bash
# リポジトリをクローン
git clone https://github.com/wolingzong/trae-efcore-webapp.git
cd trae-efcore-webapp

# 開発環境をセットアップ
./scripts/setup-dev.sh

# アプリケーションを起動
cd efcore-webapp
dotnet run
```

### Docker を使用した起動

```bash
# 全サービスを起動
docker-compose up -d

# アプリケーションにアクセス
open http://localhost:8080
```

## 🧪 テスト実行

### 単体テスト

```bash
cd efcore-webapp.Tests
dotnet test
```

### 統合テスト（アプリケーション起動後）

```bash
# アプリケーションを起動
cd efcore-webapp
dotnet run &

# テストを実行
cd ../efcore-webapp.Tests
dotnet test --configuration Release
```

### Docker でのテスト

```bash
docker-compose run --rm test-runner
```

## 📊 テストレポート

テスト実行後、以下のレポートが生成されます：

- **PDF レポート**: `TestResults/acceptance-report.pdf`
- **Excel レポート**: `TestResults/test-specimen.xlsx`
  - テスト結果シート
  - スクリーンショットシート（実際のブラウザ画面）
  - 詳細ログシート
- **CSV レポート**: `TestResults/test-report.csv`

## 🔄 CI/CD パイプライン

GitHub Actions により以下の自動化が実行されます：

### 1. テストステージ
- ✅ 単体テスト実行
- ✅ 統合テスト実行
- ✅ SQL Server コンテナでのテスト
- ✅ テストレポート生成・アップロード

### 2. セキュリティスキャン
- 🔍 依存関係の脆弱性チェック
- 🔍 古いパッケージの検出

### 3. ビルド・デプロイ
- 🏗️ プロダクションビルド
- 📦 Docker イメージ作成・プッシュ
- 🚀 デプロイメントパッケージ生成

### 4. パフォーマンステスト
- ⚡ 負荷テスト実行
- 📈 パフォーマンス指標収集

## 🐳 Docker デプロイ

### 本番環境デプロイ

```bash
# デプロイスクリプトを実行
./scripts/deploy.sh
```

### 手動デプロイ

```bash
# イメージをビルド
docker build -t efcore-webapp:latest .

# コンテナを起動
docker run -d \
  --name efcore-webapp \
  -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  efcore-webapp:latest
```

## 📁 プロジェクト構造

```
├── .github/workflows/     # GitHub Actions ワークフロー
├── efcore-webapp/         # メインアプリケーション
│   ├── Data/             # データベースコンテキスト
│   ├── Models/           # データモデル
│   └── Program.cs        # アプリケーションエントリポイント
├── efcore-webapp.Tests/   # テストプロジェクト
│   ├── Features/         # BDD テストシナリオ
│   ├── Utils/            # テストユーティリティ
│   └── ProductFeatureTests.cs
├── scripts/              # デプロイ・セットアップスクリプト
├── Dockerfile            # Docker イメージ定義
├── docker-compose.yml    # Docker Compose 設定
└── README.md
```

## 🔧 設定

### データベース接続

**SQL Server** (本番環境):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MyWebAppDb;User ID=sa;Password=YourStrong@Password;TrustServerCertificate=True;"
  }
}
```

**SQLite** (開発環境):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=app.db"
  }
}
```

### 環境変数

- `ASPNETCORE_ENVIRONMENT`: 実行環境 (Development/Production)
- `ASPNETCORE_URLS`: バインドURL
- `PUPPETEER_EXECUTABLE_PATH`: Chrome実行パス (Docker用)

## 🤝 コントリビューション

1. フォークしてください
2. フィーチャーブランチを作成してください (`git checkout -b feature/amazing-feature`)
3. 変更をコミットしてください (`git commit -m 'Add amazing feature'`)
4. ブランチにプッシュしてください (`git push origin feature/amazing-feature`)
5. プルリクエストを作成してください

## 📝 ライセンス

このプロジェクトは MIT ライセンスの下で公開されています。

## 🆘 トラブルシューティング

### よくある問題

**1. SQL Server 接続エラー**
```bash
# SQL Server コンテナの状態を確認
docker-compose logs sqlserver

# 接続をテスト
docker exec -it traework_sqlserver_1 /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P YourStrong@Password
```

**2. テスト失敗**
```bash
# アプリケーションが起動していることを確認
curl http://localhost:5000

# テストログを確認
dotnet test --logger "console;verbosity=detailed"
```

**3. Docker ビルドエラー**
```bash
# Docker キャッシュをクリア
docker system prune -a

# 再ビルド
docker-compose build --no-cache
```

## 📞 サポート

問題や質問がある場合は、GitHub Issues でお知らせください。