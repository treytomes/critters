using Critters.Events;
using Critters.States;

namespace Critters.Services;

interface IGameStateManager : IGameComponent, IEventHandler, IDisposable
{
	bool HasState { get; }
	GameState? CurrentState { get; }
	void EnterState(GameState state);
	bool LeaveState();
}
