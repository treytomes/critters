using OpenTK.Mathematics;

namespace Critters;

static class VectorExtensions
{
	public static Vector2 Floor(this Vector2 @this)
	{
		return new Vector2((float)Math.Floor(@this.X), (float)Math.Floor(@this.Y));
	}

	public static Vector3 Floor(this Vector3 @this)
	{
		return new Vector3((float)Math.Floor(@this.X), (float)Math.Floor(@this.Y), (float)Math.Floor(@this.Z));
	}

	public static Vector4 Floor(this Vector4 @this)
	{
		return new Vector4((float)Math.Floor(@this.X), (float)Math.Floor(@this.Y), (float)Math.Floor(@this.Z), (float)Math.Floor(@this.W));
	}

	public static Vector2 Fract(this Vector2 @this)
	{
		return new Vector2(@this.X.Fract(), @this.Y.Fract());
	}

	public static Vector3 Fract(this Vector3 @this)
	{
		return new Vector3(@this.X.Fract(), @this.Y.Fract(), @this.Z.Fract());
	}

	public static Vector4 Fract(this Vector4 @this)
	{
		return new Vector4(@this.X.Fract(), @this.Y.Fract(), @this.Z.Fract(), @this.W.Fract());
	}

	public static Vector3 Www(this Vector4 @this)
	{
		return Vector3.One * @this.W;
	}
}