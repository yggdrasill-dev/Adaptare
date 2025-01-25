using DotNet.Globbing;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace Adaptare.RabbitMQ.Exchanges;

internal class RabbitGlobMessageExchange : IMessageExchange
{
	private readonly string m_ExchangeName;
	private readonly CreateChannelOptions? m_CreateChannelOptions;
	private readonly Glob m_Glob;

	public RabbitGlobMessageExchange(string pattern, string exchangeName, CreateChannelOptions? createChannelOptions)
	{
		ArgumentException.ThrowIfNullOrEmpty(exchangeName, nameof(exchangeName));

		m_Glob = Glob.Parse(pattern);
		m_ExchangeName = exchangeName;
		m_CreateChannelOptions = createChannelOptions;
	}

	public async ValueTask<IMessageSender> GetMessageSenderAsync(
		string subject,
		IServiceProvider serviceProvider,
		CancellationToken cancellationToken = default)
	{
		var factory = serviceProvider.GetRequiredService<IMessageQueueServiceFactory>();
		var connectionManager = serviceProvider.GetRequiredService<RabbitMQConnectionManager>();
		var connection = await connectionManager.GetConnectionAsync(cancellationToken).ConfigureAwait(false);

		return factory.CreateMessageQueueService(
			 serviceProvider,
			 m_ExchangeName,
			 await connection.CreateChannelAsync(m_CreateChannelOptions, cancellationToken).ConfigureAwait(false));
	}

	public ValueTask<bool> MatchAsync(
		string subject,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
	{
		return ValueTask.FromResult(m_Glob.IsMatch(subject));
	}
}