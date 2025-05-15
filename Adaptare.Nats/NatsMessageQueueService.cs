using NATS.Client.JetStream.Models;

namespace Adaptare.Nats;

internal class NatsMessageQueueService(
	INatsConnectionManager natsConnectionManager) 
	: INatsMessageQueueService
{
	public async ValueTask RegisterStreamAsync(StreamConfig config, CancellationToken cancellationToken = default)
	{
		var js = natsConnectionManager.CreateJsContext();

		_ = await js.ListStreamsAsync(cancellationToken: cancellationToken)
			.AnyAsync(stream => stream.Info.Config.Name == config.Name, cancellationToken)
			.ConfigureAwait(false)
			? await js.UpdateStreamAsync(
				config,
				cancellationToken).ConfigureAwait(false)
			: await js.CreateStreamAsync(
				config,
				cancellationToken).ConfigureAwait(false);
	}

	public ValueTask<IDisposable> SubscribeAsync(INatsSubscribe settings, CancellationToken cancellationToken = default)
		=> settings.SubscribeAsync(natsConnectionManager, cancellationToken);
}