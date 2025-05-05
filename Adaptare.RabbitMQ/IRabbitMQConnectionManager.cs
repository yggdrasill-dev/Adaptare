using RabbitMQ.Client;

namespace Adaptare.RabbitMQ;
public interface IRabbitMQConnectionManager
{
	Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken = default);
}