namespace Adaptare.Direct.Configuration;

public class DirectAcknowledgeOptions
{
	public Action<AcknowledgeResponse> AcknowledgeCallback { get; set; } = _ => { };
}