namespace Critters.Gfx;

/// <summary>
/// An image is filled with RGB pixel data.
/// </summary>
class Image : IImage<Image, Color>
{
	#region Constants

	private const int BPP = 1;

	#endregion

	#region Fields

	public readonly byte[] Data;

	#endregion

	#region Constructors

	public Image(int width, int height, byte[] data)
	{
		Width = width;
		Height = height;
		Data = data;
	}

	#endregion

	#region Properties

	public int Width { get; }
	public int Height { get; }

	public Color this[int x, int y]
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

	public void Draw(RenderingContext rc, int x, int y)
	{
		var srcIndex = 0;
		var dstIndex = (y * rc.Width + x) * BPP;
		var len = Width * BPP;
		for (var dy = 0; dy < Height; dy++)
		{
			Array.Copy(Data, srcIndex, rc.Data, dstIndex, len);
			srcIndex += len;
			dstIndex += rc.Width * BPP;
		}
	}

	public Color GetPixel(int x, int y)
	{
		var index = (y * Width + x) * BPP;
		return new Color(Data[index], Data[index + 1], Data[index + 2]);
	}

	public void SetPixel(int x, int y, Color color)
	{
		var index = (y * Width + x) * BPP;
		Data[index] = (byte)(color.Red * 255);
		Data[index + 1] = (byte)(color.Green * 255);
		Data[index + 2] = (byte)(color.Blue * 255);
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

				data[index] = color.Red;
				data[index + 1] = color.Green;
				data[index + 2] = color.Blue;
			}
		}

		return new Image(width, height, data);
	}

	#endregion
}
