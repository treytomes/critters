using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Critters.Gfx;

/// <summary>  
/// Manages a virtual display with fixed resolution that scales to fit the window  
/// while maintaining aspect ratio.  
/// </summary>  
class VirtualDisplay : IVirtualDisplay
{
	#region Fields  

	private static readonly float[] _quadVertices =
	{  
        // Positions    // Texture Coords  
        -1.0f, -1.0f,   0.0f, 1.0f, // Bottom-left  
         1.0f, -1.0f,   1.0f, 1.0f, // Bottom-right  
         1.0f,  1.0f,   1.0f, 0.0f, // Top-right  
        -1.0f,  1.0f,   0.0f, 0.0f  // Top-left  
    };

	private static readonly uint[] _quadIndices =
	{
		0, 1, 2,
		2, 3, 0
	};

	private readonly int _vao;
	private readonly int _vbo;
	private readonly int _ebo;
	private readonly ShaderProgram _shaderProgram;
	private readonly Texture _texture;
	private readonly int _textureUniformLocation;
	private readonly int _paletteUniformLocation;

	// Scaling and positioning variables  
	private float _scale = 1.0f;
	private Vector2 _padding = Vector2.Zero;
	private Vector2i _lastWindowSize;
	private Palette _palette;

	// Pixel data management  
	private bool _textureNeedsUpdate = false;
	private byte[] _pixelData;

	private bool _disposedValue;

	#endregion

	#region Constructors  

	/// <summary>  
	/// Creates a new virtual display with the specified settings.  
	/// </summary>  
	/// <param name="windowSize">The initial window size.</param>  
	/// <param name="settings">The settings for the virtual display.</param>  
	/// <exception cref="ArgumentNullException">Thrown if settings is null.</exception>  
	/// <exception cref="Exception">Thrown if any OpenGL resource creation fails.</exception>  
	public VirtualDisplay(Vector2i windowSize, VirtualDisplaySettings settings)
	{
		if (settings == null)
			throw new ArgumentNullException(nameof(settings));

		try
		{
			// Compile shaders  
			_shaderProgram = ShaderProgram.ForGraphics(settings.VertexShaderPath, settings.FragmentShaderPath);

			// Generate texture  
			_texture = new Texture(settings.Width, settings.Height, true);
			_pixelData = new byte[settings.Width * settings.Height];

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

			// Cache uniform locations  
			_shaderProgram.Use();
			_textureUniformLocation = _shaderProgram.GetUniformLocation("uTexture");
			_paletteUniformLocation = _shaderProgram.GetUniformLocation("uPalette");
			GL.UseProgram(0);

			// Initialize palette  
			_palette = new Palette();

			// Set initial window size and calculate scaling  
			_lastWindowSize = windowSize;
			CalculateScaleAndPadding(windowSize);
		}
		catch
		{
			// Clean up any resources that were created before the exception  
			Dispose(true);
			throw;
		}
	}

	#endregion

	#region Properties  

	/// <summary>  
	/// Gets the width of the virtual display in pixels.  
	/// </summary>  
	public int Width => _texture.Width;

	/// <summary>  
	/// Gets the height of the virtual display in pixels.  
	/// </summary>  
	public int Height => _texture.Height;

	/// <summary>  
	/// Gets the current scale factor applied to the virtual display.  
	/// </summary>  
	public float Scale => _scale;

	/// <summary>  
	/// Gets the current padding applied to center the display.  
	/// </summary>  
	public Vector2 Padding => _padding;

	/// <summary>  
	/// Gets the color palette used by the rendering context.  
	/// </summary>  
	public Palette Palette => _palette;

	#endregion

	#region Methods  

	/// <summary>  
	/// Convert actual screen coordinates to virtual coordinates.  
	/// </summary>  
	/// <param name="actualPoint">The point in actual screen coordinates.</param>  
	/// <returns>The corresponding point in virtual display coordinates.</returns>  
	public Vector2 ActualToVirtualPoint(Vector2 actualPoint)
	{
		return (actualPoint - _padding) / _scale;
	}

	/// <summary>  
	/// Convert virtual coordinates to actual screen coordinates.  
	/// </summary>  
	/// <param name="virtualPoint">The point in virtual display coordinates.</param>  
	/// <returns>The corresponding point in actual screen coordinates.</returns>  
	public Vector2 VirtualToActualPoint(Vector2 virtualPoint)
	{
		return virtualPoint * _scale + _padding;
	}

	/// <summary>  
	/// Updates the pixel data for the virtual display.  
	/// </summary>  
	/// <param name="pixelData">The new pixel data (palette indices).</param>  
	/// <exception cref="ArgumentNullException">Thrown if pixelData is null.</exception>  
	/// <exception cref="ArgumentException">Thrown if pixelData length doesn't match display size.</exception>  
	public void UpdatePixels(byte[] pixelData)
	{
		if (pixelData == null)
			throw new ArgumentNullException(nameof(pixelData));

		if (pixelData.Length != Width * Height)
			throw new ArgumentException($"Pixel data length must be {Width * Height}", nameof(pixelData));

		System.Buffer.BlockCopy(pixelData, 0, _pixelData, 0, pixelData.Length);
		_textureNeedsUpdate = true;
	}

	/// <summary>  
	/// Sets a single pixel in the virtual display.  
	/// </summary>  
	/// <param name="x">The x-coordinate of the pixel.</param>  
	/// <param name="y">The y-coordinate of the pixel.</param>  
	/// <param name="colorIndex">The palette index to set.</param>  
	/// <returns>True if the pixel was set, false if coordinates were out of bounds.</returns>  
	public bool SetPixel(int x, int y, byte colorIndex)
	{
		if (x < 0 || x >= Width || y < 0 || y >= Height)
			return false;

		int index = y * Width + x;
		_pixelData[index] = colorIndex;
		_textureNeedsUpdate = true;
		return true;
	}

	/// <summary>  
	/// Gets the color index at the specified pixel.  
	/// </summary>  
	/// <param name="x">The x-coordinate of the pixel.</param>  
	/// <param name="y">The y-coordinate of the pixel.</param>  
	/// <returns>The palette index at the specified pixel, or 0 if out of bounds.</returns>  
	public byte GetPixel(int x, int y)
	{
		if (x < 0 || x >= Width || y < 0 || y >= Height)
			return 0;

		int index = y * Width + x;
		return _pixelData[index];
	}

	/// <summary>  
	/// Clears the virtual display to the specified color index.  
	/// </summary>  
	/// <param name="colorIndex">The palette index to fill with.</param>  
	public void Clear(byte colorIndex = 0)
	{
		Array.Fill(_pixelData, colorIndex);
		_textureNeedsUpdate = true;
	}

	/// <summary>  
	/// Resizes the display to fit the new window size.  
	/// </summary>  
	/// <param name="windowSize">The new window size.</param>  
	public void Resize(Vector2i windowSize)
	{
		if (_lastWindowSize != windowSize)
		{
			_lastWindowSize = windowSize;
			CalculateScaleAndPadding(windowSize);
		}
	}

	/// <summary>  
	/// Calculates the scale and padding based on the window size to maintain aspect ratio.  
	/// </summary>  
	/// <param name="windowSize">The window size to calculate for.</param>  
	private void CalculateScaleAndPadding(Vector2i windowSize)
	{
		// Calculate aspect ratios  
		float virtualAspect = (float)Width / Height;
		float windowAspect = (float)windowSize.X / windowSize.Y;

		// Calculate scaling factors based on aspect ratio comparison  
		if (windowAspect > virtualAspect)
		{
			// Window is wider than the virtual display  
			// Scale based on height and add horizontal padding  
			_scale = (float)windowSize.Y / Height;
			_padding = new Vector2((windowSize.X - Width * _scale) / 2f, 0);
		}
		else
		{
			// Window is taller than the virtual display  
			// Scale based on width and add vertical padding  
			_scale = (float)windowSize.X / Width;
			_padding = new Vector2(0, (windowSize.Y - Height * _scale) / 2f);
		}
	}

	/// <summary>  
	/// Renders the virtual display to the current framebuffer.  
	/// </summary>  
	public void Render()
	{
		// Update texture if needed  
		if (_textureNeedsUpdate)
		{
			_texture.Data = _pixelData;
			_textureNeedsUpdate = false;
		}

		// Save current viewport and other GL state  
		int[] currentViewport = new int[4];
		GL.GetInteger(GetPName.Viewport, currentViewport);

		bool blendWasEnabled = GL.IsEnabled(EnableCap.Blend);
		int[] previousBlendFunc = new int[2];
		GL.GetInteger(GetPName.BlendSrc, out previousBlendFunc[0]);
		GL.GetInteger(GetPName.BlendDst, out previousBlendFunc[1]);

		// Calculate the final dimensions for the viewport  
		int scaledWidth = (int)(Width * _scale);
		int scaledHeight = (int)(Height * _scale);
		int xOffset = (int)_padding.X;
		int yOffset = (int)_padding.Y;

		// Set the viewport to match the scaled display area with padding  
		// This ensures aspect ratio preservation and proper positioning  
		GL.Viewport(xOffset, yOffset, scaledWidth, scaledHeight);

		// Clear the color buffer to ensure clean black bars  
		// Note: This clears the entire framebuffer, not just the viewport area  
		GL.Clear(ClearBufferMask.ColorBufferBit);

		// Setup blending  
		GL.Enable(EnableCap.Blend);
		GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

		// Use shader and VAO  
		_shaderProgram.Use();
		GL.BindVertexArray(_vao);

		// Save current texture bindings  
		int[] previousTexture0 = new int[1];
		int[] previousTexture1 = new int[1];
		GL.GetInteger(GetPName.TextureBinding2D, previousTexture0);

		// Bind palette to texture unit 1  
		GL.ActiveTexture(TextureUnit.Texture1);
		GL.GetInteger(GetPName.TextureBinding2D, previousTexture1);
		GL.BindTexture(TextureTarget.Texture2D, Palette.Id);
		GL.Uniform1(_paletteUniformLocation, 1);

		// Bind main texture to texture unit 0  
		GL.ActiveTexture(TextureUnit.Texture0);
		GL.BindTexture(TextureTarget.Texture2D, _texture.Id);
		GL.Uniform1(_textureUniformLocation, 0);

		// Draw quad  
		GL.DrawElements(PrimitiveType.Triangles, _quadIndices.Length, DrawElementsType.UnsignedInt, 0);

		// Restore previous state  
		GL.BindVertexArray(0);
		GL.UseProgram(0);

		// Restore texture bindings  
		GL.ActiveTexture(TextureUnit.Texture0);
		GL.BindTexture(TextureTarget.Texture2D, previousTexture0[0]);
		GL.ActiveTexture(TextureUnit.Texture1);
		GL.BindTexture(TextureTarget.Texture2D, previousTexture1[0]);
		GL.ActiveTexture(TextureUnit.Texture0); // Leave active texture at 0  

		// Restore blend state  
		if (!blendWasEnabled)
			GL.Disable(EnableCap.Blend);
		GL.BlendFunc((BlendingFactor)previousBlendFunc[0], (BlendingFactor)previousBlendFunc[1]);

		// Restore viewport  
		GL.Viewport(currentViewport[0], currentViewport[1], currentViewport[2], currentViewport[3]);
	}

	/// <summary>  
	/// Draws a line between two points using Bresenham's algorithm.  
	/// </summary>  
	/// <param name="x0">Starting x-coordinate.</param>  
	/// <param name="y0">Starting y-coordinate.</param>  
	/// <param name="x1">Ending x-coordinate.</param>  
	/// <param name="y1">Ending y-coordinate.</param>  
	/// <param name="colorIndex">The palette index to use for the line.</param>  
	public void DrawLine(int x0, int y0, int x1, int y1, byte colorIndex)
	{
		int dx = Math.Abs(x1 - x0);
		int dy = -Math.Abs(y1 - y0);
		int sx = x0 < x1 ? 1 : -1;
		int sy = y0 < y1 ? 1 : -1;
		int err = dx + dy;

		while (true)
		{
			SetPixel(x0, y0, colorIndex);

			if (x0 == x1 && y0 == y1) break;

			int e2 = 2 * err;
			if (e2 >= dy)
			{
				if (x0 == x1) break;
				err += dy;
				x0 += sx;
			}

			if (e2 <= dx)
			{
				if (y0 == y1) break;
				err += dx;
				y0 += sy;
			}
		}
	}

	/// <summary>  
	/// Draws a rectangle outline.  
	/// </summary>  
	/// <param name="x">X-coordinate of the top-left corner.</param>  
	/// <param name="y">Y-coordinate of the top-left corner.</param>  
	/// <param name="width">Width of the rectangle.</param>  
	/// <param name="height">Height of the rectangle.</param>  
	/// <param name="colorIndex">The palette index to use for the rectangle.</param>  
	public void DrawRectangle(int x, int y, int width, int height, byte colorIndex)
	{
		// Draw horizontal lines  
		for (int i = 0; i < width; i++)
		{
			SetPixel(x + i, y, colorIndex);
			SetPixel(x + i, y + height - 1, colorIndex);
		}

		// Draw vertical lines  
		for (int i = 1; i < height - 1; i++)
		{
			SetPixel(x, y + i, colorIndex);
			SetPixel(x + width - 1, y + i, colorIndex);
		}
	}

	/// <summary>  
	/// Fills a rectangle with the specified color.  
	/// </summary>  
	/// <param name="x">X-coordinate of the top-left corner.</param>  
	/// <param name="y">Y-coordinate of the top-left corner.</param>  
	/// <param name="width">Width of the rectangle.</param>  
	/// <param name="height">Height of the rectangle.</param>  
	/// <param name="colorIndex">The palette index to use for filling.</param>  
	public void FillRectangle(int x, int y, int width, int height, byte colorIndex)
	{
		// Clip the rectangle to the bounds of the display  
		int startX = Math.Max(0, x);
		int startY = Math.Max(0, y);
		int endX = Math.Min(Width, x + width);
		int endY = Math.Min(Height, y + height);

		// Fill the rectangle with the specified color  
		for (int j = startY; j < endY; j++)
		{
			int rowOffset = j * Width;
			for (int i = startX; i < endX; i++)
			{
				_pixelData[rowOffset + i] = colorIndex;
			}
		}

		_textureNeedsUpdate = true;
	}

	/// <summary>  
	/// Releases the unmanaged resources used by the virtual display.  
	/// </summary>  
	/// <param name="disposing">True if called from Dispose(), false if called from finalizer.</param>  
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

			// Delete OpenGL resources  
			if (_ebo != 0) GL.DeleteBuffer(_ebo);
			if (_vbo != 0) GL.DeleteBuffer(_vbo);
			if (_vao != 0) GL.DeleteVertexArray(_vao);

			_disposedValue = true;
		}
	}

	/// <summary>  
	/// Disposes the virtual display, releasing all resources.  
	/// </summary>  
	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	#endregion
}