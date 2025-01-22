using OpenTK.Mathematics;

namespace Critters.Gfx;

interface IImageRef
{
	void Render(RenderingContext rc, Vector2 position);
}