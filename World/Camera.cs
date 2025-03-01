using OpenTK.Mathematics;

namespace Critters.World;

class Camera
{
	#region Constructors

	public Camera(Vector2? viewportSize = null)
	{
		Position = Vector2.Zero;
		ViewportSize = viewportSize ?? Vector2.Zero;
	}

	#endregion

	#region Properties

	public Vector2 Position { get; set; }
	public Vector2 ViewportSize { get; set; }

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

	public Vector2 ScreenToWorld(Vector2 position)
	{
		return position + Position - ViewportSize / 2;
	}

	public Vector2 WorldToScreen(Vector2 position)
	{
		return position - Position + ViewportSize / 2;
	}

	#endregion
}
