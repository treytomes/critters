using Critters.Events;
using Critters.Gfx;
using Critters.IO;
using Critters.Services;
using Critters.States.FloatingConwayLife;
using Critters.UI;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Critters.States;

class FloatingConwayLifeState : GameState
{
	#region Fields

	private Vector2 _mousePosition = Vector2.Zero;
	private ConwayLifeSim? _sim = null;

	#endregion

	#region Constructors

	public FloatingConwayLifeState(IResourceManager resources, IEventBus eventBus, IRenderingContext rc)
		: base(resources, eventBus, rc)
	{
		UI.Add(new Label(null, resources, eventBus, rc, () => _sim?.CAType ?? "N/A", new Vector2(0, 0), new RadialColor(5, 5, 5)));
	}

	#endregion

	#region Properties

	#endregion

	#region Methods

	public override void Load()
	{
		base.Load();
	}

	public override void Unload()
	{
		base.Unload();
	}

	public override void AcquireFocus()
	{
		base.AcquireFocus();

		EventBus.Subscribe<KeyEventArgs>(OnKey);
		EventBus.Subscribe<MouseMoveEventArgs>(OnMouseMove);
		EventBus.Subscribe<MouseButtonEventArgs>(OnMouseButton);
	}

	public override void LostFocus()
	{
		EventBus.Unsubscribe<KeyEventArgs>(OnKey);
		EventBus.Unsubscribe<MouseMoveEventArgs>(OnMouseMove);
		EventBus.Unsubscribe<MouseButtonEventArgs>(OnMouseButton);

		base.LostFocus();
	}

	public override void Render(GameTime gameTime)
	{
		if (_sim == null)
		{
			_sim = new ConwayLifeSim(RC.Width, RC.Height);
			_sim.Randomize();
		}

		_sim.Render(RC);

		base.Render(gameTime);
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
		}
	}

	#endregion
}