<!--
  Adaptare: English README (en-US)
  This file is the official README in en-US. A Traditional Chinese copy is available at `README.zh-TW.md`.
-->

# Adaptare

A lightweight, transport-agnostic .NET message queue abstraction library for sending, receiving, and processing messages across different transports (Direct/in-process, NATS, RabbitMQ).

## Overview

Adaptare provides a small set of core abstractions that let you implement and register message handlers and processors without coupling your application logic to a specific transport.

Key abstractions:
- `IMessageSender` — Publish, Send, or Request messages (the multiplexer is registered at startup).
- `IMessageReceiver<TSubscriptionSettings>` — Subscribe to messages for a specific transport.
- `IMessageExchange` — Match a subject/header (`MatchAsync`) and return a transport-specific `IMessageSender`.
- `IMessageHandler<T>` / `IMessageProcessor<T,TR>` — Server-side handler/processor.
- `ISubscribeRegistration` / `IMessageQueueBackgroundRegistration` — Background registration and subscription lifecycle management.

Register `AddMessageQueue()` during startup to add the multiplexer sender, and then add transports with their respective extension methods.

## Quick start

```pwsh
dotnet restore
dotnet build -c Release
dotnet test -c Release --no-restore
```

There is a Direct transport sample under `samples/Adaptare.Sample.Direct` demonstrating a minimal in-process flow.

## Getting started (DI and startup)

Adaptare uses a builder pattern that integrates with Microsoft DI.
Register the MessageQueue in your application startup:

```csharp
builder.Services
    .AddMessageQueue();
```

Select transports and register handlers/processors with the transport-specific extensions.
Direct transport example:

```csharp
builder.Services
    .AddMessageQueue()
    .AddDirectMessageQueue(cfg => cfg
        .AddProcessor<ProcessorType>("subject1")
        .AddHandler<HandlerType>("subject2")
        .AddReplyHandler<HandlerType2>("subject3"))
    .AddDirectGlobPatternExchange("*");
```

NATS and RabbitMQ have analogous `Add{Transport}MessageQueue(...)` extension points.

`AddMessageQueue()` registers a `MultiplexerMessageSender` as `IMessageSender`. The multiplexer uses the configured exchanges (`MessageExchangeOptions.Exchanges`) and selects the first matching `IMessageExchange` by invoking `MatchAsync`.

## Message semantics

API methods on `IMessageSender` express different messaging semantics:
- `PublishAsync`: fire-and-forget (one-way).
- `RequestAsync<TMessage, TReply>`: Request/Reply: waits for a `TReply`. If the reply contains `MQ_Fail` in the header, a `MessageProcessFailException` is thrown at the caller.
- `SendAsync`: transport-specific semantics; some transports (for example, NATS) support sending raw bytes and receiving replies; others may throw `NotSupportedException`.

Example usage:

```csharp
var messageSender = serviceProvider.GetRequiredService<IMessageSender>();
await messageSender.PublishAsync("subject1", requestBytes, headers, cancellationToken);

var reply = await messageSender.RequestAsync<byte[], MyReply>("subject1", requestBytes, headers);
```

## Headers, tracing, and failures

Use `MessageHeaderValue` to carry message metadata. The library provides `TraceContextPropagator` to inject and extract distributed tracing context from headers (Activity/TraceContext propagation).

Common header keys are defined in `MessageHeaderValueConsts`:
- `MQ_Ask_Id`, `MQ_Reply_Id`, `MQ_Reply`, and `MQ_Fail`.

If a remote handler needs to indicate failure, it sets `MQ_Fail` on the reply header. The request caller then receives `MessageProcessFailException`.

## Subscriptions and ack behavior

Subscriptions are registered with `ISubscribeRegistration`, and background registrations create actual consumers. Typical RabbitMQ scenarios:
- `SubscribeRegistration<TMessage, THandler>` — Basic handler with `AutoAck` set to `true` or `false`. If `AutoAck=false`, the consumer must call `BasicAck` on success and can call `BasicNack` on failure.
- `AcknowledgeSubscribeRegistration<TMessage, THandler>` — Provides advanced ack control using `IAcknowledgeMessage<T>` and `IAcknowledgeMessageHandler<T>` (Ack, Nak, progress reports, aborts).

Background registration implementations include `NatsBackgroundRegistration` and `RabbitMQBackgroundRegistration`.

## Adding transports or exchanges

To add a new exchange or transport:
1. Implement `IMessageExchange`:
   - `MatchAsync(subject, header)` — when this exchange should handle the subject.
   - `GetMessageSenderAsync(subject, IServiceProvider)` — return a transport-specific `IMessageSender`.
2. Register your exchange via `MessageQueueConfiguration.AddExchange(exchange)` or implement an `Add{Transport}MessageQueue` extension.

Note the multiplexer uses the first matching exchange, so the registration order affects routing. Use `MessageQueueConfiguration.PushExchange` if you need to prioritize an exchange.

## Serializers

Each transport exposes a serializer registry (for example, `IRabbitMQSerializerRegistry` and `INatsSerializerRegistry`). Use `GetSerializer<T>` and `GetDeserializer<T>` for typed serialization and deserialization.

## Testing

- Tests use xUnit + NSubstitute and are located under `Adaptare.*.UnitTests`.
- During unit tests, use `AddFake{Transport}MessageQueue` to avoid starting background registrations or creating real network connections (see `AddFakeNatsMessageQueue` and `AddFakeRabbitMessageQueue`).

## Troubleshooting & common issues

- `MessageSenderNotFoundException`: thrown when no exchange matches the subject — verify you registered an exchange (or used `Add*GlobPatternExchange`) and check exchange order.
- `MessageProcessFailException`: thrown when a reply header contains `MQ_Fail`.
- RabbitMQ ack issues: if `AutoAck = false`, ensure `BasicAck` is called when processing succeeds, otherwise messages will be requeued.
- NATS connectivity issues: verify `INatsConnectionManager` configuration and `registerName` settings.

## Contributing & code style

See `.github/copilot-instructions.md` for Copilot guidance and contributor guidelines.
Short code style pointers:
- PascalCase for types, `I` prefix for interfaces.
- Private static fields start with `_`, non-static private fields with `m_` (project convention).
- Tabs for indentation (size 4), lines limited to 100 characters, CRLF line endings.

When adding a new transport or feature, prefer adding an `IMessageExchange` implementation and writing appropriate tests.

---

## Snippets

Dependency Injection:

```csharp
builder.Services
    .AddMessageQueue();
```

Configure a transport (Direct):

```csharp
builder.Services
    .AddMessageQueue()
    .AddDirectMessageQueue(config => config
        .AddProcessor<ProcessorType>("subject1")
        .AddHandler<HandlerType>("subject2")
        .AddReplyHandler<HandlerType2>("subject3"))
    .AddDirectGlobPatternExchange("*");
```

Sending messages:

```csharp
var messageSender = serviceProvider.GetRequiredService<IMessageSender>();

var request = new SendMessageType
{
    // fill request payload
};

await messageSender.PublishAsync("subject1", request.ToByteArray(), cancellationToken).ConfigureAwait(false);

// or

var responseData = await messageSender.RequestAsync<byte[]>("subject1", request.ToByteArray(), cancellationToken).ConfigureAwait(false);

// parse response if typed
var response = ReceiveType.Parser.ParseFrom(responseData.Span);
```

Receiving messages:

```csharp
internal class ProcessorType : IMessageProcessor
{
    public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        var request = SendMessageType.Parser.ParseFrom(data.Span);

        // ...process request

        return response.ToByteArray();
    }
}

internal class HandlerType : IMessageHandler
{
    public async ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        var request = SendMessageType.Parser.ParseFrom(data.Span);

        // ...process request
    }
}
```

## License

[MIT](https://choosealicense.com/licenses/mit/)
