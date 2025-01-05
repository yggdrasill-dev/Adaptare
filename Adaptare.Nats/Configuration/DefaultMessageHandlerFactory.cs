using Microsoft.Extensions.DependencyInjection;

namespace Adaptare.Nats.Configuration;

internal static class DefaultMessageHandlerFactory<THandler>
{
	public static Func<IServiceProvider, THandler> Default = sp => ActivatorUtilities.CreateInstance<THandler>(sp);
}