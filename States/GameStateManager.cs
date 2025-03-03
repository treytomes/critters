using Critters.Events;
using Critters.Gfx;
using Critters.IO;

namespace Critters.States;

class GameStateManager : IGameComponent
{
  #region Fields

  private ResourceManager? _resources;
  private EventBus? _eventBus;
  private List<GameState> _states = new List<GameState>();

  #endregion

  #region Properties

  public bool HasState
  {
    get
    {
      return _states.Count > 0;
    }
  }

  public GameState? CurrentState
  {
    get
    {
      if (_states.Count == 0)
      {
        return null;
      }
      return _states[0];
    }
  }

  #endregion

  #region Methods

  public void Load(ResourceManager resources, EventBus eventBus)
  {
    _resources = resources;
    _eventBus = eventBus;

		eventBus.Subscribe<LeaveGameStateEventArgs>(OnLeave);
		eventBus.Subscribe<EnterGameStateEventArgs>(OnEnter);
  }

  public void Unload(ResourceManager resources, EventBus eventBus)
  {
    while (HasState)
    {
      LeaveState();
    }

		eventBus.Unsubscribe<LeaveGameStateEventArgs>(OnLeave);
		eventBus.Unsubscribe<EnterGameStateEventArgs>(OnEnter);
  }

  public void EnterState(GameState state)
  {
		CurrentState?.LostFocus(_eventBus!);
    _states.Insert(0, state);
    state.Load(_resources!, _eventBus!);
		CurrentState?.AcquireFocus(_eventBus!);
  }

  public void LeaveState()
  {
		CurrentState?.LostFocus(_eventBus!);
    CurrentState?.Unload(_resources!, _eventBus!);
    _states.RemoveAt(0);
		CurrentState?.AcquireFocus(_eventBus!);
  }

  public void Render(RenderingContext rc, GameTime gameTime)
  {
    CurrentState?.Render(rc, gameTime);
  }

  public void Update(GameTime gameTime)
  {
    CurrentState?.Update(gameTime);
  }

	private void OnLeave(LeaveGameStateEventArgs e)
	{
		LeaveState();
	}

	private void OnEnter(EnterGameStateEventArgs e)
	{
		EnterState(e.State);
	}

  #endregion
}
