# RabbitMQ Extension E2E Test Implementation Checklist

このチェックリストは Testcontainers 方式による E2E テスト実装の進捗を追跡します。

---

## Phase 1: EndToEndTests (C#) - 基盤構築

### 1.1 プロジェクトセットアップ
- [x] E2Eテストプロジェクト作成 (`WebJobs.Extensions.RabbitMQ.EndToEndTests`)
- [x] 必要な NuGet パッケージ追加
  - Testcontainers.RabbitMq
  - xunit
  - xunit.runner.visualstudio
  - Microsoft.NET.Test.Sdk
  - RabbitMQ.Client

### 1.2 テストインフラ実装
- [x] RabbitMQEndToEndTestFixture 実装 (Testcontainers 使用)
- [x] テストコレクション定義

### 1.3 テスト関数実装
- [x] TriggerFunctions.cs (トリガーバインディングテスト用)
- [x] OutputFunctions.cs (出力バインディングテスト用)

### 1.4 テストケース実装
- [x] StringValue_SingleTrigger テスト
- [x] ByteArray_Trigger テスト
- [x] POCO_Trigger テスト
- [x] BasicDeliverEventArgs_Trigger テスト
- [x] Trigger_With_Output_Binding テスト
- [x] Multiple_Messages_ProcessedInOrder テスト

---

## Phase 2: ソリューション統合

- [x] ソリューションファイルにプロジェクト追加
- [x] ビルド確認
- [ ] テスト実行確認 (Docker Desktop 必要)

---

## 完了状況

| フェーズ | ステータス | 完了日 |
|---------|-----------|--------|
| Phase 1.1 | ✅ 完了 | 2026-01-29 |
| Phase 1.2 | ✅ 完了 | 2026-01-29 |
| Phase 1.3 | ✅ 完了 | 2026-01-29 |
| Phase 1.4 | ✅ 完了 | 2026-01-29 |
| Phase 2 | 🔄 一部完了 | 2026-01-29 |

---

## 実装ログ

### 2026-01-29
- ブランチ `tsushi/e2etesting` を `dev` から作成
- チェックリストファイル作成
- E2Eテストプロジェクト作成 (`WebJobs.Extensions.RabbitMQ.EndToEndTests.csproj`)
- NuGet パッケージ追加 (Testcontainers.RabbitMq, xunit, etc.)
- RabbitMQEndToEndTestFixture 実装 (Testcontainers による RabbitMQ コンテナ管理)
- TriggerFunctions.cs 実装 (String, ByteArray, POCO, BasicDeliverEventArgs)
- OutputFunctions.cs 実装 (Trigger + Output binding)
- RabbitMQEndToEndTests.cs 実装 (6つのテストケース)
- ソリューションファイルにプロジェクト追加
- ビルド成功確認

---

## 作成されたファイル

```
extension/WebJobs.Extensions.RabbitMQ.EndToEndTests/
├── WebJobs.Extensions.RabbitMQ.EndToEndTests.csproj
├── RabbitMQEndToEndTestFixture.cs
├── TriggerFunctions.cs
├── OutputFunctions.cs
└── RabbitMQEndToEndTests.cs
```

---

## テスト実行方法

```powershell
# Docker Desktop が起動していることを確認
cd extension
dotnet test WebJobs.Extensions.RabbitMQ.EndToEndTests
```

---

## 次のステップ (Phase 3以降)

- [ ] LangEndToEndTests 共通基盤作成
- [ ] 各言語 (Java, JavaScript, Python) の関数アプリ作成
- [ ] CI/CD パイプライン統合
