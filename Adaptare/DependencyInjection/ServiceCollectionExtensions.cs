using Adaptare;
using Adaptare.Configuration;

using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static MessageQueueConfiguration AddMessageQueue(this IServiceCollection services)
	{
		var configuration = new MessageQueueConfiguration(services);

		_ = services
			.AddSingleton<IReplyPromiseStore, ReplyPromiseStore>()
			.AddSingleton<IMessageSender>(
				sp => new MultiplexerMessageSender(
					sp,
					sp.GetRequiredService<IOptions<MessageExchangeOptions>>().Value.Exchanges));

		return configuration;
	}
}