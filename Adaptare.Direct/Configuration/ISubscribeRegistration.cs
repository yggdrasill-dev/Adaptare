using DotNet.Globbing;

namespace Adaptare.Direct.Configuration;

internal interface ISubscribeRegistration
{
	Glob SubjectGlob { get; }

	IMessageSender ResolveMessageSender(IServiceProvider serviceProvider);
}