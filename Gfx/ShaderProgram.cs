using OpenTK.Graphics.OpenGL4;

namespace Critters.Gfx;

class ShaderProgram : IDisposable
{
	#region Fields

	public int Id;
	private bool disposedValue;

	#endregion

	#region Constructors

	private ShaderProgram(params Shader[] shaders)
	{
		Id = GL.CreateProgram();
		foreach (var shader in shaders)
		{
			GL.AttachShader(Id, shader.Id);
		}
		GL.LinkProgram(Id);
		CheckProgramLinkStatus();
	}

	#endregion

	#region Methods

	public static ShaderProgram ForGraphics(string vertexShaderPath, string fragmentShaderPath)
	{
		using var vertexShader = Shader.FromFile(ShaderType.VertexShader, vertexShaderPath);
		using var fragmentShader = Shader.FromFile(ShaderType.FragmentShader, fragmentShaderPath);
		return new ShaderProgram(vertexShader, fragmentShader);
	}

	public static ShaderProgram ForCompute(string computeShaderSource)
	{
		using var computeShader = new Shader(ShaderType.ComputeShader, computeShaderSource);
		return new ShaderProgram(computeShader);
	}

	public void Use()
	{
		GL.UseProgram(Id);
	}

	public int GetUniformLocation(string name)
	{
		return GL.GetUniformLocation(Id, name);
	}

	public ShaderUniform1 GetUniform1(string name)
	{
		return new ShaderUniform1(GetUniformLocation(name));
	}

	public ShaderUniform2 GetUniform2(string name)
	{
		return new ShaderUniform2(GetUniformLocation(name));
	}

	private void CheckProgramLinkStatus()
	{
			GL.GetProgram(Id, GetProgramParameterName.LinkStatus, out int status);
			if (status == 0)
			{
					var infoLog = GL.GetProgramInfoLog(Id);
					throw new Exception($"Program linking failed: {infoLog}");
			}
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!disposedValue)
		{
			GL.DeleteProgram(Id);
			disposedValue = true;
		}
	}

	~ShaderProgram()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: false);
	}

	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	#endregion
}