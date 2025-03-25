using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Critters.Gfx;

class ShaderUniform2(int location)
{
	public void Set(float x, float y)
	{
		GL.Uniform2(location, x, y);
	}

	public void Set(Vector2 vector)
	{
		GL.Uniform2(location, vector);
	}
}
