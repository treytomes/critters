using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Critters.Gfx;

class VirtualDisplay : IDisposable
{
	#region Fields

	private static readonly float[] _quadVertices = 
	{
		// Positions     // Texture Coords
		-1.0f, -1.0f,    0.0f, 1.0f, // Bottom-left
			1.0f, -1.0f,    1.0f, 1.0f, // Bottom-right
			1.0f,  1.0f,    1.0f, 0.0f, // Top-right
		-1.0f,  1.0f,    0.0f, 0.0f  // Top-left
	};

	private static readonly uint[] _quadIndices =
	{
		0, 1, 2,
		2, 3, 0
	};

	public readonly Palette Palette;
	private int _vao;
	private int _vbo;
	private int _ebo;
	private ShaderProgram _shaderProgram;
	private readonly Texture _texture;
	private float _scale = 1.0f;
	private Vector2 _padding = Vector2.Zero;
	
	private bool _disposedValue;

	#endregion

	#region Constructors

	public VirtualDisplay(Vector2i windowSize, Settings.VirtualDisplaySettings settings)
	{
		// Compile shaders
		_shaderProgram = new ShaderProgram(settings.VertexShaderPath, settings.FragmentShaderPath);

		// Generate texture
		_texture = new Texture(settings.Width, settings.Height, true);

		// Create VAO, VBO, and EBO
		_vao = GL.GenVertexArray();
		if (_vao == 0)
		{
			throw new Exception("Unable to generate vertex array.");
		}

		_vbo = GL.GenBuffer();
		if (_vbo == 0)
		{
			throw new Exception("Unable to generate vertex buffer.");
		}

		_ebo = GL.GenBuffer();
		if (_ebo == 0)
		{
			throw new Exception("Unable to generate element buffer.");
		}

		GL.BindVertexArray(_vao);

		// Bind vertex buffer
		GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
		GL.BufferData(BufferTarget.ArrayBuffer, _quadVertices.Length * sizeof(float), _quadVertices, BufferUsageHint.StaticDraw);

		// Bind element buffer
		GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
		GL.BufferData(BufferTarget.ElementArrayBuffer, _quadIndices.Length * sizeof(uint), _quadIndices, BufferUsageHint.StaticDraw);

		// Set vertex attribute pointers
		GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
		GL.EnableVertexAttribArray(0);

		GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
		GL.EnableVertexAttribArray(1);

		GL.BindVertexArray(0);

		Palette = new Palette();
		Resize(windowSize);
	}

	#endregion

	#region Properties

	public int Width => _texture.Width;

	public int Height => _texture.Height;

	public float Scale => _scale;

	#endregion

	#region Methods

	/// <summary>
	/// Convert actual screen coordinates to virtual coordinates.
	/// </summary>
	public Vector2 ActualToVirtualPoint(Vector2 actualPoint)
	{
		return (actualPoint - _padding) / _scale;
	}

	/// <summary>
	/// Convert virtual coordinates to actual screen coordinates.
	/// </summary>
	public Vector2 VirtualToActualPoint(Vector2 virtualPoint)
	{
		return virtualPoint * _scale + _padding;
	}
	
	public void UpdatePixels(byte[] pixelData)
	{
		_texture.Data = pixelData;
	}

	public void Resize(Vector2i windowSize)
	{
		// Calculate aspect ratios.
		var virtualAspect = (float)_texture.Width / _texture.Height;
		var windowAspect = (float)windowSize.X / windowSize.Y;

		// Calculate scaling factors.
		if (windowAspect > virtualAspect)
		{
			// Window is wider than the virtual display
			_scale = (float)windowSize.Y / _texture.Height;
			_padding = new Vector2((windowSize.X - _texture.Width * _scale) / 2f, 0);
		}
		else
		{
			// Window is taller than the virtual display
			_scale = (float)windowSize.X / _texture.Width;
			_padding = new Vector2(0, (windowSize.Y - _texture.Height * _scale) / 2f);
		}

		// Set the viewport with padding
		GL.Viewport((int)_padding.X, (int)_padding.Y, (int)(_texture.Width * _scale), (int)(_texture.Height * _scale));
	}

	public void Render()
	{
		GL.Clear(ClearBufferMask.ColorBufferBit);
		
		// Use shader and VAO
		_shaderProgram.Use();
		GL.BindVertexArray(_vao);

		// Bind _paletteTextureId to the uPalette sampler2d shader variable.
		GL.ActiveTexture(TextureUnit.Texture1);
		GL.BindTexture(TextureTarget.Texture2D, Palette.Id);
		GL.Uniform1(_shaderProgram.GetUniformLocation("uPalette"), 1);

		// Bind texture
		GL.ActiveTexture(TextureUnit.Texture0);
		_texture.Bind();
		GL.Uniform1(_shaderProgram.GetUniformLocation("uTexture"), 0);

		// Draw quad
		GL.DrawElements(PrimitiveType.Triangles, _quadIndices.Length, DrawElementsType.UnsignedInt, 0);

		// Unbind VAO and shader
		GL.BindVertexArray(0);
		GL.UseProgram(0);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
				_shaderProgram?.Dispose();
				Palette?.Dispose();
				_texture?.Dispose();
			}

			GL.DeleteBuffer(_ebo);
			GL.DeleteBuffer(_vao);
			GL.DeleteBuffer(_vbo);

			_disposedValue = true;
		}
	}

	~VirtualDisplay()
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