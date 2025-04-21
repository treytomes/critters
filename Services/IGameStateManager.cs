using Critters.States;

namespace Critters.Services;

interface IGameStateManager : IGameComponent, IDisposable
{
	bool HasState { get; }
	GameState? CurrentState { get; }
	void EnterState(GameState state);
	void LeaveState();
}
