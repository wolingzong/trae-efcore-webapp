# テストフロー時系列図 (Test Sequence Diagrams)

## 1. CI/CD パイプライン全体フロー

```mermaid
sequenceDiagram
    participant Dev as 開発者
    participant GH as GitHub
    participant GA as GitHub Actions
    participant DB as SQL Server
    participant App as Web App
    participant Test as Test Runner
    participant Report as Report Generator

    Dev->>GH: git push origin main
    GH->>GA: Trigger CI/CD Workflow
    
    Note over GA: Build & Test Stage
    GA->>GA: Setup .NET 9.0
    GA->>GA: Restore NuGet packages
    GA->>GA: Build application
    
    GA->>DB: Start SQL Server container
    DB-->>GA: Container ready
    
    GA->>GA: Run unit tests
    GA->>App: Start application
    App-->>GA: Application ready (port 5000)
    
    GA->>Test: Run integration tests
    Test->>App: HTTP GET /
    App-->>Test: 200 OK
    Test->>App: HTTP GET /products
    App-->>Test: 200 OK + HTML
    
    Test->>Report: Generate screenshot
    Report->>App: Capture browser screenshot
    App-->>Report: Screenshot PNG
    
    Test->>Report: Generate test reports
    Report-->>Test: PDF + Excel + CSV
    
    Test-->>GA: Test results + Reports
    GA->>GH: Upload artifacts
    
    Note over GA: Security & Deploy Stage
    GA->>GA: Security scan
    GA->>GA: Build Docker image
    GA->>GH: Push Docker image
    
    GA-->>Dev: Notification (Success/Failure)
```

## 2. 統合テスト詳細フロー

```mermaid
sequenceDiagram
    participant Test as Test Runner
    participant HTTP as HTTP Client
    participant App as Web Application
    participant DB as Database
    participant Browser as Puppeteer Browser
    participant Report as Report Generator
    participant File as File System

    Note over Test: テスト開始
    Test->>HTTP: Create HTTP client
    HTTP->>App: GET /
    App->>DB: Check connection
    DB-->>App: Connection OK
    App-->>HTTP: 200 OK
    HTTP-->>Test: Home page response
    
    Test->>HTTP: GET /products
    App->>DB: SELECT * FROM Products
    DB-->>App: Product data
    App-->>HTTP: 200 OK + Product HTML
    HTTP-->>Test: Products page response
    
    Test->>Test: Assert "Products List" header
    
    Note over Test: スクリーンショット生成
    Test->>Browser: Launch Puppeteer
    Browser->>Browser: Download Chromium (if needed)
    Browser->>App: Navigate to /products
    App-->>Browser: Rendered page
    Browser->>Browser: Take screenshot
    Browser-->>Test: Screenshot PNG data
    Test->>File: Save screenshot file
    
    Note over Test: レポート生成
    Test->>Report: Generate PDF report
    Report->>File: Read feature file
    Report->>File: Create PDF with test results
    Report-->>Test: PDF file path
    
    Test->>Report: Generate Excel report
    Report->>File: Create Excel workbook
    Report->>Report: Add test results sheet
    Report->>Report: Add screenshot sheet
    Report->>File: Embed screenshot image
    Report->>Report: Add detail log sheet
    Report-->>Test: Excel file path
    
    Test->>Report: Generate CSV report
    Report->>File: Create CSV with test data
    Report-->>Test: CSV file path
    
    Test-->>Test: Test completed successfully
```

## 3. エラーハンドリングフロー

```mermaid
sequenceDiagram
    participant Test as Test Runner
    participant App as Application
    participant DB as Database
    participant Browser as Browser
    participant Report as Report Generator
    participant Log as Logger

    Note over Test: エラーシナリオ
    Test->>App: Start application
    App->>DB: Connect to database
    DB-->>App: Connection failed
    App-->>Test: Startup error
    
    Test->>Log: Log database error
    Test->>Test: Switch to SQLite fallback
    Test->>App: Restart with SQLite
    App-->>Test: Application ready
    
    Test->>Browser: Take screenshot
    Browser-->>Test: Browser launch failed
    Test->>Log: Log browser error
    Test->>Report: Generate fallback screenshot
    Report->>Report: Create HTML content image
    Report-->>Test: Fallback screenshot
    
    Test->>Report: Generate error report
    Report->>Report: Include error details
    Report->>Report: Add troubleshooting info
    Report-->>Test: Error report generated
    
    Test->>Log: Log test completion with warnings
    Test-->>Test: Test completed with fallbacks
```

## 4. レポート生成詳細フロー

```mermaid
sequenceDiagram
    participant Test as Test Runner
    participant Excel as Excel Generator
    participant PDF as PDF Generator
    participant CSV as CSV Generator
    participant File as File System
    participant Feature as Feature File

    Note over Test: レポート生成開始
    Test->>Feature: Read feature file
    Feature-->>Test: BDD scenarios
    
    parallel
        Test->>PDF: Generate PDF report
        PDF->>PDF: Create document
        PDF->>PDF: Add test results
        PDF->>PDF: Add execution details
        PDF->>File: Save PDF file
        PDF-->>Test: PDF completed
    and
        Test->>Excel: Generate Excel report
        Excel->>Excel: Create workbook
        Excel->>Excel: Add "テスト結果" sheet
        Excel->>Excel: Format headers and data
        Excel->>Excel: Add "スクリーンショット" sheet
        Excel->>File: Embed screenshot image
        Excel->>Excel: Add "詳細ログ" sheet
        Excel->>Excel: Create hyperlinks between sheets
        Excel->>File: Save Excel file
        Excel-->>Test: Excel completed
    and
        Test->>CSV: Generate CSV report
        CSV->>CSV: Parse feature scenarios
        CSV->>CSV: Format test results
        CSV->>CSV: Add statistics
        CSV->>File: Save CSV file
        CSV-->>Test: CSV completed
    end
    
    Test->>File: Create test results directory
    Test->>File: Copy all report files
    Test-->>Test: All reports generated
```

## 5. Docker デプロイメントフロー

```mermaid
sequenceDiagram
    participant Dev as Developer
    participant Docker as Docker Engine
    participant Registry as Docker Registry
    participant Deploy as Deployment Script
    participant App as Application Container
    participant DB as SQL Server Container

    Dev->>Deploy: ./scripts/deploy.sh
    Deploy->>Docker: docker build -t efcore-webapp
    Docker->>Docker: Multi-stage build
    Docker->>Docker: Install Puppeteer dependencies
    Docker-->>Deploy: Image built successfully
    
    Deploy->>Docker: docker-compose up -d sqlserver
    Docker->>DB: Start SQL Server container
    DB->>DB: Initialize database
    DB-->>Docker: Container healthy
    
    Deploy->>Docker: docker run efcore-webapp
    Docker->>App: Start application container
    App->>DB: Connect to database
    DB-->>App: Connection established
    App->>App: Run EnsureCreated()
    App->>App: Seed initial data
    App-->>Docker: Application ready
    
    Deploy->>Deploy: Health check
    Deploy->>App: curl http://localhost:8080
    App-->>Deploy: 200 OK
    
    Deploy->>Docker: docker-compose run test-runner
    Docker->>Docker: Start test container
    Docker->>App: Run integration tests
    App-->>Docker: Test results
    Docker-->>Deploy: Tests passed
    
    Deploy-->>Dev: Deployment successful
```

## フロー説明

### 主要なフェーズ
1. **準備フェーズ**: 環境セットアップ、依存関係解決
2. **ビルドフェーズ**: アプリケーションコンパイル
3. **テストフェーズ**: 単体・統合テスト実行
4. **レポートフェーズ**: 多形式レポート生成
5. **デプロイフェーズ**: コンテナ化とデプロイ

### 並列処理
- レポート生成は並列実行で効率化
- Docker ビルドとテスト実行の最適化
- 複数形式のレポート同時生成

### エラー回復
- データベース接続失敗時のSQLite切替
- ブラウザ失敗時のフォールバック画像
- ネットワークエラー時のリトライ機構