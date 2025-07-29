using System.Text;
using Adaptare.RabbitMQ.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Adaptare.RabbitMQ.UnitTests;

public class HandlerTests
{
    [Fact(Timeout = 1000)]
    public async Task Registration_Handler執行()
    {
        // Arrange
        var fakeMessageReceiver = Substitute.For<IMessageReceiver<RabbitSubscriptionSettings>>();
        var serviceProvider = new ServiceCollection()
            .AddMessageQueue()
            .AddRabbitMessageQueue(config => { })
            .Services
            .BuildServiceProvider();

        var sut = new SubscribeRegistration<ReadOnlyMemory<byte>, StubMessageHandler>(
            string.Empty,
            "test",
            true,
            new CreateChannelOptions(false, false),
            null,
            sp => new StubMessageHandler());

        var message = "aaa";

        fakeMessageReceiver.When(fake => fake.SubscribeAsync(Arg.Any<RabbitSubscriptionSettings>()))
            .Do(callInfo =>
            {
                var settings = callInfo.Arg<RabbitSubscriptionSettings>();
                settings.EventHandler(
                    Substitute.For<IChannel>(),
                    new BasicDeliverEventArgs(
                        "consumer",
                        1,
                        false,
                        "exchange",
                        "test",
                        new BasicProperties(),
                        Encoding.UTF8.GetBytes(message)));
            });
        // Act
        _ = await sut.SubscribeAsync(
            fakeMessageReceiver,
            serviceProvider,
            NullLogger<StubMessageHandler>.Instance,
            default);

        var actual = await StubMessageHandler.GetResultAsync();

        // Assert
        Assert.Equal(message, actual);
    }

    [Fact(Timeout = 1000)]
    public async Task Registration_AcknowledgeMessageHandler執行()
    {
        // Arrange
        var fakeMessageReceiver = Substitute.For<IMessageReceiver<RabbitSubscriptionSettings>>();
        var serviceProvider = new ServiceCollection()
            .AddMessageQueue()
            .AddRabbitMessageQueue(config => { })
            .Services
            .BuildServiceProvider();

        var sut = new AcknowledgeSubscribeRegistration<ReadOnlyMemory<byte>, StubAcknowledgeMessageHandler>(
            string.Empty,
            "test",
            new AcknowledgeOptions(),
            new CreateChannelOptions(false, false),
            null,
            sp => new StubAcknowledgeMessageHandler());

        var message = "aaa";

        fakeMessageReceiver.When(fake => fake.SubscribeAsync(Arg.Any<RabbitSubscriptionSettings>()))
            .Do(callInfo =>
            {
                var settings = callInfo.Arg<RabbitSubscriptionSettings>();
                settings.EventHandler(
                    Substitute.For<IChannel>(),
                    new BasicDeliverEventArgs(
                        "consumer",
                        1,
                        false,
                        "exchange",
                        "test",
                        new BasicProperties(),
                        Encoding.UTF8.GetBytes(message)));
            });
        // Act
        _ = await sut.SubscribeAsync(
            fakeMessageReceiver,
            serviceProvider,
            NullLogger<StubAcknowledgeMessageHandler>.Instance,
            default);

        var actual = await StubAcknowledgeMessageHandler.GetResultAsync();

        // Assert
        Assert.Equal(message, actual);
    }
}