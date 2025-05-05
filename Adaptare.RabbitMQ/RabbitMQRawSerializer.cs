using System.Buffers;
using System.Text;

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
			string text => Encoding.UTF8.GetBytes(text),
			_ => next == null
				? throw new RabbitMQException($"Can't serialize {typeof(T)}")
				: next.Serialize(message)
		};
}