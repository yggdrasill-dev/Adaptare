using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;

namespace Adaptare.Nats.Configuration.Registrations;

internal abstract class BaseRegistration<TMessage, THandler>
{
	public abstract string Subject { get; }

	protected async ValueTask HandleMessageAsync(
		MessageDataInfo<NatsMsg<TMessage>> dataInfo,
		CancellationToken cancellationToken)
	{
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

		Activity.Current?.AddTag("handler", typeof(THandler).Name);

		try
		{
			var scope = dataInfo.ServiceProvider.CreateAsyncScope();
			await using (scope.ConfigureAwait(false))
			{
				await ExecuteMessageHandlerAsync(
					scope.ServiceProvider,
					dataInfo,
					cts.Token).ConfigureAwait(false);
			}
		}
		catch (Exception ex)
		{
			Activity.Current?.AddTag("error", true);

			dataInfo.Logger.LogError(ex, "Handle {Subject} occur error.", Subject);
		}
	}

	protected abstract ValueTask ExecuteMessageHandlerAsync(
		IServiceProvider serviceProvider,
		MessageDataInfo<NatsMsg<TMessage>> dataInfo,
		CancellationToken cancellationToken);
}