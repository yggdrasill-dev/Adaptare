using Adaptare.Direct.Configuration;
using DotNet.Globbing;

namespace Adaptare.Direct;

internal class DirectMessageExchange(
	string pattern,
	IEnumerable<ISubscribeRegistration> subscribeRegistrations) : IMessageExchange
{
	private readonly Glob m_Glob = Glob.Parse(pattern);

	public ValueTask<IMessageSender> GetMessageSenderAsync(
		string subject,
		IServiceProvider serviceProvider,
		CancellationToken cancellationToken = default)
	{
		foreach (var sub in subscribeRegistrations)
			if (sub.SubjectGlob.IsMatch(subject))
				return ValueTask.FromResult(sub.ResolveMessageSender(serviceProvider));

		throw new MessageSenderNotFoundException(subject);
	}

	public ValueTask<bool> MatchAsync(string subject, IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken = default)
		=> ValueTask.FromResult(m_Glob.IsMatch(subject));
}