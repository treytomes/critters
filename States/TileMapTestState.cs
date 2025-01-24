using Critters.Events;
using Critters.Gfx;
using Critters.IO;
using Critters.UI;
using Critters.World;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Critters.States;

class MapCursor
{
	#region Constants

	private const int MAP_CURSOR_BLINK_SPEED_MS = 300;

	#endregion

	#region Fields

	private Vector2 _position = Vector2.Zero;

	#endregion

	#region Methods

	public void Update(GameTime gameTime)
	{
	}

	public void Render(RenderingContext rc, GameTime gameTime, Camera camera)
	{
		var mapCursorScreenPosition = camera.WorldToScreen(_position);
		rc.RenderRect(mapCursorScreenPosition, mapCursorScreenPosition + new Vector2(7, 7), rc.Palette[5, 0, 0]);
		if ((int)(gameTime.TotalTime.TotalMilliseconds / MAP_CURSOR_BLINK_SPEED_MS) % 2 == 0)
		{
			rc.RenderRect(mapCursorScreenPosition - new Vector2(2, 2), mapCursorScreenPosition + new Vector2(7, 7) + new Vector2(2, 2), rc.Palette[5, 0, 0]);
		}
	}

	public void MoveTo(Vector2 position)
	{
		_position = position;
	}

	public void MoveBy(Vector2 delta)
	{
		_position += delta;
	}

	#endregion
}

class TileMapTestState : GameState
{
	#region Fields

	private Label _cameraLabel;
	private Label _mouseLabel;
	private Button _sampleButton;

	private List<UIElement> _ui = new List<UIElement>();
	private Camera _camera;
	private TileRepo _tiles;
	private Level _level;
	private bool _isDraggingCamera = false;
	private Vector2 _cameraDelta = Vector2.Zero;
	private bool _cameraFastMove = false;

	/// <summary>
	/// Speed is measured in pixels per second.
	/// </summary>
	private float _cameraSpeed = 8 * 8; // 8 tiles per second

	private MapCursor _mapCursor = new MapCursor();

	#endregion

	#region Constructors

	public TileMapTestState()
		: base()
	{
		_camera = new Camera(new Vector2(320, 240));
		
		_tiles = new TileRepo();
		_level = new Level(64, 8);

		_cameraLabel = new Label($"Camera:({(int)_camera.Position.X},{ (int)_camera.Position.Y})", new Vector2(0, 0), Palette.GetIndex(5, 5, 5), Palette.GetIndex(0, 0, 0));
		_ui.Add(_cameraLabel);
		
		_mouseLabel = new Label($"Mouse:(0,0)", new Vector2(0, 8), Palette.GetIndex(5, 5, 5), Palette.GetIndex(0, 0, 0));
		_ui.Add(_mouseLabel);

		_sampleButton = new Button(new Vector2(32, 32), ButtonStyle.Raised);
		_sampleButton.Content = new Label("> Button <", new Vector2(0, 0), Palette.GetIndex(0, 0, 0), 255);
		_ui.Add(_sampleButton);
	}

	#endregion

	#region Methods
	
	public override void Load(ResourceManager resources, EventBus eventBus)
	{
		base.Load(resources, eventBus);

		_tiles.Load(resources, eventBus);

		_level = Level.Load("sample.json");
		// _level = LevelBuilder.BuildSample();
		// _level.Save("sample.json");

		_sampleButton.Clicked += OnButtonClicked;

		foreach (var ui in _ui)
		{
			ui.Load(resources, eventBus);
		}

		eventBus.Subscribe<KeyEventArgs>(OnKey);
		eventBus.Subscribe<MouseMoveEventArgs>(OnMouseMove);
		eventBus.Subscribe<MouseButtonEventArgs>(OnMouseButton);
	}

	public override void Unload(ResourceManager resources, EventBus eventBus)
	{
		base.Unload(resources, eventBus);

		_sampleButton.Clicked -= OnButtonClicked;

		foreach (var ui in _ui)
		{
			ui.Unload(resources, eventBus);
		}

		eventBus.Unsubscribe<KeyEventArgs>(OnKey);
		eventBus.Unsubscribe<MouseMoveEventArgs>(OnMouseMove);
		eventBus.Unsubscribe<MouseButtonEventArgs>(OnMouseButton);
	}

	public override void Render(RenderingContext rc, GameTime gameTime)
	{
		base.Render(rc, gameTime);

		rc.Clear();

		const int GRID_SPACING = 16;
		const byte GRID_INTENSITY = 3;
		var gridColor = Palette.GetIndex(GRID_INTENSITY, GRID_INTENSITY, 0);
		var gridDeltaX = MathHelper.FloorDiv((int)_camera.Position.X, GRID_SPACING) % GRID_SPACING;
		var gridDeltaY = MathHelper.FloorDiv((int)_camera.Position.Y, GRID_SPACING) % GRID_SPACING;
		for (var y = -GRID_SPACING; y < rc.Height + GRID_SPACING; y += GRID_SPACING)
		{
			rc.RenderHLine(0, rc.Width - 1, y - gridDeltaY, gridColor);
		}
		for (var x = -GRID_SPACING; x < rc.Width + GRID_SPACING; x += GRID_SPACING)
		{
			rc.RenderVLine(x - gridDeltaX, 0, rc.Height - 1, gridColor);
		}

		_level?.Render(rc, _tiles, _camera);

		// Render the map cursor.
		_mapCursor.Render(rc, gameTime, _camera);

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
		_camera.ScrollBy(_cameraDelta * (float)gameTime.ElapsedTime.TotalSeconds * _cameraSpeed * (_cameraFastMove ? 4 : 1));
		_cameraLabel.Text = $"Camera:({(int)_camera.Position.X},{ (int)_camera.Position.Y})";
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
				case Keys.W:
					_cameraDelta = new Vector2(_cameraDelta.X, -1);
					break;
				case Keys.S:
					_cameraDelta = new Vector2(_cameraDelta.X, 1);
					break;
				case Keys.A:
					_cameraDelta = new Vector2(-1, _cameraDelta.Y);
					break;
				case Keys.D:
					_cameraDelta = new Vector2(1, _cameraDelta.Y);
					break;
			}
		}
		else
		{
			switch (e.Key)
			{
				case Keys.W:
				case Keys.S:
					_cameraDelta = new Vector2(_cameraDelta.X, 0);
					break;
				case Keys.A:
				case Keys.D:
					_cameraDelta = new Vector2(0, _cameraDelta.Y);
					break;
			}
		}

		_cameraFastMove = e.Shift;
	}

	private void OnMouseMove(MouseMoveEventArgs e)
	{
		_mouseLabel.Text = $"Mouse:({(int)e.Position.X},{(int)e.Position.Y})";

		if (_isDraggingCamera)
		{
			_camera.ScrollBy(-e.Delta);
		}

		var worldPos = _camera.ScreenToWorld(e.Position).Floor();
		const int TILE_SIZE = 8;
		var tilePos = (worldPos / TILE_SIZE).Floor();
		_mapCursor.MoveTo(tilePos * TILE_SIZE);
	}

	private void OnMouseButton(MouseButtonEventArgs e)
	{
		if (e.Button == MouseButton.Middle)
		{
			_isDraggingCamera = e.IsPressed;
		}
	}

	private void OnButtonClicked(object? sender, ButtonClickedEventArgs e)
	{
		Console.WriteLine($"Button pressed.");
		_camera.ScrollBy(new Vector2(8, 0));
	}

	#endregion
}