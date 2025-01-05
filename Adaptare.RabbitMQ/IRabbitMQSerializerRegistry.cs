using Adaptare.RabbitMQ;

namespace Adaptare.RabbitMQ;

public interface IRabbitMQSerializerRegistry
{
	IRabbitMQSerializer<TMessage> GetSerializer<TMessage>();

	IRabbitMQDeserializer<TMessage> GetDeserializer<TMessage>();
}
