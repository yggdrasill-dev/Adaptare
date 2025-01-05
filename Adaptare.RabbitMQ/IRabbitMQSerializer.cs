using System.Buffers;

namespace Adaptare.RabbitMQ;

public interface IRabbitMQSerializer<in TMessage>
{
	ReadOnlyMemory<byte> Serialize(TMessage message);
}