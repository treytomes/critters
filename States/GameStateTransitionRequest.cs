namespace Critters.States;

/// <summary>
/// Represents a request to transition between game states.
/// </summary>
class GameStateTransitionRequest(TransitionType type, GameState? targetState)
{
	/// <summary>
	/// Gets the type of transition requested.
	/// </summary>
	public TransitionType Type => type;

	/// <summary>
	/// Gets the target state for the transition, if applicable.
	/// </summary>
	public GameState? TargetState => targetState;
}