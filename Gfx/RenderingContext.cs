using OpenTK.Mathematics;

namespace Critters.Gfx
{
	class RenderingContext
	{
		#region Fields

		private readonly VirtualDisplay _display;
		private bool _isDirty = true;
		private byte[] _pixelData;

		#endregion

		#region Constructors

		public RenderingContext(VirtualDisplay display)
		{
			_display = display;
			_pixelData = new byte[display.Width * display.Height * 3];
		}

		#endregion

		#region Methods

		public void Fill(Color color)
		{
			for (int i = 0; i < _pixelData.Length; i += 3)
			{
				_pixelData[i] = color.Red;
				_pixelData[i + 1] = color.Green;
				_pixelData[i + 2] = color.Blue;
			}
			_isDirty = true;
		}

		public void Clear()
		{
			Fill(new Color(0, 0, 0));
		}

		public void SetPixel(int x, int y, Color color)
		{
			int index = (y * _display.Width + x) * 3;
			_pixelData[index] = color.Red;
			_pixelData[index + 1] = color.Green;
			_pixelData[index + 2] = color.Blue;
			_isDirty = true;
		}

		public void Present()
		{
			if (_isDirty)
			{
				_display.UpdatePixels(_pixelData);
				_isDirty = false;
			}
		}

		#endregion
	}
}