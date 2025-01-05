namespace Adaptare.Nats;

internal class CancellationTokenDisposable : IDisposable
{
	private readonly CancellationTokenSource m_Cts;

	public CancellationTokenDisposable(CancellationToken token = default)
	{
		m_Cts = CancellationTokenSource.CreateLinkedTokenSource(token);
	}

	public CancellationToken Token => m_Cts.Token;

	public void Dispose()
	{
		if (!m_Cts.IsCancellationRequested)
		{
			m_Cts.Cancel();
		}
	}
}