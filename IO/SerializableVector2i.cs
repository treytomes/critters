using OpenTK.Mathematics;

namespace Critters.IO;

class SerializableVector2i
{
	public int X { get; set; }
	public int Y { get; set; }

	public SerializableVector2i(Vector2i v)
	{
		X = v.X;
		Y = v.Y;
	}

	public Vector2i ToVector2i() => (X, Y);
}