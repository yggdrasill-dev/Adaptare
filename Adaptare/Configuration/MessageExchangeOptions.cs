namespace Adaptare.Configuration;

internal class MessageExchangeOptions
{
	public List<IMessageExchange> Exchanges { get; } =
#if NET6_0
		new List<IMessageExchange>();

#else
		[];
#endif
}