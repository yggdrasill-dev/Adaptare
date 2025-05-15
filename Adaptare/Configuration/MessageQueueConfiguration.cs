using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Adaptare.Configuration;

public class MessageQueueConfiguration
{
	private readonly OptionsBuilder<MessageExchangeOptions> m_ExchangeOptionsBuilder;

	public IServiceCollection Services { get; }

	public MessageQueueConfiguration(IServiceCollection services)
	{
		m_ExchangeOptionsBuilder = services.AddOptions<MessageExchangeOptions>();

		Services = services ?? throw new ArgumentNullException(nameof(services));

		Services.AddHostedService<MessageQueueBackground>();
	}

	public MessageQueueConfiguration AddExchange(IMessageExchange exchange)
	{
		m_ExchangeOptionsBuilder.Configure(options => options.Exchanges.Add(exchange));

		return this;
	}

	public MessageQueueConfiguration ClearAllExchanges()
	{
		m_ExchangeOptionsBuilder.Configure(options => options.Exchanges.Clear());

		return this;
	}

	public MessageQueueConfiguration PushExchange(IMessageExchange exchange)
	{
		m_ExchangeOptionsBuilder.Configure(options => options.Exchanges.Insert(0, exchange));

		return this;
	}

	public MessageQueueConfiguration RemoveExchange(IMessageExchange exchange)
	{
		m_ExchangeOptionsBuilder.Configure(options => options.Exchanges.Remove(exchange));

		return this;
	}
}