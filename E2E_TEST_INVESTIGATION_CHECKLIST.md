# RabbitMQ Extension E2E Test Investigation Checklist

このドキュメントは、Azure Functions RabbitMQ Extension の EndToEnd テスト作成のための調査計画およびチェックリストです。

## 調査目標

Kafka Extension の EndToEndTests および LangEndToEndTests を参考に、RabbitMQ Extension 向けの同様のテストを作成する。

---

## 調査チェックリスト

### Phase 1: Kafka Extension テスト構造の調査

- [x] **Step 1.1**: Kafka EndToEndTests の構造を調査
  - テストファイル構成
  - Docker Compose 設定
  - テストフィクスチャーの実装
  - **完了**: KafkaEndToEndTestFixture.cs, KafkaEndToEndTests.cs を確認
  - **結果**: XUnit IAsyncLifetime を使用、AdminClient でトピック作成

- [x] **Step 1.2**: Kafka LangEndToEndTests の構造を調査
  - サポート言語 (Java, Python)
  - 関数アプリの構成
  - Docker イメージのビルド方式
  - **完了**: FunctionApps/java, FunctionApps/python を確認
  - **結果**: Docker コンテナで各言語の関数アプリを起動

- [x] **Step 1.3**: Kafka テストの実行方式を調査
  - テストシェルスクリプト
  - Docker Compose の起動/停止
  - **完了**: start-kafka-test-environment.sh, docker-compose.yml を確認
  - **結果**: docker-compose で Kafka + 関数アプリを起動

### Phase 2: RabbitMQ Extension の現状調査

- [x] **Step 2.1**: RabbitMQ Extension のサポート言語を確認
  - **完了**: README.md を確認
  - **結果**: C#, C# Script, JavaScript, Python, Java をサポート

- [x] **Step 2.2**: 既存の RabbitMQ テストの構造を調査
  - 単体テストの構成
  - サンプルアプリの有無
  - **完了**: WebJobs.Extensions.RabbitMQ.Tests, WebJobs.Extensions.RabbitMQ.Samples を確認
  - **結果**: 単体テストのみ、E2E テストなし

- [x] **Step 2.3**: RabbitMQ Extension の binding 仕様を調査
  - Trigger binding
  - Output binding
  - 接続文字列設定
  - **完了**: RabbitMQSamples.cs を確認
  - **結果**: ConnectionStringSetting で amqp URI を指定

### Phase 3: テスト設計

- [x] **Step 3.1**: Docker Compose 構成の設計
  - RabbitMQ コンテナ設定
  - 各言語の関数アプリコンテナ設定
  - **完了**: E2E_TEST_PROPOSAL.md に設計を記載

- [x] **Step 3.2**: テストケースの設計
  - 基本的なトリガー/出力テスト
  - 各言語固有のテスト
  - **完了**: E2E_TEST_PROPOSAL.md にテストケース一覧を記載

- [x] **Step 3.3**: テスト実行環境の設計
  - CI/CD パイプラインとの統合
  - **完了**: E2E_TEST_PROPOSAL.md に azure-pipelines-e2e.yml を記載

### Phase 4: 提案書作成

- [x] **Step 4.1**: 調査結果のまとめ
  - **完了**: E2E_TEST_PROPOSAL.md セクション 1-2

- [x] **Step 4.2**: 実装提案書の作成
  - **完了**: E2E_TEST_PROPOSAL.md セクション 3-10

---

## 進捗ログ

| 日時 | ステップ | ステータス | メモ |
|------|----------|-----------|------|
| 2026-01-29 | Step 1.1 | 完了 | Kafka E2E テスト構造を調査 |
| 2026-01-29 | Step 1.2 | 完了 | Kafka 言語別テスト構造を調査 |
| 2026-01-29 | Step 1.3 | 完了 | Kafka テスト実行スクリプトを調査 |
| 2026-01-29 | Step 2.1 | 完了 | RabbitMQ サポート言語確認 |
| 2026-01-29 | Step 2.2 | 完了 | RabbitMQ 既存テスト調査 |
| 2026-01-29 | Step 2.3 | 完了 | RabbitMQ binding 仕様調査 |
| 2026-01-29 | Step 3.1 | 完了 | Docker Compose 構成設計 |
| 2026-01-29 | Step 3.2 | 完了 | テストケース設計 |
| 2026-01-29 | Step 3.3 | 完了 | CI/CD パイプライン設計 |
| 2026-01-29 | Step 4.1 | 完了 | 調査結果のまとめ |
| 2026-01-29 | Step 4.2 | 完了 | 実装提案書の作成 |

---

## 参考資料

- Kafka Extension: `azure-functions-kafka-extension/test/`
- RabbitMQ Extension: `azure-functions-rabbitmq-extension/extension/`

## 調査結果サマリー

### Kafka Extension テスト構造

1. **EndToEndTests (C# WebJobs テスト)**
   - `KafkaEndToEndTestFixture.cs`: IAsyncLifetime でトピック作成
   - `KafkaEndToEndTests.cs`: 各種トリガー/出力テスト
   - `kafka-singlenode-compose.yaml`: Kafka + Schema Registry

2. **LangEndToEndTests (多言語テスト)**
   - `Common/`: 共通テストインフラ (KafkaE2EFixture, BaseE2E)
   - `Fixtures/`: 言語・ブローカー別テストフィクスチャー
   - `FunctionApps/`: Java, Python の関数アプリ
   - `server/`: docker-compose.yml で Kafka + 関数アプリ起動

### RabbitMQ Extension 現状

1. **サポート言語**: C#, C# Script, JavaScript, Python, Java
2. **既存テスト**: 単体テストのみ (Moq 使用)
3. **E2E テスト**: なし (新規作成が必要)
4. **Java Library**: `java-library/` に存在

