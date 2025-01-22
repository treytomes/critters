using OpenTK.Mathematics;

namespace Critters.World;

class Camera
{
	#region Constructors

	public Camera(Vector2 viewportSize)
	{
		Position = Vector2.Zero;
		ViewportSize = viewportSize;
	}

	#endregion

	#region Properties

	public Vector2 Position { get; set; }
	public Vector2 ViewportSize { get; }

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
