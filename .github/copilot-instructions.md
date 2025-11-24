# Copilot 編碼指南 — Adaptare

本文件旨在讓 AI 程式碼助理（如 Copilot 或自動化代理）能夠快速在本專案中產生有用、正確且一致的程式碼。

目的與高階架構
- Adaptare 是一個抽象化訊息傳送、接收與處理的函式庫，支援多種傳輸層（Direct / NATS / RabbitMQ）。
- 專案核心抽象（重要介面）：`IMessageSender`、`IMessageReceiver<T>`、`IMessageExchange`、`IMessageProcessor<T,TR>`、`IMessageHandler<T>`、以及背景註冊 `IMessageQueueBackgroundRegistration`。
- 訊息交換器（Exchange）負責決定要使用哪個 `IMessageSender`：實作 `MatchAsync(subject, header)` 判定是否支援，並在 `GetMessageSenderAsync(...)` 回傳 sender。參考：`Adaptare/MultiplexerMessageSender.cs`（第一個符合的 exchange 會被採用）。

啟動與 DI 模式
- 啟動範例：在 `Program` 或 DI 容器中呼叫 `services.AddMessageQueue()`（回傳 `MessageQueueConfiguration`），之後再 `Add{Transport}MessageQueue(...)`：
  - `.AddDirectMessageQueue(...)`（內部 process, 直接呼叫 handler/processor）
  - `.AddNatsMessageQueue(...)`（NATS 交換器與序列化註冊）
  - `.AddRabbitMessageQueue(...)`（RabbitMQ 交換器、序列化與連線管理）
- `AddMessageQueue()` 會註冊 `IMessageSender` 為 `MultiplexerMessageSender`，並使用 `MessageQueueConfiguration` 中的 `MessageExchangeOptions.Exchanges` 做路由判斷。
- 背景訂閱會由 `IMessageQueueBackgroundRegistration` 註冊到 `MessageQueueBackground`（由 host 執行），參考：`MessageQueueBackground.cs`、`NatsBackgroundRegistration`、`RabbitMQBackgroundRegistration`。

訊息語意（建議用法）
- `PublishAsync`：fire-and-forget，無回覆預期。
- `RequestAsync<TMessage, TReply>`：Request/Reply 模式，會等待 `TReply` 型別回覆；若回覆 header 含 `MQ_Fail`，會拋出 `MessageProcessFailException`。
- `SendAsync`：傳輸層特定的送法（例如 NATS 用於 request 但回 raw bytes），某些實作會丟出 `NotSupportedException`。

Header、追蹤與錯誤宣告
- 使用 `MessageHeaderValue`（`Adaptare/MessageHeaderValue.cs`）在不同傳輸層間攜帶 key/value。
- 追蹤支援：使用 `TraceContextPropagator`（`Inject` 與 `TryExtract`）來在 header 之間傳遞 Activity / TraceContext。
- 常用 header key: `MQ_Ask_Id`、`MQ_Reply_Id`、`MQ_Reply`、`MQ_Fail`（在 `MessageHeaderValueConsts` 中）。
- 若 remote handler 要回應失敗，需在 reply header 設置 `MQ_Fail`，呼叫端會轉成 `MessageProcessFailException`。

訂閱（subscribe）與 ack 行為
- 訂閱註冊物件：實作 `ISubscribeRegistration` 的類別（例如 `SubscribeRegistration<TMessage, THandler>`、`AcknowledgeSubscribeRegistration<TMessage, THandler>`）。
- RabbitMQ 訂閱：可選 `AutoAck`（true 為自動 ack、false 需要在處理成功時呼叫 `BasicAck`）；失敗時會 `BasicNack`，並觸發重試邏輯與 `ExceptionHandler`。
- 若需要進階 ack flow（Ack、Nak、進度回報等），使用 `IAcknowledgeMessage<T>` 配合 `IAcknowledgeMessageHandler<T>` 實作。

擴充性建議（新增 transport / exchange）
- 新增 exchange：實作 `IMessageExchange` 並提供 `MatchAsync` 與 `GetMessageSenderAsync`。
- 註冊方式：使用 `MessageQueueConfiguration.AddExchange(exchange)` 或新增 `Add{Transport}MessageQueue` 擴充方法（參考 `Adaptare.Nats.DependencyInjection.ServiceCollectionExtensions.cs`、`Adaptare.RabbitMQ.DependencyInjection.ServiceCollectitonExtensions.cs`）。
- 測試友善：為 transport 提供 `AddFake{Transport}MessageQueue`（避免建立真實連線與背景工作），參考 `AddFakeNatsMessageQueue` / `AddFakeRabbitMessageQueue`。

序列化
- 每個 transport 都有 serializer registry：`IRabbitMQSerializerRegistry`、`INatsSerializerRegistry`。使用 `GetSerializer<T>` / `GetDeserializer<T>`。

測試與本機開發
- 測試框架：xUnit + NSubstitute，測試專案皆位於 `Adaptare.*.UnitTests`。
- 預設 .NET 版本為 net10.0，常用命令：
```pwsh
dotnet restore
dotnet build -c Release
dotnet test -c Release --no-restore -l trx
```
- CI 流程（參考 `.github/workflows/ci.yaml`）：
  - self-hosted runner，測試紀錄以 TRX 上傳，並打包 NuGet 套件（使用 `NUGETAPIKEY`）。
- 在測試中使用 `AddFake{Transport}MessageQueue` 以避免建立實體連線。

範例專案（samples）
- 我們新增一個簡易範例：`samples/Adaptare.Sample.Direct`（Direct transport），展示如何註冊 `MessageQueue`、新增 handler，並使用 `IMessageSender.PublishAsync` 發送訊息。可用下列步驟執行：

```pwsh
cd samples/Adaptare.Sample.Direct
dotnet run --project Adaptare.Sample.Direct.csproj
```

或執行 unit tests：
```pwsh
dotnet test samples/Adaptare.Sample.Direct.UnitTests -c Release --no-restore
```

調試與疑難排除（Debug / Troubleshooting）
- 啟動與 background registration 未執行：
  - 確認 `Add{Transport}MessageQueue` 是否呼叫且 `AddMessageQueue()` 已註冊 `MessageQueueBackground`。背景註冊會在 `MessageQueueBackground` 被 `IHost` 啟動時執行。
  - 若你在測試中想避免啟動背景流程，使用 `AddFake{Transport}MessageQueue`。

- `MessageSenderNotFoundException`：
  - 這表示沒有任何 exchange 的 `MatchAsync` 回傳 `true`。檢查有沒有呼叫 `Add*GlobPatternExchange` 或 `MessageQueueConfiguration.AddExchange(...)`，以及 subject 與 glob pattern 是否正確配對。也可檢查 `MessageQueueConfiguration.PushExchange` 的交換器順序。

- `MessageProcessFailException`：
  - 當遠端 handler 在 reply 的 header 設置 `MQ_Fail` 時，Request 呼叫端會收到 `MessageProcessFailException`。在處理端檢查是否有適當地設定 `MQ_Fail` header（例如在 RabbitMQ `Acknowledge` 情形下）與錯誤處理邏輯。

- NATS 連線問題：
  - 檢查 `INatsConnectionManager` 的連線配置與 `registerName` 是否一致。使用 `AddFakeNatsMessageQueue` 來進行單元測試。

- RabbitMQ 訂閱/ack 問題：
  - 若使用 `AutoAck = false`，務必在處理成功時呼叫 `BasicAck`，否則訊息會在 Nack 或重新投遞時回到 queue。
  - 檢查 `CreateChannelOptions` 是否與 `IChannel` 連線正確建立。

- 追蹤與日誌（Tracing / Logging）：
  - 使用 `TraceContextPropagator` 來確保 Activity/Trace 被正確注入/抽取，並在 `SubscribeRegistration` 或 `AcknowledgeSubscribeRegistration` 的 `HandleMessageAsync` 中使用 `TryExtract` 建立新的 Activity。
  - 在整合測試或本機開發時，增加更詳細的 logging（`ILogger`）以追蹤 header 與 subject 流程，或在呼叫 `Inject` 時檢查 header 是否包含必要的 tracing keys。


重要設計與慣例
- DI pattern：「註冊 MessageQueue → 註冊 transport」的 builder pattern： `AddMessageQueue().Add{Transport}MessageQueue(...)`。
- 訂閱註冊：把 `ISubscribeRegistration` 註冊到 DI，以讓 background 註冊去建立訂閱（BackgroundRegistration 會遍歷 `ISubscribeRegistration`）。
- 命名與 style：介面以 `I` 為前綴；建議在 handler / processor 調用前使用 `CreateAsyncScope` 建立 scope。
- 第一個符合的 exchange 原則：`MultiplexerMessageSender` 只會選擇第一個 `MatchAsync` 為 `true` 的交換器（這表示 exchange 的註冊順序會影響路由），有需要時可用 `MessageQueueConfiguration.PushExchange` 來把交換器推到最前面。
- 錯誤處理：如果沒有找到 sender 則會拋出 `MessageSenderNotFoundException`；若遠端 handler 標記失敗則會拋 `MessageProcessFailException`。

優先查閱檔案（新人閱讀清單）
- `Adaptare/DependencyInjection/ServiceCollectionExtensions.cs` — DI bootstrapping
- `Adaptare/MultiplexerMessageSender.cs` — exchange 選擇邏輯
- `Adaptare/Configuration/MessageQueueConfiguration.cs` — exchange 註冊與順序
- `Adaptare/TraceContextPropagator.cs` — 分散式追蹤對應
- `Adaptare/MessageHeaderValueConsts.cs`、`Adaptare/MessageHeaderValue.cs` — header contract
- `Adaptare.RabbitMQ/Configuration/SubscribeRegistration.cs` — consumer/handler 範例
- `Adaptare.Nats/*` 與 `Adaptare.RabbitMQ/*` — 各傳輸層實作

快速提示（Do/Don't）
- Do：使用現有 `AddMessageQueue` 與 `Add*MessageQueue` 擴充來保持一致的 DI 註冊與初始化流程。
- Do：在建立 headers 時使用 `TraceContextPropagator.Inject` 保留追蹤上下文。
- Do：在單元測試中使用 `AddFake{Transport}MessageQueue` 來避免建立外部連線與背景工作。
- Do：在需要 transport-specific sender 時使用 `ActivatorUtilities.CreateInstance<>` 來保持 DI lifetime 與依賴注入一致。
- Don’t：不要在 `MultiplexerMessageSender` 內硬編新 exchange，應新增 `IMessageExchange` 與註冊方式。

範例
- 在 Startup 註冊 NATS handler：
```csharp
services.AddMessageQueue()
  .AddNatsMessageQueue(cfg => cfg.AddHandler<HandlerType>("subject1"));
```
- 發送 request 並解析回覆：
```csharp
var messageSender = sp.GetRequiredService<IMessageSender>();
var responseData = await messageSender.RequestAsync<MyRequest, MyReply>("subject1", request);
```

---
## Copilot/程式碼風格（合併於本專案的風格規範）
### 命名規則
- 介面名稱以 `I` 開頭。
- 類別、結構、列舉和介面使用 PascalCase。
- 常數使用 PascalCase。
- 私有或內部靜態欄位以 `_` 開頭；私有或內部欄位以 `m_` 開頭（簡潔的慣例，請留意專案已有不同風格的地方）。

### 格式化規則
- 縮排使用 Tab（Tab 寬度 4）。
- 每行不超過 100 個字元。
- 行結尾使用 CRLF。

### 程式碼風格要點
- 優先使用 `var`。
- 優先使用表達式主體、模式比對、條件運算與初始化器等 C# 現代語法糖。
- 優先使用 `readonly` 欄位與不可變結構。
- 優先使用檔案範圍命名空間、方法群組、頂層語句與局部函式（必要時）。

### 空格與新行
- 在 `catch`、`else`、`finally` 前換行；在匿名型別和物件初始化器的成員前換行；逗號後插入空格等。
