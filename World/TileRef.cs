using Newtonsoft.Json;
using System;

namespace Critters.World;

/// <summary>
/// Represents an immutable reference to a Tile in a Chunk.
/// </summary>
public readonly struct TileRef : IEquatable<TileRef>
{
	#region Fields

	/// <summary>
	/// Represents an empty tile reference (points to no tile).
	/// </summary>
	public static readonly TileRef Empty = new TileRef(0);

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a new instance of the <see cref="TileRef"/> struct.
	/// </summary>
	/// <param name="tileId">The ID of the tile this reference points to. Must be non-negative.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when tileId is negative.</exception>
	public TileRef(int tileId)
	{
		if (tileId < 0)
			throw new ArgumentOutOfRangeException(nameof(tileId), "Tile ID cannot be negative");

		TileId = tileId;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets the ID of the tile this reference points to. A value of 0 indicates an empty tile.
	/// </summary>
	public int TileId { get; }

	/// <summary>
	/// Gets a value indicating whether this tile reference is empty (points to no tile).
	/// </summary>
	[JsonIgnore]
	public bool IsEmpty => TileId == 0;

	#endregion

	#region Methods

	/// <summary>
	/// Creates a new TileRef with the specified tile ID.
	/// </summary>
	/// <param name="tileId">The ID of the tile to reference.</param>
	/// <returns>A new TileRef instance.</returns>
	public static TileRef Create(int tileId) => new TileRef(tileId);

	/// <summary>
	/// Returns a string representation of the TileRef.
	/// </summary>
	/// <returns>A string representation of the TileRef.</returns>
	public override string ToString() => $"TileRef({TileId})";

	/// <summary>
	/// Determines whether this instance is equal to another object.
	/// </summary>
	/// <param name="obj">The object to compare with.</param>
	/// <returns>true if the objects are equal; otherwise, false.</returns>
	public override bool Equals(object? obj) => obj is TileRef other && Equals(other);

	/// <summary>
	/// Determines whether this instance is equal to another TileRef.
	/// </summary>
	/// <param name="other">The TileRef to compare with.</param>
	/// <returns>true if the TileRefs are equal; otherwise, false.</returns>
	public bool Equals(TileRef other) => TileId == other.TileId;

	/// <summary>
	/// Returns the hash code for this instance.
	/// </summary>
	/// <returns>A hash code for this instance.</returns>
	public override int GetHashCode() => TileId.GetHashCode();

	/// <summary>
	/// Determines whether two TileRef instances are equal.
	/// </summary>
	/// <param name="left">The first TileRef to compare.</param>
	/// <param name="right">The second TileRef to compare.</param>
	/// <returns>true if the TileRefs are equal; otherwise, false.</returns>
	public static bool operator ==(TileRef left, TileRef right) => left.Equals(right);

	/// <summary>
	/// Determines whether two TileRef instances are not equal.
	/// </summary>
	/// <param name="left">The first TileRef to compare.</param>
	/// <param name="right">The second TileRef to compare.</param>
	/// <returns>true if the TileRefs are not equal; otherwise, false.</returns>
	public static bool operator !=(TileRef left, TileRef right) => !left.Equals(right);

	#endregion
}