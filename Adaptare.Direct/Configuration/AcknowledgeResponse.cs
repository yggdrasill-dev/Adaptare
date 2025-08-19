namespace Adaptare.Direct.Configuration;

public record AcknowledgeResponse(
	AcknowledgeType Type,
	Guid? MessageId);