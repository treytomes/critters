using System.Reactive.Disposables;

namespace Critters;

/// <summary>
/// Extension method to add a disposable to a CompositeDisposable
/// </summary>
static class DisposableExtensions
{
	public static T AddTo<T>(this T disposable, CompositeDisposable compositeDisposable)
		where T : IDisposable
	{
		compositeDisposable.Add(disposable);
		return disposable;
	}
}