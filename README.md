# Adaptare
Adaptare is a library developed to abstract the sending, receiving, and processing of message transmission.

## Examples

* Dependency Injection

    ```csharp
    builder.Services
        .AddMessageQueue();
    ```

* Configuration for Adding MessageQueue System

    ```csharp
    builder.Services
        .AddMessageQueue()
        .AddDirectMessageQueue(config => config
            .AddProcessor<ProcessorType>("subject1")
            .AddHandler<HandlerType>("subject2")
            .AddReplyHandler<HandlerType2>("subject3"))
        .AddDirectGlobPatternExchange("*");
    ```

* Sending Messages

    ```csharp
    var messageSender = serviceProvider.GetRequiredService<IMessageSender>();
 
    var request = new SendMessageType
    {
        ...
    };

    await messageSender.PublishAsync(
        "sudject1",
        request.ToByteArray(),
        cancellationToken).ConfigureAwait(false);

    // or

    var responseData = await messageSender.RequestAsync(
        "sudject1",
        request.ToByteArray(),
        cancellationToken).ConfigureAwait(false);

    var response = ReceiveType.Parser.ParseFrom(responseData.Span);

    // or

    await messageSender.SendAsync(
        "sudject1",
        request.ToByteArray(),
        cancellationToken).ConfigureAwait(false);
    ```

* Receiving Messages

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