using System.Collections;
using OpenTK.Graphics.OpenGL4;

namespace Critters.Gfx
{
	/// <summary>
	/// Generate a palette with 6 increments each of red, green, and blue.
	/// </summary>
	class Palette : IReadOnlyList<Color>, IDisposable
	{
		#region Constants

		private const int PALETTE_SIZE = 256;

		#endregion

		#region Fields

		public readonly int Id;
		private readonly List<Color> _colors;
		private bool disposedValue;

		#endregion

		#region Constructors

		public Palette()
		{
			// Generate the colors.
			const int BITS = 6;
			_colors = new List<Color>();
			for (var r = 0; r < BITS; r++) {
				for (var g = 0; g < BITS; g++) {
					for (var b = 0; b < BITS; b++) {
						var rr = r * 255 / (BITS - 1);
						var gg = g * 255 / (BITS - 1);
						var bb = b * 255 / (BITS - 1);

						var mid = (rr * 30 + gg * 59 + bb * 11) / 100;

						var r1 = ~~((rr + mid * 1) / 2 * 230 / 255 + 10);
						var g1 = ~~((gg + mid * 1) / 2 * 230 / 255 + 10);
						var b1 = ~~((bb + mid * 1) / 2 * 230 / 255 + 10);

						_colors.Add(new Color((byte)r1, (byte)g1, (byte)b1));
					}
				}
			}

			while (_colors.Count < PALETTE_SIZE)
			{
				_colors.Add(new Color(0, 0, 0));
			}

			// Generate the reference texture.
			Id = GL.GenTexture();
			if (Id == 0)
			{
				throw new Exception("Unable to generate palette texture.");
			}

			GL.BindTexture(TextureTarget.Texture2D, Id);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

			// Populate palette data
			var paletteData = new byte[PALETTE_SIZE * 3];
			for (int i = 0; i < PALETTE_SIZE; i++)
			{
				var color = _colors[i];
				paletteData[i * 3] = color.Red;
				paletteData[i * 3 + 1] = color.Green;
				paletteData[i * 3 + 2] = color.Blue;
			}

			// Update the palette texture
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, 256, 1, 0, PixelFormat.Rgb, PixelType.UnsignedByte, paletteData);
		}

		#endregion

		#region Properties

		/// <summary>
		/// Retrieve the color associated with a palette index.
		/// </summary>
		public Color this[int index] => _colors[index];

		/// <summary>
		/// Retrieve the palette index associated with a radial RGB value.
		/// </summary>
		/// <param name="r6">0-5</param>
		/// <param name="g6">0-5</param>
		/// <param name="b6">0-5</param>
		/// <returns></returns>
		public byte this[byte r6, byte g6, byte b6] {
			get
			{
				return (byte)(r6 * 6 * 6 + g6 * 6 + b6);
			}
		}

		public int Count => _colors.Count;

		#endregion

		#region Methods

		public IEnumerator<Color> GetEnumerator() => _colors.GetEnumerator();
		
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// Dispose managed state (managed objects)
				}

				// Dispose unmanaged state.
				GL.DeleteTexture(Id);

				disposedValue = true;
			}
		}

		~Palette()
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