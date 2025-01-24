namespace Critters.Gfx;

readonly struct RadialColor : IEquatable<RadialColor>
{
	#region Fields

	public readonly byte Red;
	public readonly byte Green;
	public readonly byte Blue;

	#endregion

	#region Constructors

	public RadialColor(byte r, byte g, byte b)
	{
		if (r > 5)
		{
			throw new ArgumentException("Invalid color value.", nameof(r));
		}
		if (g > 5)
		{
			throw new ArgumentException("Invalid color value.", nameof(g));
		}
		if (b > 5)
		{
			throw new ArgumentException("Invalid color value.", nameof(b));
		}
		Red = r;
		Green = g;
		Blue = b;
	}

	#endregion

	#region Methods

	public bool Equals(RadialColor other)
	{
		return Red == other.Red && Green == other.Green && Blue == other.Blue;
	}

	public override bool Equals(object? obj)
	{
		return (obj != null) && obj is RadialColor && Equals((RadialColor)obj);
	}

	public override int GetHashCode()
	{
		return GetIndex();
	}

	/// <summary>
	/// Calculate the palette index for this color.
	/// </summary>
	public byte GetIndex()
	{
		return (byte)((Red * 6 * 6) + (Green * 6) + Blue);
	}

	public RadialColor Add(RadialColor other)
	{
		return new RadialColor(
			(byte)Math.Min(5, Red + other.Red),
			(byte)Math.Min(5, Green + other.Green),
			(byte)Math.Min(5, Blue + other.Blue)
		);
	}

	public static bool operator ==(RadialColor a, RadialColor b)
	{
		return a.Equals(b);
	}

	public static bool operator !=(RadialColor a, RadialColor b)
	{
		return !(a == b);
	}

	public static RadialColor operator +(RadialColor a, RadialColor b)
	{
		return a.Add(b);
	}

	#endregion
}