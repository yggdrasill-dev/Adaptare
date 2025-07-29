namespace Adaptare.RabbitMQ.Configuration;

public class AcknowledgeOptions
{
	public bool Multiple { get; set; } = false;

	public bool NackRequeue { get; set; } = false;
}