using OpenTK.Graphics.OpenGL4;

namespace Critters.Gfx;

class ShaderUniform1(int location)
{
	public void Set(int value)
	{
		GL.Uniform1(location, value);
	}
}
