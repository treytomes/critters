using System.Collections;

namespace Critters.Gfx
{
	/// <summary>
	/// Generate a palette with 6 increments each of red, green, and blue.
	/// </summary>
	class Palette : IReadOnlyList<Color>
	{
		#region Fields

		private readonly List<Color> _colors;

		#endregion

		#region Constructors

		public Palette()
		{
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
		}

		#endregion

		#region Properties

		public Color this[int index] => _colors[index];

		public Color this[int r6, int g6, int b6] {
			get
			{
				var index = GetIndex(r6, g6, b6);
				return _colors[index];
			}
		}

		public int Count => _colors.Count;

		#endregion

		#region Methods

		public int GetIndex(int r6, int g6, int b6) => r6 * 6 * 6 + g6 * 6 + b6;

		public IEnumerator<Color> GetEnumerator() => _colors.GetEnumerator();
		
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		#endregion
	}
}