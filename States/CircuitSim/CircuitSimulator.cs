using Critters.Gfx;
using Critters.World;
using OpenTK.Mathematics;

namespace Critters.States.CircuitSim;

/// <summary>
/// Manages the circuit simulation
/// </summary>
class CircuitSimulator
{
	private ILevel _level;
	private Dictionary<(int, int), CircuitComponent> _components = new Dictionary<(int, int), CircuitComponent>();
	private HashSet<(int, int)> _activePositions = new HashSet<(int, int)>();
	private int _tileSize;

	// Track interactive components for input handling
	private Dictionary<(int, int), IInteractiveComponent> _interactiveComponents =
		new Dictionary<(int, int), IInteractiveComponent>();

	/// <summary>
	/// Gets the number of active components in the simulation
	/// </summary>
	public int ComponentCount => _components.Count;

	/// <summary>
	/// Creates a new circuit simulator
	/// </summary>
	/// <param name="level">Level to use for component placement</param>
	/// <param name="tileSize">Size of each tile in pixels</param>
	public CircuitSimulator(ILevel level, int tileSize)
	{
		_level = level;
		_tileSize = tileSize;
	}
	/// <summary>
	/// Updates all components in the circuit
	/// </summary>
	/// <param name="deltaTime">Time since last update in seconds</param>
	public void Update(float deltaTime)
	{
		// First update power sources
		foreach (var kvp in _components)
		{
			if (kvp.Value is PowerSource)
			{
				kvp.Value.Update(this, kvp.Key.Item1, kvp.Key.Item2, deltaTime);
			}
		}

		// Then update all other components
		// Make a copy of the keys to avoid modification during iteration
		var positions = new List<(int, int)>(_components.Keys);
		foreach (var pos in positions)
		{
			var component = _components[pos];
			if (!(component is PowerSource))
			{
				component.Update(this, pos.Item1, pos.Item2, deltaTime);
			}
		}

		// Update the level tiles to reflect component states
		foreach (var kvp in _components)
		{
			if (kvp.Value.IsDirty)
			{
				_level.SetTile(kvp.Key.Item1, kvp.Key.Item2, GetTileIdForComponent(kvp.Value));
				kvp.Value.IsDirty = false;
			}
		}
	}

	/// <summary>
	/// Gets a component at the specified position
	/// </summary>
	/// <param name="x">X position</param>
	/// <param name="y">Y position</param>
	/// <returns>The component at the position, or null if none exists</returns>
	public CircuitComponent? GetComponentAt(int x, int y)
	{
		if (_components.TryGetValue((x, y), out var component))
		{
			return component;
		}
		return null;
	}

	/// <summary>
	/// Places a component at the specified position
	/// </summary>
	/// <param name="x">X position</param>
	/// <param name="y">Y position</param>
	/// <param name="component">Component to place</param>
	public void PlaceComponent(int x, int y, CircuitComponent component)
	{
		_components[(x, y)] = component;
		_activePositions.Add((x, y));

		// Register interactive components
		if (component is IInteractiveComponent interactiveComponent)
		{
			_interactiveComponents[(x, y)] = interactiveComponent;
		}

		_level.SetTile(x, y, GetTileIdForComponent(component));
		component.IsDirty = false;
	}

	/// <summary>
	/// Removes a component at the specified position
	/// </summary>
	/// <param name="x">X position</param>
	/// <param name="y">Y position</param>
	/// <returns>True if a component was removed</returns>
	public bool RemoveComponent(int x, int y)
	{
		bool removed = _components.Remove((x, y));
		if (removed)
		{
			_activePositions.Remove((x, y));
			_interactiveComponents.Remove((x, y));
			_level.SetTile(x, y, 0); // Empty tile
		}
		return removed;
	}

	/// <summary>
	/// Maps a component to a tile ID for storage in the level
	/// </summary>
	/// <param name="component">Component to map</param>
	/// <returns>Tile ID representing the component</returns>
	private int GetTileIdForComponent(CircuitComponent component)
	{
		// Use tile IDs to represent different component types
		if (component is PowerSource)
			return 1;
		else if (component is Wire)
			return 2;
		else if (component is Ground)
			return 3;
		else if (component is Switch)
			return 4;
		else if (component is Button)
			return 5;
		else
			return 0;
	}

	/// <summary>
	/// Gets the charge at a specific position
	/// </summary>
	/// <param name="x">X position</param>
	/// <param name="y">Y position</param>
	/// <returns>Charge value or 0 if no component exists</returns>
	public float GetChargeAt(int x, int y)
	{
		var component = GetComponentAt(x, y);
		return component?.Charge ?? 0;
	}

	/// <summary>
	/// Renders all components in the circuit
	/// </summary>
	/// <param name="rc">Rendering context</param>
	/// <param name="camera">Camera for viewport transformation</param>
	public void Render(IRenderingContext rc, Camera camera)
	{
		// Calculate visible range
		var viewportSize = rc.ViewportSize;
		var startPos = ((camera.Position - viewportSize / 2) / _tileSize).Floor() * _tileSize;
		var endPos = startPos + viewportSize + Vector2.One * _tileSize;

		// Convert to tile coordinates
		int startTileX = (int)(startPos.X / _tileSize);
		int startTileY = (int)(startPos.Y / _tileSize);
		int endTileX = (int)(endPos.X / _tileSize) + 1;
		int endTileY = (int)(endPos.Y / _tileSize) + 1;

		// Render each component in the visible range
		for (int y = startTileY; y < endTileY; y++)
		{
			for (int x = startTileX; x < endTileX; x++)
			{
				var component = GetComponentAt(x, y);
				if (component != null)
				{
					Vector2 worldPos = new Vector2(x, y) * _tileSize;
					Vector2 screenPos = camera.WorldToScreen(worldPos);
					component.Render(rc, screenPos, _tileSize);
				}
			}
		}
	}

	/// <summary>
	/// Creates a sample circuit for demonstration
	/// </summary>
	public void CreateSampleCircuit_v1()
	{
		// Clear any existing components
		_components.Clear();
		_activePositions.Clear();

		// Create a simple circuit with power source, wires, and ground
		PlaceComponent(10, 10, new PowerSource());

		// Create a wire path
		for (int i = 0; i < 5; i++)
		{
			PlaceComponent(11 + i, 10, new Wire());
		}

		// Add some branches
		PlaceComponent(13, 9, new Wire());
		PlaceComponent(13, 8, new Wire());
		PlaceComponent(14, 8, new Wire());
		PlaceComponent(15, 8, new Wire());

		PlaceComponent(13, 11, new Wire());
		PlaceComponent(13, 12, new Wire());

		// Add grounds
		PlaceComponent(16, 10, new Ground());
		PlaceComponent(15, 8, new Ground());
		PlaceComponent(13, 12, new Ground());
	}

	/// <summary>
	/// Creates a sample circuit with interactive components
	/// </summary>
	public void CreateSampleCircuit()
	{
		// Clear any existing components
		_components.Clear();
		_activePositions.Clear();
		_interactiveComponents.Clear();

		// Create a simple circuit with power source, wires, switch, button, and ground
		PlaceComponent(10, 10, new PowerSource());

		// Create wire paths
		for (int i = 0; i < 3; i++)
		{
			PlaceComponent(11 + i, 10, new Wire());
		}

		// Add a switch
		PlaceComponent(14, 10, new Switch());

		// Continue wire path
		PlaceComponent(15, 10, new Wire());
		PlaceComponent(16, 10, new Wire());

		// Add a button branch
		PlaceComponent(13, 9, new Wire());
		PlaceComponent(13, 8, new Wire());
		PlaceComponent(13, 7, new Button());
		PlaceComponent(14, 7, new Wire());
		PlaceComponent(15, 7, new Wire());

		// Add grounds
		PlaceComponent(17, 10, new Ground());
		PlaceComponent(15, 7, new Ground());
	}

	/// <summary>
	/// Creates a component from a tile ID
	/// </summary>
	/// <param name="tileId">Tile ID from the level</param>
	/// <returns>A new component instance or null for empty tiles</returns>
	public CircuitComponent? CreateComponentFromTileId(int tileId)
	{
		switch (tileId)
		{
			case 1:
				return new PowerSource();
			case 2:
				return new Wire();
			case 3:
				return new Ground();
			case 4:
				return new Switch();
			case 5:
				return new Button();
			default:
				return null;
		}
	}

	/// <summary>
	/// Loads components from the level data
	/// </summary>
	public void LoadFromLevel()
	{
		_components.Clear();
		_activePositions.Clear();

		// Get level boundaries
		var (min, max) = _level.GetBoundaries();
		int minX = (int)(min.X / _tileSize);
		int minY = (int)(min.Y / _tileSize);
		int maxX = (int)(max.X / _tileSize) + 1;
		int maxY = (int)(max.Y / _tileSize) + 1;

		// Scan the level for components
		for (int y = minY; y < maxY; y++)
		{
			for (int x = minX; x < maxX; x++)
			{
				var tile = _level.GetTile(x, y);
				if (!tile.IsEmpty)
				{
					var component = CreateComponentFromTileId(tile.TileId);
					if (component != null)
					{
						_components[(x, y)] = component;
						_activePositions.Add((x, y));
					}
				}
			}
		}
	}

	/// <summary>
	/// Handles a mouse click at the specified world position
	/// </summary>
	/// <param name="worldPosition">Position in world coordinates</param>
	/// <returns>True if the click was handled by a component</returns>
	public bool HandleClick(Vector2 worldPosition)
	{
		int tileX = (int)(worldPosition.X / _tileSize);
		int tileY = (int)(worldPosition.Y / _tileSize);

		if (_interactiveComponents.TryGetValue((tileX, tileY), out var component))
		{
			return component.HandleClick(worldPosition);
		}

		return false;
	}

	/// <summary>
	/// Handles a mouse press at the specified world position
	/// </summary>
	/// <param name="worldPosition">Position in world coordinates</param>
	/// <returns>True if the press was handled by a component</returns>
	public bool HandlePress(Vector2 worldPosition)
	{
		int tileX = (int)(worldPosition.X / _tileSize);
		int tileY = (int)(worldPosition.Y / _tileSize);

		if (_interactiveComponents.TryGetValue((tileX, tileY), out var component))
		{
			return component.HandlePress(worldPosition);
		}

		return false;
	}

	/// <summary>
	/// Handles a mouse release at the specified world position
	/// </summary>
	/// <param name="worldPosition">Position in world coordinates</param>
	/// <returns>True if the release was handled by a component</returns>
	public bool HandleRelease(Vector2 worldPosition)
	{
		int tileX = (int)(worldPosition.X / _tileSize);
		int tileY = (int)(worldPosition.Y / _tileSize);

		if (_interactiveComponents.TryGetValue((tileX, tileY), out var component))
		{
			return component.HandleRelease(worldPosition);
		}

		return false;
	}
}
