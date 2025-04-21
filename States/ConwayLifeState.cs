using Critters.Gfx;
using Critters.Services;
using Critters.States.ConwayLife;
using Critters.UI;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Critters.States;

class ConwayLifeState : GameState
{
	#region Fields

	private Vector2 _mousePosition = Vector2.Zero;
	private ConwayLifeSim? _sim = null;
	private readonly SimulationConfig _config = new SimulationConfig();
	private float _timeSinceLastUpdate = 0;
	private bool _paused = false;

	#endregion

	#region Constructors

	public ConwayLifeState(IResourceManager resources, IRenderingContext rc)
		: base(resources, rc)
	{
		UI.Add(new Label(resources, rc, () => _sim?.CAType ?? "N/A", new Vector2(0, 0), new RadialColor(5, 5, 5)));
		UI.Add(new Label(resources, rc, () => _paused ? "PAUSED" : "Running", new Vector2(0, 20), new RadialColor(5, 2, 2)));
	}

	#endregion

	#region Methods

	public override void Load()
	{
		base.Load();
		_sim = new ConwayLifeSim(RC.Width, RC.Height, _config);
		_sim.Randomize();
	}

	public override void Unload()
	{
		_sim?.Dispose();
		_sim = null;
		base.Unload();
	}

	public override void Render(GameTime gameTime)
	{
		_sim?.Render(RC);
		base.Render(gameTime);
	}

	public override void Update(GameTime gameTime)
	{
		if (_sim == null)
		{
			return;
		}

		if (!_paused)
		{
			_timeSinceLastUpdate += (float)gameTime.ElapsedTime.TotalSeconds;
			float updateInterval = 1.0f / _config.UpdatesPerSecond;

			if (_timeSinceLastUpdate >= updateInterval)
			{
				_sim.Update(gameTime);
				_timeSinceLastUpdate = 0;
			}
		}

		base.Update(gameTime);
	}

	public override bool KeyDown(KeyboardKeyEventArgs e)
	{
		switch (e.Key)
		{
			case Keys.Escape:
				Leave();
				return true;
		}
		return base.KeyDown(e);
	}

	public override bool KeyUp(KeyboardKeyEventArgs e)
	{
		switch (e.Key)
		{
			case Keys.Tab:
				_sim?.SwapGenerators();
				return true;
			case Keys.R:
				_sim?.Randomize();
				return true;
			case Keys.Space:
				_paused = !_paused;
				return true;
			case Keys.Up:
				_config.UpdatesPerSecond = Math.Min(60, _config.UpdatesPerSecond + 1);
				return true;
			case Keys.Down:
				_config.UpdatesPerSecond = Math.Max(1, _config.UpdatesPerSecond - 1);
				return true;
			case Keys.W:
				_config.WrapEdges = !_config.WrapEdges;
				_sim?.Configure(_config);
				return true;
			case Keys.C:
				_sim?.Clear();
				return true;
		}
		return base.KeyUp(e);
	}

	public override bool MouseMove(MouseMoveEventArgs e)
	{
		_mousePosition = e.Position;
		return base.MouseMove(e);
	}

	public override bool MouseDown(MouseButtonEventArgs e)
	{
		return OnMouseButton(e) || base.MouseDown(e);
	}

	public override bool MouseUp(MouseButtonEventArgs e)
	{
		return OnMouseButton(e) || base.MouseUp(e);
	}

	private bool OnMouseButton(MouseButtonEventArgs e)
	{
		if (_sim == null)
		{
			return false;
		}

		if (e.Button == MouseButton.Left && e.Action == InputAction.Release)
		{
			var value = _sim.GetCell(_mousePosition);
			_sim.SetCell(_mousePosition, !value);
			return true;
		}
		else if (e.Button == MouseButton.Right && e.Action == InputAction.Release)
		{
			// Place a glider or other pattern at mouse position
			_sim.PlacePattern(_mousePosition);
			return true;
		}
		return false;
	}

	#endregion
}