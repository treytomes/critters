using Critters.Events;
using Critters.Gfx;
using Critters.Services;
using Critters.States.TileMapTest;
using Critters.UI;
using Critters.World;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

// Reference: https://bwiggs.com/projects/terrain/

namespace Critters.States;

class TileMapTestState : GameState
{
	#region Fields

	private Label _cameraLabel;
	private Label _mouseLabel;
	private Button _sampleButton;
	private IDisposable? _buttonClickSubscription;

	private Camera _camera;
	private TileRepo _tiles;
	private Level _level;
	private InfiniteTerrainGenerator _generator = new();
	private bool _isDraggingCamera = false;
	private Vector2 _cameraDelta = Vector2.Zero;
	private bool _cameraFastMove = false;

	/// <summary>
	/// Speed is measured in pixels per second.
	/// </summary>
	private float _cameraSpeed = 8 * 8; // 8 tiles per second

	private MapCursor _mapCursor = new();

	#endregion

	#region Constructors

	public TileMapTestState(IResourceManager resources, IRenderingContext rc)
		: base(resources, rc)
	{
		_camera = new Camera();

		_tiles = new TileRepo();
		_level = new Level(64, 8);

		_cameraLabel = new Label(resources, rc, $"Camera:({(int)_camera.Position.X},{(int)_camera.Position.Y})", new Vector2(0, 0), new RadialColor(5, 5, 5), new RadialColor(0, 0, 0));
		UI.Add(_cameraLabel);

		_mouseLabel = new Label(resources, rc, "Mouse:(0,0)", new Vector2(0, 8), new RadialColor(5, 5, 5), new RadialColor(0, 0, 0));
		UI.Add(_mouseLabel);

		_sampleButton = new Button(resources, rc, ButtonStyle.Raised);
		_sampleButton.Position = new Vector2(32, 32);
		_sampleButton.Content = new Label(resources, rc, "> Button <", new Vector2(0, 0), new RadialColor(0, 0, 0));
		UI.Add(_sampleButton);
	}

	#endregion

	#region Methods

	public override void Load()
	{
		base.Load();

		_tiles.Load(Resources);

		_level = Level.Load("sample.json");
		// _level = LevelBuilder.BuildSample();
		// _level.Save("sample.json");
	}

	public override void Unload()
	{
		_buttonClickSubscription?.Dispose();
		_buttonClickSubscription = null;

		base.Unload();
	}

	public override void AcquireFocus()
	{
		base.AcquireFocus();

		// Subscribe to button click events using Rx
		_buttonClickSubscription = _sampleButton.ClickEvents.Subscribe(OnButtonClicked);
	}

	public override void LostFocus()
	{
		// Dispose of the subscription when losing focus
		_buttonClickSubscription?.Dispose();
		_buttonClickSubscription = null;

		base.LostFocus();
	}

	public override void Render(GameTime gameTime)
	{
		RC.Clear();
		_camera.ViewportSize = RC.ViewportSize;

		const int GRID_SPACING = 16;
		const byte GRID_INTENSITY = 3;
		var gridColor = Palette.GetIndex(GRID_INTENSITY, GRID_INTENSITY, 0);
		var gridDeltaX = MathHelper.FloorDiv((int)_camera.Position.X, GRID_SPACING) % GRID_SPACING;
		var gridDeltaY = MathHelper.FloorDiv((int)_camera.Position.Y, GRID_SPACING) % GRID_SPACING;
		for (var y = -GRID_SPACING; y < RC.Height + GRID_SPACING; y += GRID_SPACING)
		{
			RC.RenderHLine(0, RC.Width - 1, y - gridDeltaY, gridColor);
		}
		for (var x = -GRID_SPACING; x < RC.Width + GRID_SPACING; x += GRID_SPACING)
		{
			RC.RenderVLine(x - gridDeltaX, 0, RC.Height - 1, gridColor);
		}

		_generator.UpdateViewPosition(_camera.Position);

		// _level?.Render(rc, _tiles, _camera);
		Render(_tiles, _camera);

		// Render the map cursor.
		_mapCursor.Render(RC, gameTime, _camera);

		base.Render(gameTime);
	}

	const int _tileSize = 8;
	public void Render(TileRepo tiles, Camera camera)
	{
		// Calculate visible tile range.
		var startPos = ((camera.Position - RC.ViewportSize / 2) / _tileSize).Floor() * _tileSize;

		// Render one extra tile around the edges.
		var endPos = startPos + RC.ViewportSize + Vector2.One * _tileSize;

		for (var y = startPos.Y; y < endPos.Y; y += _tileSize)
		{
			for (var x = startPos.X; x < endPos.X; x += _tileSize)
			{
				var tileX = x / _tileSize;
				var tileY = y / _tileSize;

				var screenPos = camera.WorldToScreen(new Vector2(x, y)).Floor();

				var tileType = _generator.GetTileAt((int)tileX, (int)tileY);
				var color = tileType switch
				{
					TerrainType.Water => new RadialColor(0, 0, 5),
					TerrainType.Sand => new RadialColor(4, 4, 0),
					TerrainType.Grass => new RadialColor(0, 5, 0),
					TerrainType.Hill => new RadialColor(2, 4, 2),
					TerrainType.Mountain => new RadialColor(3, 3, 3),
					_ => new RadialColor(5, 0, 5),
				};

				RC.RenderFilledRect(new Box2(screenPos, screenPos + new Vector2(_tileSize, _tileSize)), color.Index);
			}
		}
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);

		_camera.ScrollBy(_cameraDelta * (float)gameTime.ElapsedTime.TotalSeconds * _cameraSpeed * (_cameraFastMove ? 4 : 1));
		_cameraLabel.Text = StringProvider.From($"Camera:({(int)_camera.Position.X},{(int)_camera.Position.Y})");
	}

	public override bool KeyDown(KeyboardKeyEventArgs e)
	{
		_cameraFastMove = e.Shift;
		switch (e.Key)
		{
			case Keys.Escape:
				Leave();
				return true;
			case Keys.W:
				_cameraDelta = new Vector2(_cameraDelta.X, -1);
				return true;
			case Keys.S:
				_cameraDelta = new Vector2(_cameraDelta.X, 1);
				return true;
			case Keys.A:
				_cameraDelta = new Vector2(-1, _cameraDelta.Y);
				return true;
			case Keys.D:
				_cameraDelta = new Vector2(1, _cameraDelta.Y);
				return true;
		}
		return base.KeyDown(e);
	}

	public override bool KeyUp(KeyboardKeyEventArgs e)
	{
		_cameraFastMove = e.Shift;
		switch (e.Key)
		{
			case Keys.W:
			case Keys.S:
				_cameraDelta = new Vector2(_cameraDelta.X, 0);
				return true;
			case Keys.A:
			case Keys.D:
				_cameraDelta = new Vector2(0, _cameraDelta.Y);
				return true;
		}
		return base.KeyUp(e);
	}

	public override bool MouseMove(MouseMoveEventArgs e)
	{
		_mouseLabel.Text = StringProvider.From($"Mouse:({(int)e.Position.X},{(int)e.Position.Y})");

		if (_isDraggingCamera)
		{
			_camera.ScrollBy(-e.Delta);
		}

		var worldPos = _camera.ScreenToWorld(e.Position).Floor();
		const int TILE_SIZE = 8;
		var tilePos = (worldPos / TILE_SIZE).Floor();
		_mapCursor.MoveTo(tilePos * TILE_SIZE);
		return base.MouseMove(e);
	}

	// Changed method signature to accept just the event args
	private void OnButtonClicked(ButtonClickedEventArgs e)
	{
		Console.WriteLine($"Button pressed.");
		_camera.ScrollBy(new Vector2(8, 0));
	}

	#endregion
}