namespace Critters;

static class ArrayExtensions
{
	public static double[,] ToArray(this double[,] @this)
	{
		var result = new double[@this.GetLength(0), @this.GetLength(1)];
		for (var o = 0; o < @this.GetLength(1); o++)
		{
			for (var i = 0; i < @this.GetLength(0); i++)
			{
				result[o, i] = @this[o, i];
			}
		}
		return result;
	}
}