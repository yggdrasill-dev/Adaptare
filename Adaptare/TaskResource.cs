using System.Runtime.CompilerServices;

namespace Adaptare;

public class TaskResource<TResource>(Task<TResource> resourcePromise)
	: IAsyncDisposable
{
	private Task<TResource> Resource => resourcePromise;

	public static implicit operator TaskResource<TResource>(Task<TResource> resource) => new(resource);

	public static implicit operator Task<TResource>(TaskResource<TResource> resource) => resource.Resource;

	public TaskAwaiter<TResource> GetAwaiter() => resourcePromise.GetAwaiter();

	public ConfiguredTaskAwaitable<TResource> ConfigureAwait(bool continueOnCapturedContext)
		=> resourcePromise.ConfigureAwait(continueOnCapturedContext);

	public ConfiguredTaskAwaitable<TResource> ConfigureAwait(ConfigureAwaitOptions options)
		=> resourcePromise.ConfigureAwait(options);

	public async ValueTask DisposeAsync()
	{
		try
		{
			var result = await resourcePromise.ConfigureAwait(continueOnCapturedContext: false);

			if (result is IAsyncDisposable asyncDisposable)
				await asyncDisposable.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
			else if (result is IDisposable disposable)
			{
				disposable.Dispose();
			}
		}
		catch (TaskCanceledException)
		{
		}

		GC.SuppressFinalize(this);
	}
}