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

	public Bitmap(Image image)
	{
		Width = image.Width;
		Height = image.Height;
		Data = new bool[Width * Height];
		
		for (var y = 0; y < Height; y++)
		{
			for (var x = 0; x < Width; x++)
			{
				var color = image.GetPixel(x, y);
				Data[y * Width + x] = color.Red > 0 || color.Green > 0 || color.Blue > 0;
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

	public void Draw(RenderingContext rc, int x, int y, byte fgColor, byte bgColor)
	{
		for (var dy = 0; dy < Height; dy++)
		{
			for (var dx = 0; dx < Width; dx++)
			{
				var value = GetPixel(dx, dy);
				rc.SetPixel(x + dx, y + dy, value ? fgColor : bgColor);
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

	#endregion
}