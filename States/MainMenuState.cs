using Critters.Events;
using Critters.Gfx;
using Critters.IO;
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

	#endregion

	#region Constructors

	public MainMenuState(IResourceManager resources, IEventBus eventBus, IRenderingContext rc)
		: base(resources, eventBus, rc)
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
		};

		for (var n = 0; n < items.Length; n++)
		{
			var btn = new Button(null, Resources, EventBus, RC, new Vector2(32, 32 + n * 14), ButtonStyle.Raised);
			btn.Content = new Label(null, Resources, EventBus, RC, items[n], new Vector2(0, 0), new RadialColor(0, 0, 0));
			btn.Metadata = n;
			UI.Add(btn);
			_menuButtons.Add(btn);
		}

		base.Load();
	}

	public override void AcquireFocus()
	{
		base.AcquireFocus();

		foreach (var btn in _menuButtons)
		{
			btn.Clicked += OnButtonClicked;
		}

		EventBus.Subscribe<KeyEventArgs>(OnKey);
		EventBus.Subscribe<MouseMoveEventArgs>(OnMouseMove);
	}

	public override void LostFocus()
	{
		foreach (var btn in _menuButtons)
		{
			btn.Clicked -= OnButtonClicked;
		}
		EventBus.Unsubscribe<KeyEventArgs>(OnKey);
		EventBus.Unsubscribe<MouseMoveEventArgs>(OnMouseMove);

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
	}

	private void OnMouseMove(MouseMoveEventArgs e)
	{
		_hasMouseHover = _bounds.ContainsInclusive(e.Position);
	}

	private void OnButtonClicked(object? sender, ButtonClickedEventArgs e)
	{
		var btn = sender as Button;
		var metadata = (int)btn!.Metadata!;
		switch (metadata)
		{
			case 0:
				Enter(new TileMapTestState(Resources, EventBus, RC));
				break;
			case 1:
				Enter(new SimplexNoiseState(Resources, EventBus, RC));
				break;
			case 2:
				Enter(new HeatLampExperimentState(Resources, EventBus, RC));
				break;
			case 3:
				Enter(new ConwayLifeState(Resources, EventBus, RC));
				break;
			case 4:
				Enter(new FloatingConwayLifeState(Resources, EventBus, RC));
				break;
			case 5:
				Enter(new ParticlesState(Resources, EventBus, RC));
				break;
		}
	}

	#endregion
}