using System.Text;
using RabbitMQ.Client;

namespace Adaptare.RabbitMQ.UnitTests;

public class RabbitMessageQueueServiceTests
{
    [Fact]
    public async Task RabbitMessageQueueService_送出訊息()
    {
        // Arrange
        var fakeChannel = Substitute.For<IChannel>();

        var sut = new RabbitMessageSender(
            "test",
            "testApp",
            fakeChannel,
            RabbitMQSerializerRegistry.Default);

        var data = Encoding.UTF8.GetBytes("aaa");

        // Act
        await sut.PublishAsync("a.b.c", data);

        // Assert
        _ = fakeChannel.Received(1)
            .BasicPublishAsync(
                Arg.Is("test"),
                Arg.Is("a.b.c"),
                Arg.Is(false),
                Arg.Any<BasicProperties>(),
                Arg.Is<ReadOnlyMemory<byte>>(x => x.ToArray().SequenceEqual(data)),
                Arg.Any<CancellationToken>());
    }
}