using System.Buffers;

namespace Adaptare.RabbitMQ;

public interface IRabbitMQDeserializer<out TMessage>
{
	TMessage? Deserialize(in ReadOnlySequence<byte> buffer);
}