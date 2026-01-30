# RabbitMQ Extension E2E Test Implementation Proposal

このドキュメントは、Azure Functions RabbitMQ Extension の EndToEnd テスト実装に関する調査結果と提案をまとめたものです。

---

## 1. 調査結果サマリー

### 1.1 Kafka Extension のテスト構造

#### EndToEndTests (C# WebJobs テスト)
```
test/Microsoft.Azure.WebJobs.Extensions.Kafka.EndToEndTests/
├── kafka-singlenode-compose.yaml      # Kafka + Schema Registry の Docker Compose
├── KafkaEndToEndTestFixture.cs        # IAsyncLifetime でトピック作成/削除
├── KafkaEndToEndTests.cs              # XUnit テストケース
├── KafkaOutputFunctions.cs            # 出力バインディングテスト用関数
├── TriggerFunctions.cs                # トリガーバインディングテスト用関数
├── start-kafka-test-environment.sh   # テスト環境起動スクリプト
└── stop-kafka-test-environment.sh    # テスト環境停止スクリプト
```

**主な特徴:**
- XUnit + IAsyncLifetime パターンでテスト環境管理
- Docker Compose で Kafka クラスタを起動
- Confluent Kafka AdminClient でトピック作成
- Azure WebJobs ホストを in-process で起動

#### LangEndToEndTests (多言語テスト)
```
test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/
├── Common/
│   ├── KafkaE2EFixture.cs       # 言語・ブローカー別フィクスチャー基底クラス
│   ├── BaseE2E.cs               # テスト基底クラス
│   ├── Constants.cs             # ポート、イメージ名などの定数
│   └── ...
├── Fixtures/
│   ├── JavaConfluentE2EFixture.cs
│   └── PythonConfluentE2EFixture.cs
├── FunctionApps/
│   ├── java/
│   │   └── Confluent/
│   │       ├── Dockerfile
│   │       ├── pom.xml
│   │       └── src/
│   └── python/
│       └── Confluent/
│           ├── Dockerfile
│           ├── requirements.txt
│           └── SingleKafkaTriggerQueueOutput/
├── server/
│   └── docker-compose.yml       # Kafka + 関数アプリコンテナ
└── Tests/
    ├── JavaConfluentAppTest.cs
    └── PythonConfluentAppTest.cs
```

**主な特徴:**
- Docker コンテナで各言語の関数アプリを起動
- Azure Queue Storage で結果を検証
- HTTP トリガー経由でメッセージを送信
- Kafka トリガーで受信 → Queue に出力

### 1.2 RabbitMQ Extension の現状

**サポート言語:** C#, C# Script, JavaScript, Python, Java

**既存テスト:**
- `WebJobs.Extensions.RabbitMQ.Tests/`: 単体テストのみ (Moq 使用)
- E2E テスト: なし

**binding 属性:**
- `ConnectionStringSetting`: amqp URI (例: `amqp://user:pass@host:5672/vhost`)
- `QueueName`: RabbitMQ キュー名
- `DisableCertificateValidation`: 証明書検証無効化

---

## 2. 提案するテスト構造

### 2.1 ディレクトリ構造

```
extension/
├── WebJobs.Extensions.RabbitMQ.EndToEndTests/
│   ├── docker-compose.yml
│   ├── RabbitMQEndToEndTestFixture.cs
│   ├── RabbitMQEndToEndTests.cs
│   ├── RabbitMQOutputFunctions.cs
│   ├── TriggerFunctions.cs
│   ├── start-rabbitmq-test-environment.sh
│   ├── start-rabbitmq-test-environment.ps1
│   └── WebJobs.Extensions.RabbitMQ.EndToEndTests.csproj
│
└── WebJobs.Extensions.RabbitMQ.LangEndToEndTests/
    ├── Common/
    │   ├── RabbitMQE2EFixture.cs
    │   ├── BaseE2E.cs
    │   ├── Constants.cs
    │   └── ...
    ├── Fixtures/
    │   ├── CSharpE2EFixture.cs
    │   ├── JavaE2EFixture.cs
    │   ├── JavaScriptE2EFixture.cs
    │   └── PythonE2EFixture.cs
    ├── FunctionApps/
    │   ├── csharp/
    │   │   ├── Dockerfile
    │   │   └── ...
    │   ├── java/
    │   │   ├── Dockerfile
    │   │   ├── pom.xml
    │   │   └── src/
    │   ├── javascript/
    │   │   ├── Dockerfile
    │   │   ├── package.json
    │   │   └── ...
    │   └── python/
    │       ├── Dockerfile
    │       ├── requirements.txt
    │       └── ...
    ├── server/
    │   └── docker-compose.yml
    └── Tests/
        ├── CSharpAppTest.cs
        ├── JavaAppTest.cs
        ├── JavaScriptAppTest.cs
        └── PythonAppTest.cs
```

### 2.2 Docker Compose 設計

#### EndToEndTests 用 (rabbitmq-compose.yaml)

```yaml
version: '3.8'
services:
  rabbitmq:
    image: rabbitmq:3-management-alpine
    hostname: rabbitmq
    ports:
      - "5672:5672"    # AMQP
      - "15672:15672"  # Management UI
    environment:
      RABBITMQ_DEFAULT_USER: guest
      RABBITMQ_DEFAULT_PASS: guest
    healthcheck:
      test: rabbitmq-diagnostics -q check_running
      interval: 10s
      timeout: 5s
      retries: 10
```

#### LangEndToEndTests 用 (server/docker-compose.yml)

```yaml
version: '3.8'
services:
  rabbitmq:
    image: rabbitmq:3-management-alpine
    hostname: rabbitmq
    ports:
      - "5672:5672"
      - "15672:15672"
    environment:
      RABBITMQ_DEFAULT_USER: guest
      RABBITMQ_DEFAULT_PASS: guest
    healthcheck:
      test: rabbitmq-diagnostics -q check_running
      interval: 10s
      timeout: 5s
      retries: 10

  csharp-function:
    build:
      context: ../FunctionApps/csharp
      dockerfile: Dockerfile
    depends_on:
      rabbitmq:
        condition: service_healthy
    ports:
      - "7071:7071"
    environment:
      - RabbitMQConnection=amqp://guest:guest@rabbitmq:5672
      - AzureWebJobsStorage=${AzureWebJobsStorage}

  java-function:
    build:
      context: ../FunctionApps/java
      dockerfile: Dockerfile
    depends_on:
      rabbitmq:
        condition: service_healthy
    ports:
      - "7072:7071"
    environment:
      - RabbitMQConnection=amqp://guest:guest@rabbitmq:5672
      - AzureWebJobsStorage=${AzureWebJobsStorage}

  javascript-function:
    build:
      context: ../FunctionApps/javascript
      dockerfile: Dockerfile
    depends_on:
      rabbitmq:
        condition: service_healthy
    ports:
      - "7073:7071"
    environment:
      - RabbitMQConnection=amqp://guest:guest@rabbitmq:5672
      - AzureWebJobsStorage=${AzureWebJobsStorage}

  python-function:
    build:
      context: ../FunctionApps/python
      dockerfile: Dockerfile
    depends_on:
      rabbitmq:
        condition: service_healthy
    ports:
      - "7074:7071"
    environment:
      - RabbitMQConnection=amqp://guest:guest@rabbitmq:5672
      - AzureWebJobsStorage=${AzureWebJobsStorage}
```

---

## 3. テストケース設計

### 3.1 EndToEndTests (C# WebJobs)

| テスト名 | 説明 |
|---------|------|
| `StringValue_SingleTrigger` | 文字列メッセージの送受信 |
| `ByteArray_Trigger` | バイト配列メッセージの送受信 |
| `POCO_Trigger` | POCO オブジェクトの送受信 |
| `BasicDeliverEventArgs_Trigger` | 生のイベントデータアクセス |
| `Trigger_With_Output_Binding` | トリガー + 出力バインディング連携 |
| `Manual_Ack_Test` | 手動確認応答テスト |
| `Message_Headers_Test` | メッセージヘッダーのテスト |

### 3.2 LangEndToEndTests (多言語)

| 言語 | テスト名 | 説明 |
|------|---------|------|
| C# | `CSharp_Single_Event` | 単一メッセージトリガー |
| C# | `CSharp_Output_Binding` | 出力バインディング |
| Java | `Java_Single_Event` | 単一メッセージトリガー |
| Java | `Java_Output_Binding` | 出力バインディング |
| JavaScript | `JavaScript_Single_Event` | 単一メッセージトリガー |
| JavaScript | `JavaScript_Output_Binding` | 出力バインディング |
| Python | `Python_Single_Event` | 単一メッセージトリガー |
| Python | `Python_Output_Binding` | 出力バインディング |

---

## 4. 各言語の関数アプリ設計

### 4.1 C# 関数アプリ

```csharp
// SingleRabbitMQTriggerQueueOutput.cs
public static class RabbitMQFunctions
{
    [FunctionName("SingleRabbitMQTriggerQueueOutput")]
    public static void Run(
        [RabbitMQTrigger("e2e-test-queue", ConnectionStringSetting = "RabbitMQConnection")] string message,
        [Queue("e2e-csharp-output")] out string queueMsg,
        ILogger log)
    {
        log.LogInformation($"Received: {message}");
        queueMsg = message;
    }
}
```

### 4.2 Java 関数アプリ

```java
// Function.java
public class Function {
    @FunctionName("SingleRabbitMQTriggerQueueOutput")
    public void run(
        @RabbitMQTrigger(
            connectionStringSetting = "RabbitMQConnection",
            queueName = "e2e-test-queue"
        ) String message,
        @QueueOutput(
            name = "queueOutput",
            queueName = "e2e-java-output",
            connection = "AzureWebJobsStorage"
        ) OutputBinding<String> output,
        final ExecutionContext context
    ) {
        context.getLogger().info("Received: " + message);
        output.setValue(message);
    }
}
```

### 4.3 JavaScript 関数アプリ

```javascript
// function.json
{
  "bindings": [
    {
      "type": "rabbitMQTrigger",
      "direction": "in",
      "name": "message",
      "queueName": "e2e-test-queue",
      "connectionStringSetting": "RabbitMQConnection"
    },
    {
      "type": "queue",
      "direction": "out",
      "name": "queueOutput",
      "queueName": "e2e-javascript-output",
      "connection": "AzureWebJobsStorage"
    }
  ]
}

// index.js
module.exports = async function (context, message) {
    context.log('Received:', message);
    context.bindings.queueOutput = message;
};
```

### 4.4 Python 関数アプリ

```python
# function.json
{
  "bindings": [
    {
      "type": "rabbitMQTrigger",
      "direction": "in",
      "name": "message",
      "queueName": "e2e-test-queue",
      "connectionStringSetting": "RabbitMQConnection"
    },
    {
      "type": "queue",
      "direction": "out",
      "name": "queueOutput",
      "queueName": "e2e-python-output",
      "connection": "AzureWebJobsStorage"
    }
  ]
}

# __init__.py
import logging
import azure.functions as func

def main(message: func.RabbitMQMessage, queueOutput: func.Out[str]):
    logging.info(f"Received: {message.get_body().decode('utf-8')}")
    queueOutput.set(message.get_body().decode('utf-8'))
```

---

## 5. テスト実行フロー

### 5.1 EndToEndTests

```
1. docker-compose up -d (RabbitMQ 起動)
2. RabbitMQ の起動を待機 (healthcheck)
3. XUnit テスト実行
   a. RabbitMQEndToEndTestFixture.InitializeAsync() でキュー作成
   b. 各テストケース実行
   c. RabbitMQEndToEndTestFixture.DisposeAsync() でクリーンアップ
4. docker-compose down
```

### 5.2 LangEndToEndTests

```
1. docker-compose up --build -d (RabbitMQ + 関数アプリ起動)
2. すべてのサービスの起動を待機
3. XUnit テスト実行
   a. HTTP リクエストで関数アプリを呼び出し
   b. RabbitMQ にメッセージ発行
   c. Azure Queue Storage から結果を検証
4. docker-compose down
```

---

## 6. CI/CD パイプライン統合

### azure-pipelines-e2e.yml

```yaml
trigger:
  branches:
    include:
      - main
      - dev

pool:
  vmImage: 'ubuntu-latest'

variables:
  - name: AzureWebJobsStorage
    value: $(AZURE_WEBJOBS_STORAGE)

stages:
  - stage: EndToEndTests
    displayName: 'Run E2E Tests'
    jobs:
      - job: RunTests
        steps:
          - task: DockerCompose@0
            displayName: 'Start RabbitMQ'
            inputs:
              containerregistrytype: 'Container Registry'
              dockerComposeFile: 'extension/WebJobs.Extensions.RabbitMQ.EndToEndTests/docker-compose.yml'
              action: 'Run services'

          - task: DotNetCoreCLI@2
            displayName: 'Run E2E Tests'
            inputs:
              command: 'test'
              projects: 'extension/WebJobs.Extensions.RabbitMQ.EndToEndTests/*.csproj'
              arguments: '--configuration Release'

          - task: DockerCompose@0
            displayName: 'Stop RabbitMQ'
            inputs:
              containerregistrytype: 'Container Registry'
              dockerComposeFile: 'extension/WebJobs.Extensions.RabbitMQ.EndToEndTests/docker-compose.yml'
              action: 'Run a Docker Compose command'
              dockerComposeCommand: 'down'
```

---

## 7. 代替案の検討

### 7.1 Testcontainers の使用 (推奨代替案)

Docker Compose の代わりに [Testcontainers](https://dotnet.testcontainers.org/) を使用する方法もあります。

**メリット:**
- テストコード内でコンテナを管理
- XUnit と統合が容易
- コンテナの起動/停止が自動化

**デメリット:**
- 追加の NuGet パッケージが必要
- 既存の Kafka テスト構造との一貫性が減少

```csharp
public class RabbitMQEndToEndTestFixture : IAsyncLifetime
{
    private readonly RabbitMqContainer _rabbitMqContainer = new RabbitMqBuilder()
        .WithImage("rabbitmq:3-management-alpine")
        .Build();

    public async Task InitializeAsync()
    {
        await _rabbitMqContainer.StartAsync();
        // キュー作成など
    }

    public async Task DisposeAsync()
    {
        await _rabbitMqContainer.DisposeAsync();
    }
}
```

### 7.2 比較表

| 方式 | Docker Compose | Testcontainers |
|------|---------------|----------------|
| セットアップ | 外部ファイル | コード内 |
| 一貫性 | Kafka と同じ構造 | 異なる |
| 柔軟性 | 高い | 高い |
| CI/CD 統合 | 容易 | 容易 |
| デバッグ容易性 | 中 | 高 |

---

## 8. 実装計画

### Phase 1: EndToEndTests (C#)
- [ ] プロジェクト構造作成
- [ ] Docker Compose ファイル作成
- [ ] RabbitMQEndToEndTestFixture 実装
- [ ] 基本的なトリガー/出力テスト実装
- [ ] CI パイプライン設定

### Phase 2: LangEndToEndTests 共通基盤
- [ ] Common ディレクトリ構造作成
- [ ] RabbitMQE2EFixture 基底クラス実装
- [ ] BaseE2E テストクラス実装
- [ ] 定数/ユーティリティ実装

### Phase 3: 言語別関数アプリ
- [ ] C# 関数アプリ作成
- [ ] Java 関数アプリ作成
- [ ] JavaScript 関数アプリ作成
- [ ] Python 関数アプリ作成

### Phase 4: 言語別テスト
- [ ] C# テスト実装
- [ ] Java テスト実装
- [ ] JavaScript テスト実装
- [ ] Python テスト実装

### Phase 5: 統合とドキュメント
- [ ] CI/CD パイプライン統合
- [ ] ローカル実行ドキュメント作成
- [ ] README 更新

---

## 9. 必要なリソース

### Azure リソース
- Azure Storage Account (Queue Storage 用)

### ローカル開発環境
- Docker Desktop
- .NET 8.0 SDK
- Azure Functions Core Tools
- 各言語のランタイム (Java, Node.js, Python)

---

## 10. 推奨事項

1. **Docker Compose 方式を採用**: Kafka Extension との一貫性を維持
2. **Testcontainers も検討**: 将来的により柔軟なテスト管理が必要な場合
3. **段階的実装**: まず EndToEndTests を実装し、その後 LangEndToEndTests を追加
4. **CI/CD 優先**: 早期にパイプラインに統合してリグレッション検出を可能に

---

## 付録: 参考リンク

- [Kafka Extension E2E Tests](../azure-functions-kafka-extension/test/)
- [RabbitMQ .NET Client](https://www.rabbitmq.com/dotnet-api-guide.html)
- [Azure Functions RabbitMQ Bindings](https://learn.microsoft.com/azure/azure-functions/functions-bindings-rabbitmq)
- [Testcontainers for .NET](https://dotnet.testcontainers.org/)
