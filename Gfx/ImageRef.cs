using OpenTK.Mathematics;

namespace Critters.Gfx;

class ImageRef : IImageRef
{
	private Image _image;

	public ImageRef(Image image)
	{
		_image = image;
	}

	public void Render(IRenderingContext rc, Vector2 position)
	{
		_image.Render(rc, position);
	}
}