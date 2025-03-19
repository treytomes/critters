using OpenTK.Mathematics;

namespace Critters.IO;

class SerializableVector2
{
	public float X { get; set; }
	public float Y { get; set; }

	public SerializableVector2(Vector2 v)
	{
		X = v.X;
		Y = v.Y;
	}

	public Vector2 ToVector2() => (X, Y);
}