﻿using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using Adaptare.RabbitMQ.UnitTests;
using Adaptare.RabbitMQ.Configuration;

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
}
