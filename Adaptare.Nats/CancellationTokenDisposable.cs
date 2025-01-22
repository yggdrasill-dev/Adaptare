namespace Adaptare.Nats;

internal class CancellationTokenDisposable(CancellationToken token = default) : IDisposable
{
	private readonly CancellationTokenSource m_Cts = CancellationTokenSource.CreateLinkedTokenSource(token);

	public CancellationToken Token => m_Cts.Token;

	public void Dispose()
	{
		if (!m_Cts.IsCancellationRequested)
		{
			m_Cts.Cancel();
		}
	}
}