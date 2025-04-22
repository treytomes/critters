using OpenTK.Mathematics;

namespace Critters.Gfx;

interface IImageRef
{
	void Render(IRenderingContext rc, Vector2 position);
}