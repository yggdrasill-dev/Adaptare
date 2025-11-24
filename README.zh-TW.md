# Adaptare

Adaptare 是一個 .NET 的訊息佇列抽象函式庫，用來簡化跨不同傳輸層（Direct/in-process、NATS、RabbitMQ）之間的訊息送出、接收與處理。

## 概觀

Adaptare 提供一組核心抽象，讓你可以撰寫與註冊訊息處理器與處理流程，而不將業務邏輯鎖定到特定傳輸層：

- `IMessageSender`：發送 / 發佈 / Request 訊息（啟動時會注入 multiplexer）。
- `IMessageReceiver<TSubscriptionSettings>`：針對某個傳輸層訂閱訊息。
- `IMessageExchange`：判定交換器是否支援某 subject（`MatchAsync`）並回傳 transport-specific `IMessageSender`。
- `IMessageHandler<T>` / `IMessageProcessor<T, TR>`：伺服端處理邏輯（handler/processor）。
- `ISubscribeRegistration` / `IMessageQueueBackgroundRegistration`：背景註冊與訂閱生命週期管理。

這種設計可以讓應用程式邏輯與傳輸層解耦，同時保留針對特定傳輸層的優化可能性。

## 快速開始

Clone 並建置此專案：

```pwsh
dotnet restore
dotnet build -c Release
dotnet test -c Release --no-restore
```

專案中有一個 Direct transport 的簡單範例（`samples/Adaptare.Sample.Direct`），示範最小的 in-process 使用流程。

## 開始使用（DI 與啟動）

此函式庫採用 builder pattern 並與 Microsoft DI 相容。
在啟動程式中註冊 MessageQueue：

```csharp
builder.Services
    .AddMessageQueue();
```

接著選擇欲啟用的傳輸層，每種傳輸層會提供擴充方法來註冊其設定與 handler/processor：

Direct transport 範例：

```csharp
builder.Services
    .AddMessageQueue()
    .AddDirectMessageQueue(cfg => cfg
        .AddProcessor<ProcessorType>("subject1")
        .AddHandler<HandlerType>("subject2")
        .AddReplyHandler<HandlerType2>("subject3"))
    .AddDirectGlobPatternExchange("*");
```

NATS 與 RabbitMQ 的註冊方式類似，提供 `Add{Transport}MessageQueue(...)` 的擴充：

```csharp
builder.Services
    .AddMessageQueue()
    .AddNatsMessageQueue(cfg => cfg
        // configure connections, serializers, handlers/processors..)
    .AddRabbitMessageQueue(cfg => cfg
        // configure connections, serializers, handlers/processors..)
```

`AddMessageQueue()` 會註冊一個 `MultiplexerMessageSender` 作為 `IMessageSender`，該 multiplexer 會使用 `MessageExchangeOptions.Exchanges` 的順序，呼叫 `MatchAsync` 選擇第一個符合的 `IMessageExchange`。

## 訊息語意

`IMessageSender` 的 API 代表不同的通訊語意：

- `PublishAsync`：fire-and-forget（單向發佈）。
- `RequestAsync<TMessage, TReply>`：Request/Reply 模式，會等待 `TReply` 回覆；若回覆 header 中含 `MQ_Fail`，使用端會收到 `MessageProcessFailException`。
- `SendAsync`：依傳輸層實作；例如 NATS 可以用於 request 且回傳 raw bytes；部分傳輸層可能會回拋 `NotSupportedException`。

範例用法：

```csharp
var messageSender = serviceProvider.GetRequiredService<IMessageSender>();
await messageSender.PublishAsync("subject1", requestBytes, headers, cancellationToken);

var reply = await messageSender.RequestAsync<byte[], MyReply>("subject1", requestBytes, headers);
```

## Header、追蹤與失敗處理

使用 `MessageHeaderValue` 封裝訊息的 metadata。函式庫提供 `TraceContextPropagator` 來注入與抽取分散式追蹤的 context（例如 Activity）。

常用 header key 定義於 `MessageHeaderValueConsts`：
- `MQ_Ask_Id`、`MQ_Reply_Id`、`MQ_Reply`、`MQ_Fail`。

如果遠端處理器要標示錯誤，應在回覆 header 裡設置 `MQ_Fail`，請求端會收到 `MessageProcessFailException`。

## 訂閱與 Ack 行為

訂閱使用 `ISubscribeRegistration` 實作，transport 會在 background 註冊時建立訂閱。RabbitMQ 的常見情況包括：

- `SubscribeRegistration<TMessage, THandler>`：一般 handler；可選 `AutoAck`（true 或 false）。若 `AutoAck=false`，處理成功後需呼叫 `BasicAck`，失敗時可 `BasicNack` 來重新佇列或觸發重試邏輯。
- `AcknowledgeSubscribeRegistration<TMessage, THandler>`：提供 `IAcknowledgeMessage<T>` 與 `IAcknowledgeMessageHandler<T>` 的進階 Ack 流程（Ack、Nak、進度與中止等）。

背景註冊由 `IMessageQueueBackgroundRegistration` 的實作負責，例如 `NatsBackgroundRegistration` 與 `RabbitMQBackgroundRegistration`。

## 新增傳輸層或 Exchange

如需新增交換器或傳輸層：

1. 實作 `IMessageExchange`：
   - `MatchAsync(subject, header)`：判定何時此 exchange 適用。
   - `GetMessageSenderAsync(subject, IServiceProvider)`：回傳 transport-specific 的 `IMessageSender`。
2. 使用 `MessageQueueConfiguration.AddExchange(exchange)` 或撰寫 `Add{Transport}MessageQueue` 的擴充方法來註冊交換器。

請注意，Multiplexer 會採第一個 match 的 exchange，故 exchange 註冊順序會影響路由；可使用 `MessageQueueConfiguration.PushExchange` 變更優先順序。

## 序列化

每個 transport 都有自己的 serializer registry（例如 `IRabbitMQSerializerRegistry`、`INatsSerializerRegistry`），請使用 `GetSerializer<T>` / `GetDeserializer<T>` 取得型別化的(反)序列化器。

## 測試

- 測試以 xUnit + NSubstitute 為主，測試專案位於 `Adaptare.*.UnitTests`。
- 在單元測試中，使用 `AddFake{Transport}MessageQueue` 來避免啟動 background 註冊或建立真正的網路連線（參考 `AddFakeNatsMessageQueue`、`AddFakeRabbitMessageQueue`）。

## 疑難排解與常見錯誤

- `MessageSenderNotFoundException`：沒有 exchange 符合該 subject；檢查是否已呼叫 `Add*GlobPatternExchange` 或 `MessageQueueConfiguration.AddExchange(...)`。也可檢查 exchange 的註冊順序。
- `MessageProcessFailException`：遠端 handler 在 reply header 設置 `MQ_Fail`；在處理端檢查是否正確設置該 header（例如在 RabbitMQ 的 ack/handler 實作）。
- RabbitMQ 的 ack 問題：若 `AutoAck = false`，必須在成功處理時呼叫 `BasicAck`，否則訊息將被重新送回佇列。
- NATS 連線問題：檢查 `INatsConnectionManager` 與 `registerName` 設定是否一致。

## Contributing 與 程式碼風格

更多 Copilot 指南與貢獻建議請參閱 `.github/copilot-instructions.md`；以下為簡短的程式碼風格重點：

- 命名：類別與公用型別採用 PascalCase，介面以 `I` 為前綴。
- 私有欄位：私有/內部靜態變數以 `_` 開頭；其他私有欄位以 `m_` 開頭（專案慣例）。
- 縮排：使用 Tab，Tab 寬度為 4；每行最大字元數為 100；行尾為 CRLF。

若欲新增傳輸層或功能，請優先新增 `IMessageExchange` 的實作並編寫測試。

---

以下為常見的程式碼片段，保留原本的示範用法：

* Dependency Injection

    ```csharp
    builder.Services
        .AddMessageQueue();
    ```

* 設定 MessageQueue

    ```csharp
    builder.Services
        .AddMessageQueue()
        .AddDirectMessageQueue(config => config
            .AddProcessor<ProcessorType>("subject1")
            .AddHandler<HandlerType>("subject2")
            .AddReplyHandler<HandlerType2>("subject3"))
        .AddDirectGlobPatternExchange("*");
    ```

* 發送訊息

    ```csharp
    var messageSender = serviceProvider.GetRequiredService<IMessageSender>();
 
    var request = new SendMessageType
    {
        ...
    };

    await messageSender.PublishAsync(
        "subject1",
        request.ToByteArray(),
        cancellationToken).ConfigureAwait(false);

    // 或

    var responseData = await messageSender.RequestAsync(
        "subject1",
        request.ToByteArray(),
        cancellationToken).ConfigureAwait(false);

    var response = ReceiveType.Parser.ParseFrom(responseData.Span);

    // 或

    await messageSender.SendAsync(
        "subject1",
        request.ToByteArray(),
        cancellationToken).ConfigureAwait(false);
    ```

* 接收訊息

    ```csharp
    internal class ProcessorType : IMessageProcessor
    {
        public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
        {
            var request = SendMessageType.Parser.ParseFrom(data.Span);

            ...

            return response.ToByteArray();
        }
    }

    internal class HandlerType : IMessageHandler
    {
        public async ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
        {
            var request = SendMessageType.Parser.ParseFrom(data.Span);

            ...
        }
    }
    ```

## License

[MIT](https://choosealicense.com/licenses/mit/)
