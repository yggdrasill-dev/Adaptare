using Adaptare.RabbitMQ.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace Adaptare.RabbitMQ.UnitTests;

public partial class DependencyInjectionTests
{
    [Fact]
    public void 註冊RabbitMQ()
    {
        // Arrange
        var sut = new ServiceCollection();

        // Act
        sut.AddMessageQueue()
            .AddRabbitMessageQueue(configure => configure
                .ConfigureConnection(
                    sp => new RabbitMQConnectionOptions
                    {
                        ConnectionPromise = Task.FromResult(Substitute.For<IConnection>())
                    }));
    }

    [Fact]
    public void 註冊RabbitMQ的Handler()
    {
        // Arrange
        var sut = new ServiceCollection();

        // Act
        sut.AddMessageQueue()
            .AddRabbitMessageQueue(configure => configure
                .AddHandler<StubMessageHandler>("queueName"));
    }

    [Fact]
    public void 用HandlerType註冊RabbitMQ的Handler()
    {
        // Arrange
        var sut = new ServiceCollection();

        // Act
        sut.AddMessageQueue()
            .AddRabbitMessageQueue(configure => configure
                .AddHandler(typeof(StubMessageHandler), "queueName"));
    }

    [Fact]
    public void 註冊RabbitMQ的AcknowledgeHandler()
    {
        // Arrange
        var sut = new ServiceCollection();

        // Act
        sut.AddMessageQueue()
            .AddRabbitMessageQueue(configure => configure
                .AddAcknowledgeHandler<StubAcknowledgeMessageHandler>("queueName"));
    }

    [Fact]
    public void 用HandlerType註冊RabbitMQ的AcknowledgeHandler()
    {
        // Arrange
        var sut = new ServiceCollection();

        // Act
        sut.AddMessageQueue()
            .AddRabbitMessageQueue(configure => configure
                .AddAcknowledgeHandler(typeof(StubAcknowledgeMessageHandler), "queueName"));
    }

    [Fact]
    public void 註冊FakeRabbitMessageQueue()
    {
        // Arrange
        var sut = new ServiceCollection();

        sut.AddMessageQueue()
            .AddRabbitMessageQueue(configure => configure
                .ConfigureConnection(
                    sp => new RabbitMQConnectionOptions
                    {
                        ConnectionPromise = Task.FromResult(Substitute.For<IConnection>())
                    }))
            .Services
            .AddFakeRabbitMessageQueue();

        // Act
        var finds = sut
            .Where(desc => desc.ServiceType == typeof(IMessageQueueBackgroundRegistration))
            .ToArray();

        // Assert
        Assert.Empty(finds);
    }
}