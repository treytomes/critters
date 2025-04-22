using System.Reactive.Disposables;
using System.Reactive.Linq;
using Critters.Events;
using Critters.Gfx;
using Critters.Services;
using Critters.UI;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Critters.States;

class MainMenuState : GameState
{
	#region Fields

	private bool _hasMouseHover = false;
	private Box2 _bounds = new Box2(32, 32, 128, 96);
	private List<Button> _menuButtons = new List<Button>();
	private CompositeDisposable _subscriptions = new CompositeDisposable();

	#endregion

	#region Constructors

	public MainMenuState(IResourceManager resources, IRenderingContext rc)
		: base(resources, rc)
	{
	}

	#endregion

	#region Methods

	public override void Load()
	{
		var items = new[] {
			"> Tile Map Test <",
			"> Simplex Noise <",
			">   Heat Lamp   <",
			">     Conway    <",
			">    FlConway   <",
			">   Particles   <",
			">    Terminal   <",
		};

		for (var n = 0; n < items.Length; n++)
		{
			var btn = new Button(Resources, RC, ButtonStyle.Raised);
			btn.Position = new Vector2(32, 32 + n * 16);
			btn.Content = new Label(Resources, RC, items[n], new Vector2(0, 0), new RadialColor(0, 0, 0));
			btn.Metadata = n;
			UI.Add(btn);
			_menuButtons.Add(btn);
		}

		base.Load();
	}

	public override void AcquireFocus()
	{
		base.AcquireFocus();

		// Clear any existing subscriptions
		_subscriptions.Clear();

		// Create new subscriptions for all buttons
		foreach (var btn in _menuButtons)
		{
			// Subscribe to ClickEvents observable
			var subscription = btn.ClickEvents.Subscribe(OnButtonClicked);

			// Add to composite disposable for easy cleanup
			_subscriptions.Add(subscription);
		}
	}

	public override void LostFocus()
	{
		// Dispose all button click subscriptions
		_subscriptions.Clear();

		base.LostFocus();
	}

	public override void Render(GameTime gameTime)
	{
		RC.Clear();

		var segments = 32;
		var dx = RC.Width / segments;
		var dy = RC.Height / segments;
		for (var n = 0; n < segments; n++)
		{
			var x1 = 0;
			var y1 = n * dy;
			var x2 = (segments - n) * dx;
			var y2 = 0;
			RC.RenderLine(x1, y1, x2, y2, RC.Palette[0, 5, 0]);
		}

		var fillColor = _hasMouseHover ? RC.Palette[0, 1, 4] : RC.Palette[0, 1, 3];
		var borderColor = _hasMouseHover ? RC.Palette[5, 5, 5] : RC.Palette[4, 4, 4];
		RC.RenderFilledRect(_bounds, fillColor);
		RC.RenderRect(_bounds, borderColor);

		RC.RenderCircle(290, 190, 24, RC.Palette[5, 4, 0]);
		RC.RenderFilledCircle(290, 190, 23, RC.Palette[3, 2, 0]);

		RC.FloodFill(new Vector2(310, 220), RC.Palette[1, 0, 2]);

		base.Render(gameTime);
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

	public override bool MouseMove(MouseMoveEventArgs e)
	{
		_hasMouseHover = _bounds.ContainsInclusive(e.Position);
		return base.MouseMove(e);
	}

	// Modified to accept ButtonClickedEventArgs directly
	private void OnButtonClicked(ButtonClickedEventArgs e)
	{
		var metadata = (int)e.Metadata!;
		switch (metadata)
		{
			case 0:
				Enter(new TileMapTestState(Resources, RC));
				break;
			case 1:
				Enter(new SimplexNoiseState(Resources, RC));
				break;
			case 2:
				Enter(new HeatLampExperimentState(Resources, RC));
				break;
			case 3:
				Enter(new ConwayLifeState(Resources, RC));
				break;
			case 4:
				Enter(new FloatingConwayLifeState(Resources, RC));
				break;
			case 5:
				Enter(new ParticlesState(Resources, RC));
				break;
			case 6:
				Enter(new TerminalGameState(Resources, RC));
				break;
		}
	}

	// Override Dispose to clean up subscriptions
	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_subscriptions.Dispose();
		}
		base.Dispose(disposing);
	}

	#endregion
}