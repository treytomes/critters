namespace Critters;

/// <summary>  
/// Provides helper methods for common mathematical operations.
/// </summary>  
public static class MathHelper
{
	public const float Pi = (float)Math.PI;
	public const float PiOver2 = Pi / 2.0f;
	public const float PiOver4 = Pi / 4.0f;

	/// <summary>
	/// Computes the floor modulus, which works correctly for negative values.
	/// For example, FloorMod(-1, 5) returns 4, whereas (-1 % 5) returns -1 in C#.
	/// </summary>
	/// <param name="x">The dividend.</param>
	/// <param name="y">The divisor.</param>
	/// <returns>
	/// The floor modulus, which is always in the range [0, y) when y is positive.
	/// </returns>
	/// <exception cref="DivideByZeroException">Thrown when y is zero.</exception>
	public static int FloorMod(int x, int y)
	{
		if (y == 0)
			throw new DivideByZeroException("Cannot compute modulus with zero divisor");

		// For positive divisors, ensure the result is in the range [0, y)
		if (y > 0)
		{
			int r = x % y;
			// If the remainder is negative, add the divisor to get a positive result
			return r < 0 ? r + y : r;
		}
		else
		{
			// For negative divisors, ensure the result is in the range (y, 0]
			int r = x % y;
			// If the remainder is positive, add the divisor to get a negative result
			return r > 0 ? r + y : r;
		}
	}

	/// <summary>
	/// Computes the floor division, which rounds towards negative infinity.
	/// For example, FloorDiv(-1, 5) returns -1, whereas (-1 / 5) returns 0 in C#.
	/// </summary>
	/// <param name="x">The dividend.</param>
	/// <param name="y">The divisor.</param>
	/// <returns>The floor division result.</returns>
	/// <exception cref="DivideByZeroException">Thrown when y is zero.</exception>
	public static int FloorDiv(int x, int y)
	{
		if (y == 0)
			throw new DivideByZeroException("Cannot divide by zero");

		// Integer division in C# truncates toward zero, not toward negative infinity
		// For positive numbers, truncation and floor division are the same
		// For negative numbers, we need to adjust if there's a remainder

		int q = x / y;
		int r = x % y;

		// If the signs are different and the remainder is not zero, adjust the quotient
		if ((x < 0) != (y < 0) && r != 0)
		{
			q--;
		}

		return q;
	}

	// public static int FloorDiv(int dividend, int divisor)
	// {
	// 	if (divisor == 0)
	// 		throw new DivideByZeroException("The divisor cannot be zero.");

	// 	return (dividend / divisor) - ((dividend % divisor < 0) ? 1 : 0);
	// }

	/// <summary>  
	/// Performs positive modulus operation, ensuring the result is always non-negative.  
	/// </summary>  
	/// <param name="a">The dividend.</param>  
	/// <param name="b">The divisor.</param>  
	/// <returns>The non-negative remainder after division.</returns>  
	/// <exception cref="DivideByZeroException">Thrown when divisor is zero.</exception>  
	public static int PositiveModulus(int a, int b)
	{
		if (b == 0)
			throw new DivideByZeroException("The modulus divisor cannot be zero.");

		var result = a % b;
		return (result < 0) ? result + Math.Abs(b) : result;
	}

	/// <summary>  
	/// Performs positive modulus operation on floating-point numbers, ensuring the result is always non-negative.  
	/// </summary>  
	/// <param name="a">The dividend.</param>  
	/// <param name="b">The divisor.</param>  
	/// <returns>The non-negative remainder after division.</returns>  
	/// <exception cref="DivideByZeroException">Thrown when divisor is zero.</exception>  
	public static float PositiveModulus(float a, float b)
	{
		if (b == 0)
			throw new DivideByZeroException("The modulus divisor cannot be zero.");

		var result = a % b;
		return (result < 0) ? result + Math.Abs(b) : result;
	}

	/// <summary>  
	/// Alias for PositiveModulus to maintain backward compatibility.  
	/// </summary>  
	public static int Modulus(int a, int b) => PositiveModulus(a, b);

	/// <summary>  
	/// Alias for PositiveModulus to maintain backward compatibility.  
	/// </summary>  
	public static float Modulus(float a, float b) => PositiveModulus(a, b);

	/// <summary>  
	/// Clamps a value between an inclusive minimum and maximum value.  
	/// </summary>  
	/// <typeparam name="T">The type of the value to clamp.</typeparam>  
	/// <param name="value">The value to clamp.</param>  
	/// <param name="min">The inclusive minimum value.</param>  
	/// <param name="max">The inclusive maximum value.</param>  
	/// <returns>The clamped value.</returns>  
	public static T Clamp<T>(T value, T min, T max)
		where T : IComparable<T>
	{
		if (value.CompareTo(min) < 0)
			return min;
		else if (value.CompareTo(max) > 0)
			return max;
		else
			return value;
	}

	/// <summary>  
	/// Linearly interpolates between two values.  
	/// </summary>  
	/// <param name="a">The starting value.</param>  
	/// <param name="b">The ending value.</param>  
	/// <param name="t">The interpolation factor (0.0 to 1.0).</param>  
	/// <returns>The interpolated value.</returns>  
	public static float Lerp(float a, float b, float t)
	{
		return a + (b - a) * Clamp(t, 0.0f, 1.0f);
	}

	/// <summary>  
	/// Linearly interpolates between two values without clamping the interpolation factor.  
	/// </summary>  
	/// <param name="a">The starting value.</param>  
	/// <param name="b">The ending value.</param>  
	/// <param name="t">The interpolation factor.</param>  
	/// <returns>The interpolated value.</returns>  
	public static float LerpUnclamped(float a, float b, float t)
	{
		return a + (b - a) * t;
	}

	/// <summary>  
	/// Linearly interpolates between two double values.  
	/// </summary>  
	/// <param name="a">The starting value.</param>  
	/// <param name="b">The ending value.</param>  
	/// <param name="t">The interpolation factor (0.0 to 1.0).</param>  
	/// <returns>The interpolated value.</returns>  
	public static double Lerp(double a, double b, double t)
	{
		return a + (b - a) * Clamp(t, 0.0, 1.0);
	}

	/// <summary>  
	/// Linearly interpolates between two integer values.  
	/// </summary>  
	/// <param name="a">The starting value.</param>  
	/// <param name="b">The ending value.</param>  
	/// <param name="t">The interpolation factor (0.0 to 1.0).</param>  
	/// <returns>The interpolated value as an integer.</returns>  
	public static int Lerp(int a, int b, float t)
	{
		return (int)Math.Round(Lerp((float)a, (float)b, t));
	}

	/// <summary>  
	/// Calculates the inverse of linear interpolation to find the interpolation factor.  
	/// </summary>  
	/// <param name="a">The starting value.</param>  
	/// <param name="b">The ending value.</param>  
	/// <param name="value">The value to find the interpolation factor for.</param>  
	/// <returns>The interpolation factor (not clamped).</returns>  
	public static float InverseLerp(float a, float b, float value)
	{
		if (Math.Abs(b - a) < float.Epsilon)
			return 0f;

		return (value - a) / (b - a);
	}

	/// <summary>  
	/// Converts an angle in degrees to radians.  
	/// </summary>  
	/// <param name="degrees">The angle in degrees.</param>  
	/// <returns>The angle in radians.</returns>  
	public static float DegreesToRadians(float degrees)
	{
		return degrees * (float)Math.PI / 180f;
	}

	/// <summary>  
	/// Converts an angle in radians to degrees.  
	/// </summary>  
	/// <param name="radians">The angle in radians.</param>  
	/// <returns>The angle in degrees.</returns>  
	public static float RadiansToDegrees(float radians)
	{
		return radians * 180f / (float)Math.PI;
	}

	/// <summary>  
	/// Performs a smooth step interpolation between 0 and 1.  
	/// </summary>  
	/// <param name="edge0">The lower edge of the interpolation range.</param>  
	/// <param name="edge1">The upper edge of the interpolation range.</param>  
	/// <param name="x">The value to interpolate.</param>  
	/// <returns>0 if x ≤ edge0, 1 if x ≥ edge1, and smooth interpolation otherwise.</returns>  
	public static float SmoothStep(float edge0, float edge1, float x)
	{
		// Clamp x to the range [0, 1]  
		x = Clamp((x - edge0) / (edge1 - edge0), 0.0f, 1.0f);

		// Evaluate polynomial  
		return x * x * (3 - 2 * x);
	}

	/// <summary>  
	/// Checks if two floating-point values are approximately equal.  
	/// </summary>  
	/// <param name="a">The first value.</param>  
	/// <param name="b">The second value.</param>  
	/// <param name="epsilon">The maximum difference between the values.</param>  
	/// <returns>True if the values are approximately equal.</returns>  
	public static bool Approximately(float a, float b, float epsilon = 1e-5f)
	{
		return Math.Abs(a - b) < epsilon;
	}
}