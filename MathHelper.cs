namespace Critters;

static class MathHelper
{
	/// <summary>
	/// Floor division divides two numbers and rounds the result down to the nearest integer, even for negative numbers.
	/// </summary>
	public static int FloorDiv(int dividend, int divisor)
	{
			return (dividend / divisor) - ((dividend % divisor < 0) ? 1 : 0);
	}

	public static int Modulus(int a, int b)
	{
		if (b == 0)
		{
			throw new DivideByZeroException("The modulus divisor cannot be zero.");
		}

		var result = a % b;
		return (result < 0) ? result + Math.Abs(b) : result;
	}

	public static float Modulus(float a, float b)
	{
		if (b == 0)
		{
			throw new DivideByZeroException("The modulus divisor cannot be zero.");
		}

		var result = a % b;
		return (result < 0) ? result + Math.Abs(b) : result;
	}

	public static T Clamp<T>(T value, T inclusiveMin, T inclusiveMax)
		where T : IComparable<T>
	{
		if (value.CompareTo(inclusiveMin) < 0)
		{
			return inclusiveMin;
		}
		else if (value.CompareTo(inclusiveMax) > 0)
		{
			return inclusiveMax;
		}
		else
		{
			return value;
		}
	}
}