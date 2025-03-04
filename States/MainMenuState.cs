using Critters.Events;
using Critters.Gfx;
using Critters.IO;
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
	private List<UIElement> _ui = new List<UIElement>();
	private List<Button> _menuButtons = new List<Button>();

	#endregion

	#region Constructors

	public MainMenuState()
	{
	}

	#endregion

	#region Methods

	public override void Load(ResourceManager resources, EventBus eventBus)
	{
		base.Load(resources, eventBus);

		{
			var btn = new Button(new Vector2(32, 32), ButtonStyle.Raised);
			btn.Content = new Label("> Tile Map Test <", new Vector2(0, 0), new RadialColor(0, 0, 0));
			_ui.Add(btn);
			_menuButtons.Add(btn);
		}

		foreach (var ui in _ui)
		{
			ui.Load(resources, eventBus);
		}
	}

	public override void Unload(ResourceManager resources, EventBus eventBus)
	{
		base.Unload(resources, eventBus);

		foreach (var ui in _ui)
		{
			ui.Unload(resources, eventBus);
		}
	}

	public override void AcquireFocus(EventBus eventBus)
	{
		base.AcquireFocus(eventBus);

		foreach (var btn in _menuButtons)
		{
			btn.Clicked += OnButtonClicked;
		}

		eventBus.Subscribe<KeyEventArgs>(OnKey);
		eventBus.Subscribe<MouseMoveEventArgs>(OnMouseMove);
	}

	public override void LostFocus(EventBus eventBus)
	{
		foreach (var btn in _menuButtons)
		{
			btn.Clicked -= OnButtonClicked;
		}
		eventBus.Unsubscribe<KeyEventArgs>(OnKey);
		eventBus.Unsubscribe<MouseMoveEventArgs>(OnMouseMove);

		base.LostFocus(eventBus);
	}

	public override void Render(RenderingContext rc, GameTime gameTime)
	{
		base.Render(rc, gameTime);
		
		rc.Clear();

		var segments = 32;
		var dx = rc.Width / segments;
		var dy = rc.Height / segments;
		for (var n = 0; n < segments; n++)
		{
			var x1 = 0;
			var y1 = n * dy;
			var x2 = (segments - n) * dx;
			var y2 = 0;
			rc.RenderLine(x1, y1, x2, y2, rc.Palette[0, 5, 0]);
		}

		var fillColor = _hasMouseHover ? rc.Palette[0, 1, 4] : rc.Palette[0, 1, 3];
		var borderColor = _hasMouseHover ? rc.Palette[5, 5, 5] : rc.Palette[4, 4, 4];
		rc.RenderFilledRect(_bounds, fillColor);
		rc.RenderRect(_bounds, borderColor);

		rc.RenderCircle(290, 190, 24, rc.Palette[5, 4, 0]);
		rc.RenderFilledCircle(290, 190, 23, rc.Palette[3, 2, 0]);

		rc.FloodFill(new Vector2(310, 220), rc.Palette[1, 0, 2]);

		foreach (var ui in _ui)
		{
			ui.Render(rc, gameTime);
		}
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);
		
		foreach (var ui in _ui)
		{
			ui.Update(gameTime);
		}
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
		Enter(new TileMapTestState());
	}

	#endregion
}