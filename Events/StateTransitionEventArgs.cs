using Critters.States;

namespace Critters.Events;

/// <summary>  
/// Provides data for the StateTransitioned event, which occurs after a transition has completed.
/// </summary>  
class StateTransitionEventArgs(TransitionType transitionType, GameState? previousState, GameState? newState) : EventArgs
{
	/// <summary>  
	/// Gets the type of transition that occurred.  
	/// </summary>  
	public TransitionType TransitionType => transitionType;

	/// <summary>  
	/// Gets the previous state, if any.  
	/// </summary>  
	public GameState? PreviousState => previousState;

	/// <summary>  
	/// Gets the new state, if any.  
	/// </summary>  
	public GameState? NewState => newState;
}