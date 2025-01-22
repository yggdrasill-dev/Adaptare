using Microsoft.Extensions.Logging;

namespace Adaptare.RabbitMQ;

internal class ExceptionHandler(
	Func<Exception, CancellationToken, Task> handleException,
	ILogger<ExceptionHandler> logger)
{
	private readonly Func<Exception, CancellationToken, Task> m_HandleException = handleException ?? throw new ArgumentNullException(nameof(handleException));
	private readonly ILogger<ExceptionHandler> m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));

	public Task HandleExceptionAsync(Exception ex, CancellationToken cancellationToken = default)
	{
		try
		{
			return m_HandleException(ex, cancellationToken);
		}
		catch (Exception handleEx)
		{
			m_Logger.LogCritical(handleEx, "Handle exception occur error.");

			return Task.CompletedTask;
		}
	}
}
