using OpenTK.Mathematics;

namespace Critters.Gfx
{
	class RenderingContext
	{
		#region Constants

		private const int BPP = 1;

		#endregion

		#region Fields

		private readonly VirtualDisplay _display;
		private bool _isDirty = true;
		public byte[] Data;

		#endregion

		#region Constructors

		public RenderingContext(VirtualDisplay display)
		{
			_display = display;
			Data = new byte[display.Width * display.Height * BPP];
		}

		#endregion

		#region Properties

		public int Width => _display.Width;
		public int Height => _display.Height;

		#endregion

		#region Methods

		public void Fill(byte paletteIndex)
		{
			for (int i = 0; i < Data.Length; i += BPP)
			{
				Data[i] = paletteIndex;
			}
			_isDirty = true;
		}

		public void Clear()
		{
			Fill(0);
		}

		public void SetPixel(int x, int y, byte paletteIndex)
		{
			int index = (y * _display.Width + x) * BPP;
			Data[index] = paletteIndex;
			_isDirty = true;
		}

		public void Present()
		{
			if (_isDirty)
			{
				_display.UpdatePixels(Data);
				_isDirty = false;
			}
		}

		#endregion
	}
}