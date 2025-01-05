using NATS.Client.JetStream.Models;

namespace Adaptare.Nats;

internal interface INatsMessageQueueService
	: IMessageReceiver<INatsSubscribe>
{
	ValueTask RegisterStreamAsync(StreamConfig config, CancellationToken cancellationToken = default);
}