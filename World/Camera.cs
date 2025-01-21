using OpenTK.Mathematics;

namespace Critters.World;

class Camera
{
	#region Constructors

	public Camera()
	{
		Position = Vector2.Zero;
	}

	#endregion

	#region Properties

	public Vector2 Position { get; set; }

	#endregion

	#region Methods

	public void ScrollBy(Vector2 delta)
	{
		Position += delta;
	}

	public void ScrollTo(Vector2 position)
	{
		Position = position;
	}

	public Vector2 Transform(Vector2 position)
	{
		return position - Position;
	}

	#endregion
}
