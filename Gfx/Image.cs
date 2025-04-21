using OpenTK.Mathematics;

namespace Critters.Gfx;

/// <summary>
/// An image is filled with palette-indexed pixel data.
/// </summary>
class Image : IImage<Image, byte>
{
	#region Constants

	private const int BPP = 1;

	#endregion

	#region Fields

	public readonly byte[] Data;

	#endregion

	#region Constructors

	public Image(int width, int height, byte[] data, int scale)
	{
		if (scale < 1)
		{
			throw new ArgumentException("Value must be > 0.", nameof(scale));
		}
		Width = width * scale;
		Height = height * scale;
		Data = new byte[Width * Height];

		for (var y = 0; y < height; y++)
		{
			for (var x = 0; x < width; x++)
			{
				var color = data[y * width + x];

				for (var sy = 0; sy < scale; sy++)
				{
					for (var sx = 0; sx < scale; sx++)
					{
						var dy = y * scale + sy;
						var dx = x * scale + sx;
						Data[dy * Width + dx] = color;
					}
				}
			}
		}
	}

	public Image(int width, int height, byte[] data)
	{
		Width = width;
		Height = height;
		Data = (byte[])data.Clone();
	}

	#endregion

	#region Properties

	public int Width { get; }
	public int Height { get; }

	public byte this[int x, int y]
	{
		get
		{
			return GetPixel(x, y);
		}
		set
		{
			SetPixel(x, y, value);
		}
	}

	#endregion

	#region Methods

	// public void Render(RenderingContext rc, int x, int y)
	// {
	// 	if (x + Width < 0 || y + Height < 0 || x >= rc.Width || y >= rc.Height)
	// 	{
	// 		// The image is completely offscreen.
	// 		return;
	// 	}

	// 	var srcIndex = 0;
	// 	var dstIndex = (y * rc.Width + x) * BPP;
	// 	var len = Width * BPP;
	// 	if (x + len > rc.Width)
	// 	{
	// 		len = rc.Width - x;
	// 	}

	// 	for (var dy = 0; dy < Height; dy++)
	// 	{
	// 		if (y + dy >= rc.Height)
	// 		{
	// 			break;
	// 		}

	// 		Array.Copy(Data, srcIndex, rc.Data, dstIndex, len);
	// 		srcIndex += len;
	// 		dstIndex += rc.Width * BPP;
	// 	}
	// }


	public void Render(IRenderingContext rc, Vector2 position)
	{
		Render(rc, (int)position.X, (int)position.Y);
	}

	/// <summary>
	/// A value of 255 in either color indicates transparent.
	/// </summary>
	public void Render(IRenderingContext rc, int x, int y)
	{
		for (var dy = 0; dy < Height; dy++)
		{
			if (y + dy < 0)
			{
				continue;
			}
			if (y + dy >= rc.Height)
			{
				break;
			}
			for (var dx = 0; dx < Width; dx++)
			{
				if (x + dx < 0)
				{
					continue;
				}
				if (x + dx >= rc.Width)
				{
					break;
				}
				var color = GetPixel(dx, dy);
				if (color < 255)
				{
					rc.SetPixel(x + dx, y + dy, color);
				}
			}
		}
	}

	public void Recolor(byte oldPaletteIndex, byte newPaletteIndex)
	{
		for (var i = 0; i < Data.Length; i++)
		{
			if (Data[i] == oldPaletteIndex)
			{
				Data[i] = newPaletteIndex;
			}
		}
	}

	public byte GetPixel(int x, int y)
	{
		var index = (y * Width + x) * BPP;
		return Data[index];
	}

	public void SetPixel(int x, int y, byte color)
	{
		var index = (y * Width + x) * BPP;
		Data[index] = color;
	}

	/// <summary>
	/// Create a new image from a rectangle of this image.
	/// </summary>
	public Image Crop(int x, int y, int width, int height)
	{
		var data = new byte[width * height * BPP];

		for (var i = 0; i < height; i++)
		{
			for (var j = 0; j < width; j++)
			{
				var color = GetPixel(x + j, y + i);
				var index = (i * width + j) * BPP;
				data[index] = color;
			}
		}

		return new Image(width, height, data);
	}

	public Image Scale(int factor)
	{
		if (factor < 1)
		{
			throw new ArgumentException("Value must be > 0.", nameof(factor));
		}
		return new Image(Width, Height, Data, factor);
	}

	#endregion
}
