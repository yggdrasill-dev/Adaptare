namespace Adaptare.UnitTests;

public class MultiplexerMessageSenderTests
{
	[Fact]
	public async Task MultiplexerMessageSender_訊息交換器會選擇第一個符合的Sender()
	{
		var fakeServiceProvider = Substitute.For<IServiceProvider>();

		var exchange1 = Substitute.For<IMessageExchange>();
		var exchange2 = Substitute.For<IMessageExchange>();

		_ = exchange1
			.MatchAsync(Arg.Any<string>(), Arg.Any<IEnumerable<MessageHeaderValue>>())
			.Returns(true);
		_ = exchange2
			.MatchAsync(Arg.Any<string>(), Arg.Any<IEnumerable<MessageHeaderValue>>())
			.Returns(true);

		_ = exchange1.GetMessageSenderAsync(Arg.Any<string>(), Arg.Any<IServiceProvider>())
			.Returns(Substitute.For<IMessageSender>());
		_ = exchange2.GetMessageSenderAsync(Arg.Any<string>(), Arg.Any<IServiceProvider>())
			.Returns(Substitute.For<IMessageSender>());

		var sut = new MultiplexerMessageSender(
			fakeServiceProvider,
			[
				exchange1,
				exchange2
			]);

		await sut.PublishAsync("test", Array.Empty<byte>());

		_ = await exchange1.Received(1)
			.GetMessageSenderAsync(Arg.Is("test"), Arg.Any<IServiceProvider>());
		_ = await exchange2.Received(0)
			.GetMessageSenderAsync(Arg.Is("test"), Arg.Any<IServiceProvider>());
	}

	[Fact]
	public async Task MultiplexerMessageSender_沒有找到任何符合的訊息交換器會發生Exception()
	{
		var fakeServiceProvider = Substitute.For<IServiceProvider>();

		var sut = new MultiplexerMessageSender(
			fakeServiceProvider,
			[]);

		_ = await Assert.ThrowsAsync<MessageSenderNotFoundException>(
			async () => await sut.PublishAsync("test", Array.Empty<byte>()));
	}
}