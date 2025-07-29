using RabbitMQ.Client;

namespace Adaptare.RabbitMQ.Configuration;

public class RabbitMQSenderOptions : ICloneable
{
	public CreateChannelOptions? CreateChannelOptions { get; set; }

	public bool Mandatory { get; set; } = false;

	public string? AppId { get; set; }

	object ICloneable.Clone()
		=> Clone();

	public RabbitMQSenderOptions Clone()
		=> new()
		{
			CreateChannelOptions = CreateChannelOptions,
			Mandatory = Mandatory,
			AppId = AppId
		};
}