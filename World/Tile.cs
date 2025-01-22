using Critters.Gfx;
using OpenTK.Mathematics;

namespace Critters.World;

class Tile
{
	#region Fields

	public readonly int Id;
	private readonly IImageRef _image;

	#endregion

	#region Constructors

	public Tile(int id, IImageRef image)
	{
		Id = id;
		_image = image;
	}

	#endregion

	#region Methods

	public void Render(RenderingContext rc, Vector2 position)
	{
		_image.Render(rc, position);
	}

	#endregion
}