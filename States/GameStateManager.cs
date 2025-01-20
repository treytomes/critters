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

  public GameState CurrentState
  {
    get
    {
      if (_states.Count == 0)
      {
        throw new NoStateException();
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
  }

  public void Unload(ResourceManager resources, EventBus eventBus)
  {
    while (HasState)
    {
      LeaveState();
    }
  }

  public void EnterState(GameState state)
  {
    _states.Insert(0, state);
    state.Load(_resources!, _eventBus!);
  }

  public void LeaveState()
  {
    CurrentState.Unload(_resources!, _eventBus!);
    _states.RemoveAt(0);
  }

  public void Render(RenderingContext rc, GameTime gameTime)
  {
    _states[0].Render(rc, gameTime);
  }

  #endregion
}
