using OpenTK.Mathematics;

namespace Critters.Gfx
{
	class Image
	{
		#region Fields

		private readonly int _width;
		private readonly int _height;
		private readonly byte[] _data;

		#endregion

		#region Constructors

		public Image(int width, int height, byte[] data)
		{
			_width = width;
			_height = height;
			_data = data;
		}

		#endregion

		#region Properties
		
		public int Width
		{
			get
			{
				return _width;
			}
		}

		public int Height
		{
			get
			{
				return _height;
			}
		}

		#endregion

		#region Methods

		public Color4 GetPixel(int x, int y)
		{
			int index = (y * _width + x) * 4;

			return new Color4(_data[index], _data[index + 1], _data[index + 2], _data[index + 3]);
		}

		#endregion
	}
}