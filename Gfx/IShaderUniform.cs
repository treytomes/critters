using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Critters.Gfx;

/// <summary>  
/// Interface for shader uniform wrappers.  
/// </summary>  
public interface IShaderUniform
{
	/// <summary>  
	/// Gets the location of this uniform in the shader program.  
	/// </summary>  
	int Location { get; }

	/// <summary>  
	/// Checks if this uniform is valid (has a valid location).  
	/// </summary>  
	bool IsValid { get; }
}
