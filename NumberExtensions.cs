namespace Critters;

static class NumberExtensions
{
	public static float Fract(this float @this)
	{
		return @this - float.Truncate(@this);
	}

	public static double Fract(this double @this)
	{
		return @this - double.Truncate(@this);
	}
}