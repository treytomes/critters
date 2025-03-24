using Critters.Events;
using Critters.Gfx;
using Critters.IO;
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

	#endregion

	#region Constructors

	public ConwayLifeState()
		: base()
	{
		UI.Add(new Label(() => _sim?.CAType ?? "N/A", new Vector2(0, 0), new RadialColor(5, 5, 5)));
	}

	#endregion

	#region Properties

	#endregion

	#region Methods
	
	public override void Load(ResourceManager resources, EventBus eventBus)
	{
		base.Load(resources, eventBus);
	}

	public override void Unload(ResourceManager resources, EventBus eventBus)
	{
		base.Unload(resources, eventBus);
	}

	public override void AcquireFocus(EventBus eventBus)
	{
		base.AcquireFocus(eventBus);

		eventBus.Subscribe<KeyEventArgs>(OnKey);
		eventBus.Subscribe<MouseMoveEventArgs>(OnMouseMove);
		eventBus.Subscribe<MouseButtonEventArgs>(OnMouseButton);
	}

	public override void LostFocus(EventBus eventBus)
	{
		eventBus.Unsubscribe<KeyEventArgs>(OnKey);
		eventBus.Unsubscribe<MouseMoveEventArgs>(OnMouseMove);
		eventBus.Unsubscribe<MouseButtonEventArgs>(OnMouseButton);

		base.LostFocus(eventBus);
	}

	public override void Render(RenderingContext rc, GameTime gameTime)
	{
		if (_sim == null)
		{
			_sim = new ConwayLifeSim(rc.Width, rc.Height);
			_sim.Randomize();
		}

		_sim.Render(rc);

		base.Render(rc, gameTime);
	}

	public override void Update(GameTime gameTime)
	{
		_sim?.Update(gameTime);

		base.Update(gameTime);
	}

	private void OnKey(KeyEventArgs e)
	{
		if (e.IsPressed)
		{
			switch (e.Key)
			{
				case Keys.Escape:
					Leave();
					break;
			}
		}
		else
		{
			switch (e.Key)
			{
				case Keys.Tab:
					_sim?.SwapGenerators();
					break;
				case Keys.R:
					_sim?.Randomize();
					break;
			}
		}
	}

	private void OnMouseMove(MouseMoveEventArgs e)
	{
		_mousePosition = e.Position;
	}

	private void OnMouseButton(MouseButtonEventArgs e)
	{
		if (e.Button == MouseButton.Left && e.Action == InputAction.Release)
		{
			if (_sim == null)
			{
				return;
			}
			var value = _sim.GetCell(_mousePosition);
			_sim.SetCell(_mousePosition, !value);
		}
	}

	#endregion
}