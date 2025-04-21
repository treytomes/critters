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

	public FloatingConwayLifeState(IResourceManager resources, IRenderingContext rc)
		: base(resources, rc)
	{
		UI.Add(new Label(resources, rc, () => _sim?.CAType ?? "N/A", new Vector2(0, 0), new RadialColor(5, 5, 5)));
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
	}

	public override void LostFocus()
	{
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
			case Keys.R:
				_sim?.Randomize();
				return true;
		}
		return base.KeyUp(e);
	}

	public override bool MouseMove(MouseMoveEventArgs e)
	{
		_mousePosition = e.Position;
		return base.MouseMove(e);
	}

	#endregion
}