using Gfx;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Critters.Gfx
{
	class VirtualDisplay : IDisposable
	{
		#region Fields

		private static readonly float[] QuadVertices = 
		{
			// Positions     // Texture Coords
			-1.0f, -1.0f,    0.0f, 1.0f, // Bottom-left
			 1.0f, -1.0f,    1.0f, 1.0f, // Bottom-right
			 1.0f,  1.0f,    1.0f, 0.0f, // Top-right
			-1.0f,  1.0f,    0.0f, 0.0f  // Top-left
		};

		private static readonly uint[] QuadIndices =
		{
			0, 1, 2,
			2, 3, 0
		};

		public readonly Palette Palette;
		private readonly int _virtualWidth;
		private readonly int _virtualHeight;
		private int _textureId;
		private int _vao;
		private int _vbo;
		private int _ebo;
		private ShaderProgram _shaderProgram;
		private bool disposedValue;

		#endregion

		#region Constructors

		public VirtualDisplay(Settings.VirtualDisplaySettings settings)
		{
				_virtualWidth = settings.Width;
				_virtualHeight = settings.Height;

				// Compile shaders
				_shaderProgram = new ShaderProgram(settings.VertexShaderPath, settings.FragmentShaderPath);

				// Generate texture
				_textureId = GL.GenTexture();
				if (_textureId == 0)
				{
					throw new Exception("Failed to generate texture.");
				}

				GL.BindTexture(TextureTarget.Texture2D, _textureId);
				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R8, _virtualWidth, _virtualHeight, 0, PixelFormat.Red, PixelType.UnsignedByte, IntPtr.Zero);

				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

				// Create VAO, VBO, and EBO
				_vao = GL.GenVertexArray();
				_vbo = GL.GenBuffer();
				_ebo = GL.GenBuffer();

				GL.BindVertexArray(_vao);

				// Bind vertex buffer
				GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
				GL.BufferData(BufferTarget.ArrayBuffer, QuadVertices.Length * sizeof(float), QuadVertices, BufferUsageHint.StaticDraw);

				// Bind element buffer
				GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
				GL.BufferData(BufferTarget.ElementArrayBuffer, QuadIndices.Length * sizeof(uint), QuadIndices, BufferUsageHint.StaticDraw);

				// Set vertex attribute pointers
				GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
				GL.EnableVertexAttribArray(0);

				GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
				GL.EnableVertexAttribArray(1);

				GL.BindVertexArray(0);

				Palette = new Palette();
		}

		#endregion

		#region Properties

		public int Width
		{
			get
			{
				return _virtualWidth;
			}
		}

		public int Height
		{
			get
			{
				return _virtualHeight;
			}
		}

		#endregion

		#region Methods

		public void UpdatePixels(byte[] pixelData)
		{
			GL.BindTexture(TextureTarget.Texture2D, _textureId);
			GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, _virtualWidth, _virtualHeight, PixelFormat.Red, PixelType.UnsignedByte, pixelData);
		}

		public void Render(Vector2i windowSize)
		{
			GL.Clear(ClearBufferMask.ColorBufferBit);

			// Calculate aspect ratios
			var virtualAspect = (float)_virtualWidth / _virtualHeight;
			var windowAspect = (float)windowSize.X / windowSize.Y;

			// Calculate scaling factors
			float scale;
			var xPadding = 0f;
			var yPadding = 0f;

			if (windowAspect > virtualAspect)
			{
				// Window is wider than the virtual display
				scale = (float)windowSize.Y / _virtualHeight;
				xPadding = (windowSize.X - _virtualWidth * scale) / 2f;
			}
			else
			{
				// Window is taller than the virtual display
				scale = (float)windowSize.X / _virtualWidth;
				yPadding = (windowSize.Y - _virtualHeight * scale) / 2f;
			}

			// Set the viewport with padding
			GL.Viewport((int)xPadding, (int)yPadding, (int)(_virtualWidth * scale), (int)(_virtualHeight * scale));
			
			// Use shader and VAO
			_shaderProgram.Use();
			GL.BindVertexArray(_vao);

			// Bind _paletteTextureId to the uPalette sampler2d shader variable.
			GL.ActiveTexture(TextureUnit.Texture1);
			GL.BindTexture(TextureTarget.Texture2D, Palette.Id);
			GL.Uniform1(_shaderProgram.GetUniformLocation("uPalette"), 1);

			// Bind texture
			GL.ActiveTexture(TextureUnit.Texture0);
			GL.BindTexture(TextureTarget.Texture2D, _textureId);
			GL.Uniform1(_shaderProgram.GetUniformLocation("uTexture"), 0);

			// Draw quad
			GL.DrawElements(PrimitiveType.Triangles, QuadIndices.Length, DrawElementsType.UnsignedInt, 0);

			// Unbind VAO and shader
			GL.BindVertexArray(0);
			GL.UseProgram(0);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					_shaderProgram.Dispose();
					Palette.Dispose();
				}

				GL.DeleteTexture(_textureId);
				GL.DeleteBuffer(_ebo);
				GL.DeleteBuffer(_vao);
				GL.DeleteBuffer(_vbo);

				disposedValue = true;
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
}
