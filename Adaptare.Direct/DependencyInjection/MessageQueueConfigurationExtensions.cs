using Adaptare.Configuration;
using Adaptare.Direct;
using Adaptare.Direct.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class MessageQueueConfigurationExtensions
{
	public static MessageQueueConfiguration AddDirectGlobPatternExchange(
		this MessageQueueConfiguration configuration,
		string glob)
		=> configuration.AddExchange(
			new DirectMessageExchange(glob, DirectMessageQueueConfiguration.SubscribeRegistrations));
}