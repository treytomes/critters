using OpenTK.Graphics.OpenGL4;

namespace Gfx
{
	class ShaderProgram : IDisposable
	{
		#region Fields

		public int Id;
		private bool disposedValue;

		#endregion

		#region Constructors

		public ShaderProgram(string vertexShaderPath, string fragmentShaderPath)
		{
			using var vertexShader = Shader.FromFile(OpenTK.Graphics.OpenGL4.ShaderType.VertexShader, vertexShaderPath);
			using var fragmentShader = Shader.FromFile(OpenTK.Graphics.OpenGL4.ShaderType.FragmentShader, fragmentShaderPath);

			Id = GL.CreateProgram();
			GL.AttachShader(Id, vertexShader.Id);
			GL.AttachShader(Id, fragmentShader.Id);
			GL.LinkProgram(Id);
			CheckProgramLinkStatus();
		}

		#endregion

		#region Methods

		public void Use()
		{
			GL.UseProgram(Id);
		}

		public int GetUniformLocation(string name)
		{
			return GL.GetUniformLocation(Id, name);
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
}