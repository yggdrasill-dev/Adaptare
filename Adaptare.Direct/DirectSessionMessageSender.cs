using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Adaptare.Direct;

internal class DirectSessionMessageSender<TQuestion, TMessageSession>(
	Func<IServiceProvider, TMessageSession> sessionFactory,
	IServiceProvider serviceProvider)
	: IMessageSender
	where TMessageSession : IMessageSession<TQuestion>
{
	private readonly Func<IServiceProvider, TMessageSession> m_SessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
	private readonly IServiceProvider m_ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

	public async ValueTask<Answer<TReply>> AskAsync<TMessage, TReply>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
	{
		using var questionActivity = DirectDiagnostics.ActivitySource.StartActivity(
			subject,
			ActivityKind.Producer);

		_ = (questionActivity?.AddTag("mq", "Direct")
			.AddTag("handler", typeof(TMessageSession).Name));

		var scope = m_ServiceProvider.CreateAsyncScope();
		await using (scope.ConfigureAwait(false))
		{
			var handler = m_SessionFactory(scope.ServiceProvider);

			var question = new DirectQuestion<TQuestion>(subject, (TQuestion)(object)data!, header);

			_ = Task.Run(() => handler.HandleAsync(
				question,
				cancellationToken).AsTask(), cancellationToken);

			return await question.GetAnwserAsync<TReply>().ConfigureAwait(false);
		}
	}

	public ValueTask PublishAsync<TMessage>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
		=> throw new NotSupportedException();

	public ValueTask<TReply> RequestAsync<TMessage, TReply>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
		=> throw new NotSupportedException();

	public ValueTask SendAsync<TMessage>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
		=> throw new NotSupportedException();
}