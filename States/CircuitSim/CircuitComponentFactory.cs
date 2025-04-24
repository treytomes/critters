using System.Reflection.Metadata;

namespace Critters.States.CircuitSim;

static class CircuitComponentFactory
{
	public static readonly Type[] TYPES =
	[
		typeof(PowerSource),
		typeof(Wire),
		typeof(Ground),
		typeof(Switch),
		typeof(Button),
		typeof(Resistor),
		typeof(Capacitor),
		typeof(Transistor),
	];

	/// <summary>
	/// Creates a component from a tile ID
	/// </summary>
	/// <param name="tileId">Tile ID from the level</param>
	/// <returns>A new component instance or null for empty tiles</returns>
	public static CircuitComponent? CreateComponentFromTileId(int tileId)
	{
		// TODO: Maybe set Id as a property of CircuitComponent?
		var type = TYPES[tileId];
		return Activator.CreateInstance(type) as CircuitComponent;
	}

	/// <summary>
	/// Creates a new component of the selected type
	/// </summary>
	/// <returns>A new component instance</returns>
	public static CircuitComponent CreateComponentFromType(Type type)
	{
		return (Activator.CreateInstance(type) as CircuitComponent) ?? throw new ArgumentException("Type is not a circuit component.", nameof(type));
	}

	/// <summary>
	/// Maps a component to a tile ID for storage in the level
	/// </summary>
	/// <param name="component">Component to map</param>
	/// <returns>Tile ID representing the component</returns>
	public static int GetTileIdForComponent(CircuitComponent component)
	{
		return GetTileIdForComponentType(component.GetType());
	}

	public static int GetTileIdForComponentType(Type type)
	{
		var index = Array.IndexOf(TYPES, type);
		if (index < 0) index = 0;
		return index;
	}
}