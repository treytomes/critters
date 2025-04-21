using Critters.Events;

namespace Critters.States;

interface IGameState : IDisposable, IGameComponent, IEventHandler
{
	/// <summary>
	/// Gets an observable sequence of state transition requests.
	/// </summary>
	IObservable<GameStateTransitionRequest> TransitionRequests { get; }

	/// <summary>
	/// Called when this state becomes the active state.
	/// Override to attach event handlers.
	/// </summary>
	void AcquireFocus();

	/// <summary>
	/// Called when this state is no longer the active state.
	/// Override to detach event handlers.
	/// </summary>
	void LostFocus();
}
