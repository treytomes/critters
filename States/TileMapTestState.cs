using Critters.Events;
using Critters.Gfx;
using Critters.IO;
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

	public TileMapTestState()
		: base()
	{
		_camera = new Camera();
		
		_tiles = new TileRepo();
		_level = new Level(64, 8);

		_cameraLabel = new Label($"Camera:({(int)_camera.Position.X},{ (int)_camera.Position.Y})", new Vector2(0, 0), new RadialColor(5, 5, 5), new RadialColor(0, 0, 0));
		UI.Add(_cameraLabel);
		
		_mouseLabel = new Label($"Mouse:(0,0)", new Vector2(0, 8), new RadialColor(5, 5, 5), new RadialColor(0, 0, 0));
		UI.Add(_mouseLabel);

		_sampleButton = new Button(new Vector2(32, 32), ButtonStyle.Raised);
		_sampleButton.Content = new Label("> Button <", new Vector2(0, 0), new RadialColor(0, 0, 0));
		UI.Add(_sampleButton);
	}

	#endregion

	#region Methods
	
	public override void Load(IResourceManager resources, IEventBus eventBus)
	{
		base.Load(resources, eventBus);

		_tiles.Load(resources, eventBus);

		_level = Level.Load("sample.json");
		// _level = LevelBuilder.BuildSample();
		// _level.Save("sample.json");
	}

	public override void Unload(IResourceManager resources, IEventBus eventBus)
	{
		base.Unload(resources, eventBus);
	}

	public override void AcquireFocus(IEventBus eventBus)
	{
		base.AcquireFocus(eventBus);

		_sampleButton.Clicked += OnButtonClicked;

		eventBus.Subscribe<KeyEventArgs>(OnKey);
		eventBus.Subscribe<MouseMoveEventArgs>(OnMouseMove);
		eventBus.Subscribe<MouseButtonEventArgs>(OnMouseButton);
	}

	public override void LostFocus(IEventBus eventBus)
	{
		_sampleButton.Clicked -= OnButtonClicked;

		eventBus.Unsubscribe<KeyEventArgs>(OnKey);
		eventBus.Unsubscribe<MouseMoveEventArgs>(OnMouseMove);
		eventBus.Unsubscribe<MouseButtonEventArgs>(OnMouseButton);

		base.LostFocus(eventBus);
	}

	public override void Render(RenderingContext rc, GameTime gameTime)
	{
		rc.Clear();
		_camera.ViewportSize = rc.ViewportSize;

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

		_generator.UpdateViewPosition(_camera.Position);

		// _level?.Render(rc, _tiles, _camera);
		Render(rc, _tiles, _camera);

		// Render the map cursor.
		_mapCursor.Render(rc, gameTime, _camera);

		base.Render(rc, gameTime);
	}
	
	const int _tileSize = 8;
	public void Render(RenderingContext rc, TileRepo tiles, Camera camera)
	{
		// Calculate visible tile range.
		var startPos = ((camera.Position - rc.ViewportSize / 2) / _tileSize).Floor() * _tileSize;

		// Render one extra tile around the edges.
		var endPos = startPos + rc.ViewportSize + Vector2.One * _tileSize;

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

				rc.RenderFilledRect(new Box2(screenPos, screenPos + new Vector2(_tileSize, _tileSize)), color.Index);
				
				// var tileRef = GetTile(tileX, tileY);
				// if (!tileRef.IsEmpty)
				// {
				// 	tiles.Get(tileRef.TileId).Render(rc, screenPos); // Render the tile at the correct screen position.
				// }
			}
		}
	}


	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);
		
		_camera.ScrollBy(_cameraDelta * (float)gameTime.ElapsedTime.TotalSeconds * _cameraSpeed * (_cameraFastMove ? 4 : 1));
		_cameraLabel.Text = StringProvider.From($"Camera:({(int)_camera.Position.X},{ (int)_camera.Position.Y})");
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
		_mouseLabel.Text = StringProvider.From($"Mouse:({(int)e.Position.X},{(int)e.Position.Y})");

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