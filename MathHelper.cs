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