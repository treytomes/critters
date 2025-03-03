namespace Critters.States;

/// <summary>
/// Request entry to a new state state.
/// </summary>
readonly struct EnterGameStateEventArgs
{
	public EnterGameStateEventArgs(GameState state)
	{
		State = state;
	}

	public GameState State { get; }
}