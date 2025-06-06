﻿using Microsoft.Extensions.DependencyInjection;
using NATS.Client.Core;
using NATS.Client.JetStream.Models;

namespace Adaptare.Nats.UnitTests;

public class DependencyInjectionTests
{
    [Fact]
    public void 如果註冊同樣Type的實體_取得IEnumerable應該得到所有實體()
    {
        // Arrange
        var services = new ServiceCollection()
            .AddSingleton("1")
            .AddSingleton("2")
            .AddSingleton("3");

        var sut = services.BuildServiceProvider();

        var expected = new[]
        {
            "1",
            "2",
            "3"
        };

        // Act
        var actual = sut.GetRequiredService<IEnumerable<string>>();

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void 註冊NatsMessageQueue()
    {
        // Arrange
        var sut = new ServiceCollection();

        // Act
        sut.AddMessageQueue()
            .AddNatsMessageQueue(config => config
                .ConfigureResolveConnection(sp => sp.GetRequiredService<NatsConnection>()));
    }

    [Fact]
    public void 註冊一個MessageHandler()
    {
        // Arrange
        var sut = new ServiceCollection();

        // Act
        sut.AddMessageQueue()
            .AddNatsMessageQueue(config => config
                .AddHandler<StubMessageHandler<string>>("a.b.c"));
    }

    [Fact]
    public void 註冊一個Group的MessageHandler()
    {
        // Arrange
        var sut = new ServiceCollection();

        // Act
        sut.AddMessageQueue()
            .AddNatsMessageQueue(config => config
                .AddHandler<StubMessageHandler<string>>("a.b.c", "group"));
    }

    [Fact]
    public void 以MessageHandlerType註冊MessageHandler()
    {
        // Arrange
        var sut = new ServiceCollection();

        // Act
        sut.AddMessageQueue()
            .AddNatsMessageQueue(config => config
                .AddHandler(typeof(StubMessageHandler<string>), "a.b.c"));
    }

    [Fact]
    public void 以MessageHandlerType註冊Group的MessageHandler()
    {
        // Arrange
        var sut = new ServiceCollection();

        // Act
        sut.AddMessageQueue()
            .AddNatsMessageQueue(config => config
                .AddHandler(typeof(StubMessageHandler<string>), "a.b.c", "group"));
    }

    [Fact]
    public void 註冊一個MessageProcessor()
    {
        // Arrange
        var sut = new ServiceCollection();

        // Act
        sut.AddMessageQueue()
            .AddNatsMessageQueue(config => config
                .AddProcessor<StubMessageProcessor<string, string>>("a.b.c"));
    }

    [Fact]
    public void 註冊一個Group的MessageProcessor()
    {
        // Arrange
        var sut = new ServiceCollection();

        // Act
        sut.AddMessageQueue()
            .AddNatsMessageQueue(config => config
                .AddProcessor<StubMessageProcessor<string, string>>("a.b.c", "group"));
    }

    [Fact]
    public void 以MessageProcessorType註冊MessageProcessor()
    {
        // Arrange
        var sut = new ServiceCollection();

        // Act
        sut.AddMessageQueue()
            .AddNatsMessageQueue(config => config
                .AddProcessor(typeof(StubMessageProcessor<string, string>), "a.b.c"));
    }

    [Fact]
    public void 以MessageProcessorType註冊Group的MessageProcessor()
    {
        // Arrange
        var sut = new ServiceCollection();

        // Act
        sut.AddMessageQueue()
            .AddNatsMessageQueue(config => config
                .AddProcessor(typeof(StubMessageProcessor<string, string>), "a.b.c", "group"));
    }

    [Fact]
    public void 註冊一個ReplyMessageHandler()
    {
        // Arrange
        var sut = new ServiceCollection();

        // Act
        sut.AddMessageQueue()
            .AddNatsMessageQueue(config => config
                .AddReplyHandler<StubMessageHandler<string>>("a.b.c"));
    }

    [Fact]
    public void 註冊一個Group的ReplyMessageHandler()
    {
        // Arrange
        var sut = new ServiceCollection();

        // Act
        sut.AddMessageQueue()
            .AddNatsMessageQueue(config => config
                .AddReplyHandler<StubMessageHandler<string>>("a.b.c", "group"));
    }

    [Fact]
    public void 以MessageHandlerType註冊ReplyMessageHandler()
    {
        // Arrange
        var sut = new ServiceCollection();

        // Act
        sut.AddMessageQueue()
            .AddNatsMessageQueue(config => config
                .AddReplyHandler(typeof(StubMessageHandler<string>), "a.b.c"));
    }

    [Fact]
    public void 以MessageHandlerType註冊Group的ReplyMessageHandler()
    {
        // Arrange
        var sut = new ServiceCollection();

        // Act
        sut.AddMessageQueue()
            .AddNatsMessageQueue(config => config
                .AddReplyHandler(typeof(StubMessageHandler<string>), "a.b.c", "group"));
    }

    [Fact]
    public void 註冊一個JetStreamHandler()
    {
        // Arrange
        var sut = new ServiceCollection();

        // Act
        sut.AddMessageQueue()
            .AddNatsMessageQueue(config => config
                .AddJetStreamHandler<StubJetStreamHandler<string>>("a.b.c", "stream", new ConsumerConfig()));
    }

    [Fact]
    public void 以JetStreamHandlerType註冊JetStreamHandler()
    {
        // Arrange
        var sut = new ServiceCollection();

        // Act
        sut.AddMessageQueue()
            .AddNatsMessageQueue(config => config
                .AddJetStreamHandler(
                    typeof(StubJetStreamHandler<string>),
                    "a.b.c",
                    "stream",
                    new ConsumerConfig()));
    }
}