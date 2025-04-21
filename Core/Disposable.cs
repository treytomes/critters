using System.Reactive.Subjects;

namespace Critters.Core;

/// <summary>  
/// Provides a base implementation for disposable objects with proper resource cleanup  
/// and disposal notification using Reactive Extensions.  
/// </summary>  
public abstract class Disposable : IDisposable
{
	#region Fields

	private readonly Subject<DisposalEventArgs> _disposalEvents = new();

	/// <summary>
	/// Using int for Interlocked operations (0 = false, 1 = true).
	/// </summary>
	private int _isDisposed;

	#endregion

	#region Properties

	/// <summary>  
	/// Gets an observable sequence of disposal events.  
	/// </summary>  
	public IObservable<DisposalEventArgs> DisposalEvents => _disposalEvents;

	/// <summary>  
	/// Gets a value indicating whether this instance has been disposed.  
	/// </summary>  
	public bool IsDisposed => Interlocked.CompareExchange(ref _isDisposed, 0, 0) == 1;

	#endregion

	#region Methods

	/// <summary>  
	/// Disposes the current instance.  
	/// </summary>  
	public void Dispose()
	{
		// Attempt to set _isDisposed from 0 to 1. If it returns 1, it was already disposed.  
		if (Interlocked.Exchange(ref _isDisposed, 1) != 0)
			return;

		try
		{
			// Notify observers that disposal has begun  
			_disposalEvents.OnNext(new DisposalEventArgs(DisposalStage.Started, this));

			// Dispose pattern implementation  
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		finally
		{
			// Notify observers that disposal has completed  
			_disposalEvents.OnNext(new DisposalEventArgs(DisposalStage.Completed, this));

			// Complete and dispose the subject  
			_disposalEvents.OnCompleted();
			_disposalEvents.Dispose();
		}
	}

	/// <summary>  
	/// Throws an ObjectDisposedException if this instance has been disposed.  
	/// </summary>  
	/// <param name="methodName">Optional name of the method being called.</param>  
	/// <exception cref="ObjectDisposedException">Thrown if this instance is disposed.</exception>  
	protected void ThrowIfDisposed(string? methodName = null)
	{
		if (IsDisposed)
		{
			throw new ObjectDisposedException(GetType().Name,
				methodName != null ? $"Cannot access {methodName} after disposal" : null);
		}
	}

	/// <summary>  
	/// Disposes the object's resources.  
	/// </summary>  
	/// <param name="disposing">True to dispose managed resources.</param>  
	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			DisposeManagedResources();
		}

		DisposeUnmanagedResources();
	}

	/// <summary>  
	/// Disposes managed resources. Override this method in derived classes to dispose  
	/// specific managed resources.  
	/// </summary>  
	protected virtual void DisposeManagedResources()
	{
		// Base implementation does nothing  
	}

	/// <summary>  
	/// Disposes unmanaged resources. Override this method in derived classes to dispose  
	/// specific unmanaged resources.  
	/// </summary>  
	protected virtual void DisposeUnmanagedResources()
	{
		// Base implementation does nothing  
	}

	/// <summary>  
	/// Finalizer to ensure unmanaged resources are cleaned up.  
	/// </summary>  
	~Disposable()
	{
		Dispose(false);
	}

	#endregion
}

