namespace Critters.Gfx;

readonly struct Color : IEquatable<Color>
{
	#region Fields

	public readonly byte Red;
	public readonly byte Green;
	public readonly byte Blue;

	#endregion

	#region Constructors

	public Color(byte r, byte g, byte b)
	{
		Red = r;
		Green = g;
		Blue = b;
	}

	#endregion

	#region Methods

	public bool Equals(Color other)
	{
		return Red == other.Red && Green == other.Green && Blue == other.Blue;
	}

	public override bool Equals(object? obj)
	{
		return (obj != null) && obj is Color && Equals((Color)obj);
	}

	public override int GetHashCode()
	{
		return (Red << 16) | (Green << 8) | Blue;
	}

	public static bool operator ==(Color a, Color b)
	{
		return a.Equals(b);
	}

	public static bool operator !=(Color a, Color b)
	{
		return !(a == b);
	}

	#endregion
}