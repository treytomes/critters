using Critters.Events;
using Critters.Gfx;
using Critters.Services;
using Critters.States.CircuitSim;
using Critters.UI;
using Critters.World;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Critters.States;

/// <summary>
/// Game state for simulating and rendering logic circuits
/// </summary>
class CircuitSimGameState : GameState
{
	#region Constants

	public const int TILE_SIZE = 16;
	private const int CHUNK_SIZE = 64;
	private const int GRID_SPACING = 32;
	private const byte GRID_INTENSITY = 2;
	private const float BASE_CAMERA_SPEED = 8 * TILE_SIZE; // 8 tiles per second
	private const float FAST_CAMERA_MULTIPLIER = 4.0f;
	private const float SIMULATION_SPEED = 1.0f; // Normal speed multiplier

	#endregion

	#region Fields

	private Label _cameraLabel;
	private Label _mouseLabel;
	private Label _componentLabel;
	private Label _selectedComponentLabel;
	private Label _chargeLabel;
	private UI.Button _resetButton;
	private UI.Button _sampleCircuitButton;
	private IDisposable? _resetButtonClickSubscription;
	private IDisposable? _sampleButtonClickSubscription;
	private Vector2 _mousePosition = Vector2.Zero;

	private Camera _camera;
	private ILevel _level;
	private CircuitSimulator _simulator;
	private bool _isDraggingCamera;
	private Vector2 _cameraDelta = Vector2.Zero;
	private bool _cameraFastMove;
	private bool _simulationPaused = false;

	private CircuitMapCursor _mapCursor;

	// Track if we're handling a potential click
	private bool _isPotentialClick = false;
	private Vector2 _mouseDownPosition;
	private const float CLICK_DISTANCE_THRESHOLD = 5.0f; // Pixels

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a new instance of the CircuitSimGameState class
	/// </summary>
	/// <param name="resources">Resource manager for loading assets</param>
	/// <param name="rc">Rendering context for drawing</param>
	public CircuitSimGameState(IResourceManager resources, IRenderingContext rc)
		: base(resources, rc)
	{
		_camera = new Camera();
		_level = new Level(CHUNK_SIZE, TILE_SIZE);
		_simulator = new CircuitSimulator(_level, TILE_SIZE);
		_mapCursor = new CircuitMapCursor();

		// Initialize UI elements
		_cameraLabel = new Label(resources, rc, $"Camera:({(int)_camera.Position.X},{(int)_camera.Position.Y})",
			new Vector2(0, 0), new RadialColor(5, 5, 5), new RadialColor(0, 0, 0));
		UI.Add(_cameraLabel);

		_mouseLabel = new Label(resources, rc, "Mouse:(0,0)",
			new Vector2(0, 8), new RadialColor(5, 5, 5), new RadialColor(0, 0, 0));
		UI.Add(_mouseLabel);

		_componentLabel = new Label(resources, rc, "Components: 0",
			new Vector2(0, 16), new RadialColor(5, 5, 5), new RadialColor(0, 0, 0));
		UI.Add(_componentLabel);

		_selectedComponentLabel = new Label(resources, rc, "Selected: Wire",
			new Vector2(0, 24), new RadialColor(5, 5, 5), new RadialColor(0, 0, 0));
		UI.Add(_selectedComponentLabel);

		_chargeLabel = new Label(resources, rc, "Charge at cursor: 0",
			new Vector2(0, 32), new RadialColor(5, 5, 5), new RadialColor(0, 0, 0));
		UI.Add(_chargeLabel);

		_resetButton = new UI.Button(resources, rc, ButtonStyle.Raised);
		_resetButton.Position = new Vector2(rc.Width - 100, 8);
		_resetButton.Content = new Label(resources, rc, "Reset", new Vector2(0, 0), new RadialColor(5, 0, 0));
		UI.Add(_resetButton);

		_sampleCircuitButton = new UI.Button(resources, rc, ButtonStyle.Raised);
		_sampleCircuitButton.Position = new Vector2(rc.Width - 100, 32);
		_sampleCircuitButton.Content = new Label(resources, rc, "Sample Circuit", new Vector2(0, 0), new RadialColor(0, 5, 0));
		UI.Add(_sampleCircuitButton);
	}

	#endregion

	#region Methods

	/// <summary>
	/// Loads resources and initializes the state
	/// </summary>
	public override void Load()
	{
		base.Load();

		try
		{
			// Try to load the level from file, or create a new one if it fails
			try
			{
				_level = Level.Load("circuit.json");
				_simulator = new CircuitSimulator(_level, TILE_SIZE);
				_simulator.LoadFromLevel();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed to load circuit: {ex.Message}. Creating a new circuit.");
				_level = new Level(CHUNK_SIZE, TILE_SIZE);
				_simulator = new CircuitSimulator(_level, TILE_SIZE);
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error loading resources: {ex.Message}");
		}
	}

	/// <summary>
	/// Unloads resources and cleans up the state
	/// </summary>
	public override void Unload()
	{
		_resetButtonClickSubscription?.Dispose();
		_resetButtonClickSubscription = null;

		_sampleButtonClickSubscription?.Dispose();
		_sampleButtonClickSubscription = null;

		// Save the level before unloading
		try
		{
			if (_level.HasUnsavedChanges)
			{
				_level.Save("circuit.json");
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Failed to save circuit: {ex.Message}");
		}

		_level.Dispose();

		base.Unload();
	}

	/// <summary>
	/// Called when this state becomes the active state
	/// </summary>
	public override void AcquireFocus()
	{
		base.AcquireFocus();

		// Subscribe to button click events
		_resetButtonClickSubscription = _resetButton.ClickEvents.Subscribe(OnResetButtonClicked);
		_sampleButtonClickSubscription = _sampleCircuitButton.ClickEvents.Subscribe(OnSampleCircuitButtonClicked);
	}

	/// <summary>
	/// Called when this state is no longer the active state
	/// </summary>
	public override void LostFocus()
	{
		// Dispose of subscriptions when losing focus
		_resetButtonClickSubscription?.Dispose();
		_resetButtonClickSubscription = null;

		_sampleButtonClickSubscription?.Dispose();
		_sampleButtonClickSubscription = null;

		base.LostFocus();
	}

	/// <summary>
	/// Renders the state
	/// </summary>
	/// <param name="gameTime">Timing values for the current frame</param>
	public override void Render(GameTime gameTime)
	{
		RC.Clear();
		_camera.ViewportSize = RC.ViewportSize;

		// Render grid
		RenderGrid();

		// Render the circuit
		_simulator.Render(RC, _camera);

		// Render the map cursor
		_mapCursor.Render(RC, gameTime, _camera);

		// Render UI elements
		base.Render(gameTime);
	}

	/// <summary>
	/// Renders the background grid
	/// </summary>
	private void RenderGrid()
	{
		var gridColor = Palette.GetIndex(GRID_INTENSITY, GRID_INTENSITY, GRID_INTENSITY);
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
	}

	/// <summary>
	/// Updates the state
	/// </summary>
	/// <param name="gameTime">Timing values for the current frame</param>
	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);

		// Update camera position based on movement delta
		float cameraSpeed = BASE_CAMERA_SPEED * (_cameraFastMove ? FAST_CAMERA_MULTIPLIER : 1.0f);
		_camera.ScrollBy(_cameraDelta * (float)gameTime.ElapsedTime.TotalSeconds * cameraSpeed);

		// Update camera position display
		_cameraLabel.Text = StringProvider.From($"Camera:({(int)_camera.Position.X},{(int)_camera.Position.Y})");

		// Update component count display
		_componentLabel.Text = StringProvider.From($"Components: {_simulator.ComponentCount}");

		// Update selected component display
		_selectedComponentLabel.Text = StringProvider.From($"Selected: {GetSelectedComponentName()}");

		_chargeLabel.Text = StringProvider.From($"Charge at cursor: {ChargeAtMouse()}");

		// Update the circuit simulation if not paused
		if (!_simulationPaused)
		{
			_simulator.Update((float)gameTime.ElapsedTime.TotalSeconds * SIMULATION_SPEED);
		}
	}

	private float ChargeAtMouse()
	{
		var worldPos = _camera.ScreenToWorld(_mousePosition).Floor();
		var tileX = (int)(worldPos.X / TILE_SIZE);
		var tileY = (int)(worldPos.Y / TILE_SIZE);
		return _simulator.GetChargeAt(tileX, tileY);
	}

	/// <summary>
	/// Gets the name of the currently selected component type
	/// </summary>
	private string GetSelectedComponentName()
	{
		if (_mapCursor.SelectedComponentType == typeof(Wire))
			return "Wire";
		else if (_mapCursor.SelectedComponentType == typeof(PowerSource))
			return "Power Source";
		else if (_mapCursor.SelectedComponentType == typeof(Ground))
			return "Ground";
		else if (_mapCursor.SelectedComponentType == typeof(Switch))
			return "Switch";
		else if (_mapCursor.SelectedComponentType == typeof(CircuitSim.Button))
			return "Button";
		else
			return "Unknown";
	}

	/// <summary>
	/// Handles key down events
	/// </summary>
	/// <param name="e">Key event arguments</param>
	/// <returns>True if the event was handled; otherwise, false</returns>
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
			case Keys.Space:
				_simulationPaused = !_simulationPaused;
				return true;
			case Keys.Tab:
				_mapCursor.CycleComponentType();
				return true;
			case Keys.Delete:
			case Keys.Backspace:
				// Delete component under cursor
				int tileX = (int)(_mapCursor.Position.X / TILE_SIZE);
				int tileY = (int)(_mapCursor.Position.Y / TILE_SIZE);
				_simulator.RemoveComponent(tileX, tileY);
				return true;
			case Keys.R:
				// Reset all components
				ResetCircuit();
				return true;
		}

		return base.KeyDown(e);
	}

	/// <summary>
	/// Handles key up events
	/// </summary>
	/// <param name="e">Key event arguments</param>
	/// <returns>True if the event was handled; otherwise, false</returns>
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

	/// <summary>
	/// Handles mouse move events
	/// </summary>
	/// <param name="e">Mouse move event arguments</param>
	/// <returns>True if the event was handled; otherwise, false</returns>
	public override bool MouseMove(MouseMoveEventArgs e)
	{
		_mousePosition = e.Position;

		// Update mouse position display
		_mouseLabel.Text = StringProvider.From($"Mouse:({(int)e.Position.X},{(int)e.Position.Y})");

		// Handle camera dragging
		if (_isDraggingCamera)
		{
			_camera.ScrollBy(-e.Delta);
		}

		// Update map cursor position
		var worldPos = _camera.ScreenToWorld(e.Position).Floor();
		var tilePos = (worldPos / TILE_SIZE).Floor();
		_mapCursor.MoveTo(tilePos * TILE_SIZE);

		return base.MouseMove(e);
	}

	/// <summary>
	/// Handles mouse down events
	/// </summary>
	/// <param name="e">Mouse button event arguments</param>
	/// <returns>True if the event was handled; otherwise, false</returns>
	public override bool MouseDown(MouseButtonEventArgs e)
	{
		if (base.MouseDown(e))
		{
			// Give the UI a chance to handle the event first
			return true;
		}

		if (e.Button == MouseButton.Right)
		{
			_isDraggingCamera = true;
			return true;
		}
		else if (e.Button == MouseButton.Left)
		{
			// Get the world position under the mouse
			var worldPos = _camera.ScreenToWorld(_mousePosition);

			// Store mouse position to check if this is a click later
			_mouseDownPosition = _mousePosition;
			_isPotentialClick = true;

			// Handle press event for interactive components
			if (_simulator.HandlePress(worldPos))
			{
				return true;
			}

			// If not interacting with an existing component, place a new one
			var tileX = (int)(worldPos.X / TILE_SIZE);
			var tileY = (int)(worldPos.Y / TILE_SIZE);

			// Place the selected component
			var component = _mapCursor.CreateSelectedComponent();
			_simulator.PlaceComponent(tileX, tileY, component);
			return true;
		}
		else if (e.Button == MouseButton.Middle)
		{
			// Cycle through component types
			_mapCursor.CycleComponentType();
			return true;
		}

		return false;
	}

	/// <summary>
	/// Handles mouse up events
	/// </summary>
	/// <param name="e">Mouse button event arguments</param>
	/// <returns>True if the event was handled; otherwise, false</returns>
	public override bool MouseUp(MouseButtonEventArgs e)
	{
		if (base.MouseUp(e))
		{
			return true;
		}

		if (e.Button == MouseButton.Right)
		{
			_isDraggingCamera = false;
			return true;
		}
		else if (e.Button == MouseButton.Left)
		{
			// Get the world position under the mouse
			var worldPos = _camera.ScreenToWorld(_mousePosition);

			// Check if this is a click (mouse didn't move much between down and up)
			bool isClick = _isPotentialClick &&
				Vector2.Distance(_mouseDownPosition, _mousePosition) < CLICK_DISTANCE_THRESHOLD;

			// Reset click tracking
			_isPotentialClick = false;

			// If it's a click, handle it first (for switches)
			if (isClick)
			{
				if (_simulator.HandleClick(worldPos))
				{
					return true;
				}
			}

			// Then handle the release event (for buttons)
			if (_simulator.HandleRelease(worldPos))
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Handles reset button click events
	/// </summary>
	private void OnResetButtonClicked(ButtonClickedEventArgs e)
	{
		ResetCircuit();
	}

	/// <summary>
	/// Resets the circuit by clearing all components
	/// </summary>
	private void ResetCircuit()
	{
		_level.Clear();
		_simulator = new CircuitSimulator(_level, TILE_SIZE);
	}

	/// <summary>
	/// Handles sample circuit button click events
	/// </summary>
	private void OnSampleCircuitButtonClicked(ButtonClickedEventArgs e)
	{
		_simulator.CreateSampleCircuit();

		// Center the camera on the sample circuit
		_camera.Position = new Vector2(13 * TILE_SIZE, 10 * TILE_SIZE);
	}

	/// <summary>
	/// Handles mouse wheel events
	/// </summary>
	/// <param name="e">Mouse wheel event arguments</param>
	/// <returns>True if the event was handled; otherwise, false</returns>
	public override bool MouseWheel(MouseWheelEventArgs e)
	{
		if (base.MouseWheel(e))
		{
			return true;
		}

		// Use mouse wheel to cycle through component types
		if (e.OffsetY > 0)
		{
			_mapCursor.CycleComponentType();
			return true;
		}

		return false;
	}

	#endregion
}
