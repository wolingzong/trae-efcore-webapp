# テストフロー図 (Test Flow Charts)

## 1. 全体テストフロー

```mermaid
flowchart TD
    A[コード変更] --> B{ブランチ確認}
    B -->|main/develop| C[CI/CD開始]
    B -->|feature/*| D[PR検証]
    
    C --> E[環境セットアップ]
    E --> F[.NET 9.0 セットアップ]
    F --> G[NuGet復元]
    G --> H[アプリビルド]
    
    H --> I[SQL Server起動]
    I --> J{DB接続確認}
    J -->|成功| K[単体テスト実行]
    J -->|失敗| L[SQLite切替]
    L --> K
    
    K --> M{テスト結果}
    M -->|成功| N[アプリ起動]
    M -->|失敗| O[エラーレポート]
    
    N --> P[統合テスト実行]
    P --> Q[スクリーンショット撮影]
    Q --> R[レポート生成]
    
    R --> S[セキュリティスキャン]
    S --> T{全体結果}
    T -->|成功| U[デプロイ実行]
    T -->|失敗| V[失敗通知]
    
    U --> W[成功通知]
    O --> X[アーティファクト保存]
    V --> X
    W --> X
    
    D --> Y[PR検証テスト]
    Y --> Z{PR結果}
    Z -->|成功| AA[マージ可能]
    Z -->|失敗| BB[修正要求]
```

## 2. テスト実行決定フロー

```mermaid
flowchart TD
    A[テスト開始] --> B{アプリケーション状態}
    B -->|起動済み| C[接続テスト]
    B -->|未起動| D[アプリ起動]
    
    D --> E{起動結果}
    E -->|成功| C
    E -->|失敗| F[起動エラー処理]
    F --> G[エラーログ記録]
    G --> H[テスト中断]
    
    C --> I{DB接続状態}
    I -->|接続OK| J[テストケース実行]
    I -->|接続NG| K[DB切替処理]
    
    K --> L{切替結果}
    L -->|成功| J
    L -->|失敗| M[DB接続エラー]
    M --> N[最小限テスト実行]
    
    J --> O[HTTP テスト]
    O --> P[画面表示テスト]
    P --> Q[データ検証テスト]
    
    Q --> R{ブラウザ利用可能}
    R -->|Yes| S[Puppeteer起動]
    R -->|No| T[フォールバック画像]
    
    S --> U{スクリーンショット}
    U -->|成功| V[実画像保存]
    U -->|失敗| T
    T --> W[代替画像生成]
    
    V --> X[レポート生成]
    W --> X
    N --> Y[エラーレポート生成]
    
    X --> Z[テスト完了]
    Y --> Z
```

## 3. レポート生成フロー

```mermaid
flowchart TD
    A[レポート生成開始] --> B[テスト結果収集]
    B --> C[Feature ファイル読込]
    C --> D[実行データ準備]
    
    D --> E{並列レポート生成}
    
    E --> F[PDF生成]
    E --> G[Excel生成]
    E --> H[CSV生成]
    
    F --> I[PDF文書作成]
    I --> J[テスト結果記載]
    J --> K[実行時間記載]
    K --> L[PDF保存]
    
    G --> M[Excelワークブック作成]
    M --> N[テスト結果シート]
    N --> O[スクリーンショットシート]
    O --> P[詳細ログシート]
    P --> Q{画像ファイル存在}
    Q -->|Yes| R[画像埋込]
    Q -->|No| S[画像なし表示]
    R --> T[シート間リンク作成]
    S --> T
    T --> U[Excel保存]
    
    H --> V[CSV形式変換]
    V --> W[統計情報追加]
    W --> X[CSV保存]
    
    L --> Y[レポート完了確認]
    U --> Y
    X --> Y
    
    Y --> Z{全レポート完了}
    Z -->|Yes| AA[アーティファクト作成]
    Z -->|No| BB[エラー処理]
    
    AA --> CC[レポート生成完了]
    BB --> DD[部分レポート保存]
    DD --> CC
```

## 4. エラーハンドリングフロー

```mermaid
flowchart TD
    A[エラー発生] --> B{エラータイプ判定}
    
    B -->|DB接続エラー| C[データベース切替]
    B -->|アプリ起動エラー| D[アプリ再起動]
    B -->|ブラウザエラー| E[フォールバック処理]
    B -->|ネットワークエラー| F[リトライ処理]
    B -->|ファイルI/Oエラー| G[代替パス処理]
    
    C --> H{切替成功}
    H -->|Yes| I[SQLite使用継続]
    H -->|No| J[テスト中断]
    
    D --> K{再起動成功}
    K -->|Yes| L[テスト継続]
    K -->|No| M[致命的エラー]
    
    E --> N[HTML表示画像生成]
    N --> O[警告付きテスト継続]
    
    F --> P{リトライ回数}
    P -->|< 3回| Q[再試行]
    P -->|>= 3回| R[タイムアウト処理]
    Q --> S[待機時間]
    S --> T[再実行]
    
    G --> U[一時ディレクトリ使用]
    U --> V[警告ログ記録]
    
    I --> W[警告レベルログ]
    J --> X[エラーレベルログ]
    L --> W
    M --> Y[致命的エラーログ]
    O --> W
    R --> X
    V --> W
    
    W --> Z[テスト継続]
    X --> AA[テスト部分実行]
    Y --> BB[テスト完全停止]
    
    Z --> CC[レポート生成]
    AA --> DD[エラーレポート生成]
    BB --> EE[失敗通知]
```

## 5. CI/CD パイプライン状態遷移

```mermaid
stateDiagram-v2
    [*] --> Waiting: コード変更なし
    Waiting --> Triggered: Push/PR
    
    Triggered --> Setup: ワークフロー開始
    Setup --> Building: 環境準備完了
    Building --> Testing: ビルド成功
    Building --> Failed: ビルド失敗
    
    Testing --> Reporting: テスト成功
    Testing --> Failed: テスト失敗
    
    Reporting --> Security: レポート生成完了
    Security --> Deploying: セキュリティOK
    Security --> Failed: セキュリティNG
    
    Deploying --> Success: デプロイ成功
    Deploying --> Failed: デプロイ失敗
    
    Success --> Waiting: 完了通知
    Failed --> Waiting: エラー通知
    
    state Testing {
        [*] --> UnitTest
        UnitTest --> IntegrationTest: 単体テスト成功
        IntegrationTest --> E2ETest: 統合テスト成功
        E2ETest --> [*]: E2Eテスト成功
        
        UnitTest --> [*]: 単体テスト失敗
        IntegrationTest --> [*]: 統合テスト失敗
        E2ETest --> [*]: E2Eテスト失敗
    }
    
    state Reporting {
        [*] --> PDFGen
        [*] --> ExcelGen
        [*] --> CSVGen
        
        PDFGen --> [*]
        ExcelGen --> [*]
        CSVGen --> [*]
    }
```

## フロー図の説明

### 主要な判定ポイント
1. **ブランチ判定**: main/develop vs feature branches
2. **アプリケーション状態**: 起動成功/失敗
3. **データベース接続**: SQL Server vs SQLite fallback
4. **ブラウザ可用性**: Puppeteer vs フォールバック
5. **テスト結果**: 成功/失敗/部分成功

### 並列処理ポイント
- レポート生成 (PDF, Excel, CSV)
- セキュリティスキャンとビルド
- 複数テストケースの実行

### フォールバック機構
- SQL Server → SQLite
- Puppeteer → HTML画像生成
- ネットワークエラー → リトライ
- ファイルI/O → 代替パス