using RabbitMQ.Client;

namespace Adaptare.RabbitMQ.Configuration;

public class RabbitMQSenderOptions
{
	public CreateChannelOptions? CreateChannelOptions { get; set; }

	public bool Mandatory { get; set; } = false;

	public string? AppId { get; set; }
}