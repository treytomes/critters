using OpenTK.Mathematics;

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
			throw new ArgumentException($"Invalid color value: {r}", nameof(r));
		}
		if (g > 5)
		{
			throw new ArgumentException($"Invalid color value: {g}", nameof(g));
		}
		if (b > 5)
		{
			throw new ArgumentException($"Invalid color value: {b}", nameof(b));
		}
		Red = r;
		Green = g;
		Blue = b;
	}

	#endregion

	#region Properties

	// public static RadialColor Black => new RadialColor(0, 0, 0);
	// public static RadialColor White => new RadialColor(5, 5, 5);
	// public static RadialColor Red => new RadialColor(5, 0, 0);
	// public static RadialColor Green => new RadialColor(0, 5, 0);
	// public static RadialColor Blue => new RadialColor(0, 0, 5);

	/// <summary>
	/// Calculate the palette index for this color.
	/// </summary>
	public byte Index
	{
		get
		{
			return (byte)((Red * 6 * 6) + (Green * 6) + Blue);
		}
	}

	#endregion

	#region Methods

	public override string ToString()
	{
		return $"{nameof(RadialColor)}({Red},{Green},{Blue})";
	}
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
		return Index;
	}

	public RadialColor Add(RadialColor other)
	{
		return new RadialColor(
			(byte)Math.Min(5, Red + other.Red),
			(byte)Math.Min(5, Green + other.Green),
			(byte)Math.Min(5, Blue + other.Blue)
		);
	}

	/// <summary>  
	/// Converts a standard Color to a RadialColor.  
	/// </summary>  
	public static RadialColor FromColor(Color4 color)
	{
		byte r = (byte)Math.Min(5, Math.Round(color.R * 5));
		byte g = (byte)Math.Min(5, Math.Round(color.G * 5));
		byte b = (byte)Math.Min(5, Math.Round(color.B * 5));
		return new RadialColor(r, g, b);
	}

	/// <summary>  
	/// Converts this RadialColor to a standard Color.  
	/// </summary>  
	public Color4 ToColor()
	{
		return new Color4(Red / 5.0f, Green / 5.0f, Blue / 5.0f, 1.0f);
	}
	/// <summary>  
	/// Linearly interpolates between two RadialColors.  
	/// </summary>  
	/// <param name="other">The target color.</param>  
	/// <param name="t">Interpolation factor (0.0 to 1.0).</param>  
	/// <returns>The interpolated color.</returns>  
	public RadialColor Lerp(RadialColor other, float t)
	{
		t = Math.Clamp(t, 0.0f, 1.0f);
		float r = MathHelper.Lerp(Red, other.Red, t);
		float g = MathHelper.Lerp(Green, other.Green, t);
		float b = MathHelper.Lerp(Blue, other.Blue, t);
		return new RadialColor(
			(byte)Math.Round(r),
			(byte)Math.Round(g),
			(byte)Math.Round(b)
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