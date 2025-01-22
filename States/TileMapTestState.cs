using Critters.Events;
using Critters.Gfx;
using Critters.IO;
using Critters.UI;
using Critters.World;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;

namespace Critters.States;

class TileMapTestState : GameState
{
	#region Fields

	private Label _cameraLabel;
	private Label _mouseLabel;
	private List<UIElement> _ui = new List<UIElement>();
	private Camera _camera;
	private Level? _level = null;

	private int _buttonId;

	#endregion

	#region Constructors

	public TileMapTestState()
	{
		_camera = new Camera();
		
		_cameraLabel = new Label($"Camera:({(int)_camera.Position.X},{ (int)_camera.Position.Y})", new Vector2(0, 0), Palette.GetIndex(5, 5, 5), Palette.GetIndex(0, 0, 0));
		_ui.Add(_cameraLabel);
		
		_mouseLabel = new Label($"Mouse:(0,0)", new Vector2(0, 8), Palette.GetIndex(5, 5, 5), Palette.GetIndex(0, 0, 0));
		_ui.Add(_mouseLabel);

		var button = new Button(new Vector2(32, 32));
		var buttonLabel = new Label("Button", new Vector2(0, 0), Palette.GetIndex(0, 0, 0), 255);
		button.Content = buttonLabel;
		_buttonId = button.Id;

		_ui.Add(button);
	}

	public override void Load(ResourceManager resources, EventBus eventBus)
	{
		var tiles = new TileRepo();
		tiles.Load(resources, eventBus);

		_level = new Level(64, 8);
		for (var y = -64; y <= 64; y++)
		{
			for (var x = -64; x <= 64; x++)
			{
				if ((x % 8 == 0) || (y % 8 == 0))
				{
					_level.SetTile(x, y, tiles.Dirt);
				}
				else
				{
					_level.SetTile(x, y, tiles.Grass);
				}
			}
		}

		foreach (var ui in _ui)
		{
			ui.Load(resources, eventBus);
		}

		eventBus.Subscribe<MouseMoveEventArgs>(OnMouseMove);
		eventBus.Subscribe<ButtonPressedEventArgs>(OnButtonPressed);
	}

	public override void Unload(ResourceManager resources, EventBus eventBus)
	{
		foreach (var ui in _ui)
		{
			ui.Unload(resources, eventBus);
		}

		eventBus.Unsubscribe<MouseMoveEventArgs>(OnMouseMove);
		eventBus.Unsubscribe<ButtonPressedEventArgs>(OnButtonPressed);
	}

	public override void Render(RenderingContext rc, GameTime gameTime)
	{
		rc.Clear();

		_level?.Render(rc, _camera);

		foreach (var ui in _ui)
		{
			ui.Render(rc, gameTime);
		}
	}

	public override void Update(GameTime gameTime)
	{
		foreach (var ui in _ui)
		{
			ui.Update(gameTime);
		}
	}

	private void OnMouseMove(MouseMoveEventArgs e)
	{
		_mouseLabel.Text = $"Mouse:({(int)e.Position.X},{(int)e.Position.Y})";
	}

	private void OnButtonPressed(ButtonPressedEventArgs e)
	{
		if (e.ButtonId == _buttonId)
		{
			Console.WriteLine($"Button {e.ButtonId} pressed.");
			_camera.ScrollBy(new Vector2(8, 0));
		}
	}

	#endregion
}