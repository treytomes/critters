using OpenTK.Graphics.OpenGL4;

namespace Critters.Gfx;

class Shader : IDisposable
{
	public readonly int Id;
	public readonly ShaderType Type;
	private bool disposedValue;

	public Shader(ShaderType type, string source)
	{
		Type = type;
		Id = GL.CreateShader(type);
		GL.ShaderSource(Id, source);
		GL.CompileShader(Id);
		CheckShaderCompileStatus();
	}

	public static Shader FromFile(ShaderType type, string path)
	{
		var source = File.ReadAllText(path);
		return new Shader(type, source);
	}

	private void CheckShaderCompileStatus()
	{
		GL.GetShader(Id, ShaderParameter.CompileStatus, out int status);
		if (status == 0)
		{
			var infoLog = GL.GetShaderInfoLog(Id);
			throw new Exception($"Shader compilation failed: {infoLog}");
		}
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!disposedValue)
		{
			if (disposing)
			{
					// TODO: dispose managed state (managed objects)
			}

			GL.DeleteShader(Id);
			disposedValue = true;
		}
	}

	~Shader()
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
}