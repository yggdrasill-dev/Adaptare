using System.Buffers;

namespace Adaptare.RabbitMQ;

public class RabbitMQRawSerializer<T>(IRabbitMQSerializer<T>? next) : IRabbitMQSerializer<T>
{
	public static readonly RabbitMQRawSerializer<T> Default = new(null);

	public ReadOnlyMemory<byte> Serialize(T message)
		=> message switch
		{
			byte[] bytes => bytes,
			Memory<byte> memory => memory.ToArray(),
			ReadOnlyMemory<byte> readOnlyMemory => readOnlyMemory,
			ReadOnlySequence<byte> readOnlySequence => readOnlySequence.ToArray(),
			_ => next == null
				? throw new RabbitMQException($"Can't serialize {typeof(T)}")
				: next.Serialize(message)
		};
}