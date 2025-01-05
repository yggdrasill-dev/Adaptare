using Microsoft.Extensions.DependencyInjection;

namespace Adaptare.Direct.UnitTests;

public class DependencyInjectionTests
{
	[Fact]
	public void DependencyInjection_註冊InProcessMessageQueue()
	{
		// Arrange
		var sut = new ServiceCollection();

		// Act
		sut.AddMessageQueue()
			.AddDirectMessageQueue(config => { });
	}

	[Fact]
	public void DependencyInjection_註冊一個MessageHandler()
	{
		// Arrange
		var sut = new ServiceCollection();

		// Act
		sut.AddMessageQueue()
			.AddDirectMessageQueue(config => config
				.AddHandler<StubMessageHandler<string>>("a.b.c"));
	}

	[Fact]
	public void DependencyInjection_以MessageHanderType註冊MessageHandler()
	{
		// Arrange
		var sut = new ServiceCollection();

		// Act
		sut.AddMessageQueue()
			.AddDirectMessageQueue(config => config
				.AddHandler(typeof(StubMessageHandler<string>), "a.b.c"));
	}

	[Fact]
	public void DependencyInjection_註冊一個MessageProcessor()
	{
		// Arrange
		var sut = new ServiceCollection();

		// Act
		sut.AddMessageQueue()
			.AddDirectMessageQueue(config => config
				.AddProcessor<StubMessageProcessor<string, string>>("a.b.c"));
	}

	[Fact]
	public void DependencyInjection_以MessageProcessorType註冊MessageProcessor()
	{
		// Arrange
		var sut = new ServiceCollection();

		// Act
		sut.AddMessageQueue()
			.AddDirectMessageQueue(config => config
				.AddProcessor(typeof(StubMessageProcessor<string, string>), "a.b.c"));
	}

	[Fact]
	public void DependencyInjection_註冊一個MessageSession()
	{
		// Arrange
		var sut = new ServiceCollection();

		// Act
		sut.AddMessageQueue()
			.AddDirectMessageQueue(config => config
				.AddSession<StubMessageSession<string>>("a.b.c"));
	}

	[Fact]
	public void DependencyInjection_以MessageSessionType註冊MessageSession()
	{
		// Arrange
		var sut = new ServiceCollection();

		// Act
		sut.AddMessageQueue()
			.AddDirectMessageQueue(config => config
				.AddSession(typeof(StubMessageSession<string>), "a.b.c"));
	}
}