using OpenTK.Mathematics;

namespace Critters;

static class VectorExtensions
{
	public static Vector2 Floor(this Vector2 @this)
	{
		return new Vector2((float)Math.Floor(@this.X), (float)Math.Floor(@this.Y));
	}
}