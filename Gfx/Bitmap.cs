using OpenTK.Mathematics;

namespace Critters.Gfx;

/// <summary>
/// A bitmap is filled with 2-bit image data.
/// 
/// You can build one from an Image.  Black pixels are false, non-black are true.
/// </summary>
class Bitmap : IImage<Bitmap, bool>
{
	#region Constants

	private const int BPP = 1;

	#endregion

	#region Fields

	public readonly bool[] Data;

	#endregion

	#region Constructors

	public Bitmap(Bitmap? image, int scale = 1)
	{
		if (image == null)
		{
			throw new ArgumentNullException(nameof(image));
		}
		if (scale < 1)
		{
			throw new ArgumentException("Value must be > 0.", nameof(scale));
		}
		Width = image.Width * scale;
		Height = image.Height * scale;
		Data = new bool[Width * Height];
		
		for (var y = 0; y < image.Height; y++)
		{
			for (var x = 0; x < image.Width; x++)
			{
				var color = image.GetPixel(x, y);
				
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

	public Bitmap(Image? image, int scale = 1)
	{
		if (image == null)
		{
			throw new ArgumentNullException(nameof(image));
		}
		if (scale < 1)
		{
			throw new ArgumentException("Value must be > 0.", nameof(scale));
		}
		Width = image.Width * scale;
		Height = image.Height * scale;
		Data = new bool[Width * Height];
		
		for (var y = 0; y < image.Height; y++)
		{
			for (var x = 0; x < image.Width; x++)
			{
				var color = image.GetPixel(x, y);
				
				for (var sy = 0; sy < scale; sy++)
				{
					for (var sx = 0; sx < scale; sx++)
					{
						var dy = y * scale + sy;
						var dx = x * scale + sx;
						Data[dy * Width + dx] = color > 0;
					}
				}
			}
		}
	}

	public Bitmap(int width, int height, bool[] data)
	{
		Width = width;
		Height = height;
		Data = data;
	}

	#endregion

	#region Properties

	public int Width { get; }
	public int Height { get; }

	public bool this[int x, int y]
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

	public void Render(RenderingContext rc, Vector2 position, RadialColor fgColor, RadialColor bgColor)
	{
		Render(rc, position, fgColor.Index, bgColor.Index);
	}

	public void Render(RenderingContext rc, Vector2 position, byte fgColor, byte bgColor)
	{
		Render(rc, (int)position.X, (int)position.Y, fgColor, bgColor);
	}

	/// <summary>
	/// Render a bitmap with the set foreground and background colors.
	/// A value of 255 in either color indicates transparent.
	/// </summary>
	public void Render(RenderingContext rc, int x, int y, byte fgColor, byte bgColor)
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
				var value = GetPixel(dx, dy);
				if (value)
				{
					if (fgColor < 255)
					{
						rc.SetPixel(x + dx, y + dy, fgColor);
					}
				}
				else
				{
					if (bgColor < 255)
					{
						rc.SetPixel(x + dx, y + dy, bgColor);
					}
				}
			}
		}
	}

	public bool GetPixel(int x, int y)
	{
		var index = (y * Width + x) * BPP;
		return Data[index];
	}

	public void SetPixel(int x, int y, bool value)
	{
		var index = (y * Width + x) * BPP;
		Data[index] = value;
	}

	/// <summary>
	/// Create a new image from a rectangle of this image.
	/// </summary>
	public Bitmap Crop(int x, int y, int width, int height)
	{
		var data = new bool[width * height * BPP];

		for (var i = 0; i < height; i++)
		{
			for (var j = 0; j < width; j++)
			{
				var value = GetPixel(x + j, y + i);
				var index = (i * width + j) * BPP;
				data[index] = value;
			}
		}

		return new Bitmap(width, height, data);
	}

	public Bitmap Scale(int factor)
	{
		if (factor < 1)
		{
			throw new ArgumentException("Value must be > 0.", nameof(factor));
		}
		return new Bitmap(this, factor);
	}

	#endregion
}