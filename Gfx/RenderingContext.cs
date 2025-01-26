using OpenTK.Mathematics;

namespace Critters.Gfx;

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
	public Vector2 ViewportSize => new Vector2(Width, Height);
	public Palette Palette => _display.Palette;

	#endregion

	#region Methods

	/// <summary>
	/// Convert actual screen coordinates to virtual coordinates.
	/// </summary>
	public Vector2 ActualToVirtualPoint(Vector2 actualPoint)
	{
		return _display.ActualToVirtualPoint(actualPoint);
	}

	/// <summary>
	/// Convert virtual coordinates to actual screen coordinates.
	/// </summary>
	public Vector2 VirtualToActualPoint(Vector2 virtualPoint)
	{
		return _display.VirtualToActualPoint(virtualPoint);
	}

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

	public void SetPixel(Vector2 pnt, byte paletteIndex)
	{
		SetPixel((int)pnt.X, (int)pnt.Y, paletteIndex);
	}

	public void SetPixel(int x, int y, byte paletteIndex)
	{
		int index = (y * _display.Width + x) * BPP;
		if (index < 0 || index >= Data.Length)
		{
			return;
		}
		Data[index] = paletteIndex;
		_isDirty = true;
	}

	public byte GetPixel(Vector2 pnt)
	{
		return GetPixel((int)pnt.X, (int)pnt.Y);
	}

	public byte GetPixel(int x, int y)
	{
		int index = (y * _display.Width + x) * BPP;
		if (index < 0 || index >= Data.Length)
		{
			return 0;
		}
		return Data[index];
	}

	public void RenderFilledRect(Box2 bounds, RadialColor color)
	{
		RenderFilledRect(bounds, color.Index);
	}

	public void RenderFilledRect(Box2 box, byte paletteIndex)
	{
		RenderFilledRect(box.Min, box.Max, paletteIndex);
	}

	public void RenderFilledRect(Vector2 pnt1, Vector2 pnt2, byte paletteIndex)
	{
		RenderFilledRect((int)pnt1.X, (int)pnt1.Y, (int)pnt2.X, (int)pnt2.Y, paletteIndex);
	}

	public void RenderFilledRect(int x1, int y1, int x2, int y2, byte paletteIndex)
	{
		if (x1 > x2)
		{
			(x1, x2) = (x2, x1);
		}
		if (y1 > y2)
		{
			(y1, y2) = (y2, y1);
		}

		for (int y = y1; y <= y2; y++)
		{
			for (int x = x1; x <= x2; x++)
			{
				SetPixel(x, y, paletteIndex);
			}
		}
	}

	public void RenderRect(Box2 bounds, RadialColor color)
	{
		RenderRect(bounds, color.Index);
	}

	public void RenderRect(Box2 bounds, byte paletteIndex)
	{
		RenderRect(bounds.Min, bounds.Max, paletteIndex);
	}

	public void RenderRect(Vector2 pnt1, Vector2 pnt2, byte paletteIndex)
	{
		RenderRect((int)pnt1.X, (int)pnt1.Y, (int)pnt2.X, (int)pnt2.Y, paletteIndex);
	}

	public void RenderRect(int x1, int y1, int x2, int y2, byte paletteIndex)
	{
		RenderHLine(x1, x2, y1, paletteIndex);
		RenderHLine(x1, x2, y2, paletteIndex);
		RenderVLine(x1, y1, y2, paletteIndex);
		RenderVLine(x2, y1, y2, paletteIndex);
	}

	public void RenderHLine(Vector2 pnt, int len, byte paletteIndex)
	{
		RenderHLine((int)pnt.X, (int)(pnt.X + len - 1), (int)pnt.Y, paletteIndex);
	}

	public void RenderHLine(int x1, int x2, int y, byte paletteIndex)
	{
		if (x1 > x2)
		{
			(x1, x2) = (x2, x1);
		}

		for (int x = x1; x <= x2; x++)
		{
			SetPixel(x, y, paletteIndex);
		}
	}

	public void RenderVLine(Vector2 pnt, int len, byte paletteIndex)
	{
		RenderVLine((int)pnt.X, (int)pnt.Y, (int)(pnt.Y + len - 1), paletteIndex);
	}

	public void RenderVLine(int x, int y1, int y2, byte paletteIndex)
	{
		if (y1 > y2)
		{
			(y1, y2) = (y2, y1);
		}

		for (int y = y1; y <= y2; y++)
		{
			SetPixel(x, y, paletteIndex);
		}
	}

	public void RenderLine(Vector2 pnt1, Vector2 pnt2, byte paletteIndex)
	{
		RenderLine((int)pnt1.X, (int)pnt1.Y, (int)pnt2.X, (int)pnt2.Y, paletteIndex);
	}

	/// <summary>
	/// Bresenham's line algorithm.
	/// </summary>
	public void RenderLine(int x1, int y1, int x2, int y2, byte paletteIndex)
	{
		int dx = Math.Abs(x2 - x1);
		int sx = x1 < x2 ? 1 : -1;
		int dy = -Math.Abs(y2 - y1);
		int sy = y1 < y2 ? 1 : -1;
		int err = dx + dy;

		while (true)
		{
			SetPixel(x1, y1, paletteIndex);
			if (x1 == x2 && y1 == y2)
			{
				break;
			}
			int e2 = 2 * err;
			if (e2 >= dy)
			{
				err += dy;
				x1 += sx;
			}
			if (e2 <= dx)
			{
				err += dx;
				y1 += sy;
			}
		}
	}

	public void RenderCircle(Vector2 center, int radius, byte paletteIndex)
	{
		RenderCircle((int)center.X, (int)center.Y, radius, paletteIndex);
	}

	public void RenderCircle(int xc, int yc, int radius, byte paletteIndex)
	{
		int x = 0;
		int y = radius;
		int d = 3 - (radius << 1);
		while (y >= x)
		{
			RenderCirclePoints(xc, yc, x, y, paletteIndex);
			x++;

			// Check for decision parameter and correspondingly update d, x, y.
			if (d > 0)
			{
				y--;
				d += ((x - y) << 2) + 10;
			}
			else
			{
				d += (x << 2) + 6;
			}
		}
	}

	private void RenderCirclePoints(int xc, int yc, int x, int y, byte paletteIndex)
	{
		SetPixel(xc + x, yc + y, paletteIndex);
		SetPixel(xc + x, yc - y, paletteIndex);
		SetPixel(xc - x, yc + y, paletteIndex);
		SetPixel(xc - x, yc - y, paletteIndex);
		SetPixel(xc + y, yc + x, paletteIndex);
		SetPixel(xc + y, yc - x, paletteIndex);
		SetPixel(xc - y, yc + x, paletteIndex);
		SetPixel(xc - y, yc - x, paletteIndex);
	}

	public void RenderFilledCircle(int xc, int yc, int radius, byte paletteIndex)
	{
		int x = 0;
		int y = radius;
		int d = 3 - (radius << 1);
		while (y >= x)
		{
			RenderFilledCirclePoints(xc, yc, x, y, paletteIndex);
			x++;

			// Check for decision parameter and correspondingly update d, x, y.
			if (d > 0)
			{
				y--;
				d += ((x - y) << 2) + 10;
			}
			else
			{
				d += (x << 2) + 6;
			}
		}
	}

	private void RenderFilledCirclePoints(int xc, int yc, int x, int y, byte paletteIndex)
	{
		RenderHLine(xc - x, xc + x, yc + y, paletteIndex);
		RenderHLine(xc - x, xc + x, yc - y, paletteIndex);
		RenderHLine(xc - y, xc + y, yc + x, paletteIndex);
		RenderHLine(xc - y, xc + y, yc - x, paletteIndex);
	}

	public void FloodFill(Vector2 pnt, byte paletteIndex)
	{
		FloodFill((int)pnt.X, (int)pnt.Y, paletteIndex);
	}

	public void FloodFill(int x, int y, byte paletteIndex)
	{
		byte targetColor = GetPixel(x, y);
		if (targetColor == paletteIndex)
		{
			return;
		}

		Queue<Vector2> queue = new();
		queue.Enqueue(new Vector2(x, y));

		while (queue.Count > 0)
		{
			Vector2 point = queue.Dequeue();
			x = (int)point.X;
			y = (int)point.Y;

			if (GetPixel(x, y) == targetColor)
			{
				SetPixel(x, y, paletteIndex);

				if (x > 0)
				{
					queue.Enqueue(new Vector2(x - 1, y));
				}
				if (x < Width - 1)
				{
					queue.Enqueue(new Vector2(x + 1, y));
				}
				if (y > 0)
				{
					queue.Enqueue(new Vector2(x, y - 1));
				}
				if (y < Height - 1)
				{
					queue.Enqueue(new Vector2(x, y + 1));
				}
			}
		}
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
/*

void Texture::flood_fill(PointUI origin, Color fill_color, Color border_color) {
	unsigned int size = width * height;
	bool* map_flags = new bool[size];
	std::memset(map_flags, false, size);

	std::queue<PointUI*> queue;

	map_flags[origin.y * width + origin.x] = true;
	queue.push(new PointUI(origin.x, origin.y));

	while (queue.size() > 0) {
		PointUI* point = queue.front();
		queue.pop();
		set_pixel(*point, fill_color);

		for (unsigned int y = point->y - 1; y <= point->y + 1; y++) {
			for (unsigned int x = point->x - 1; x <= point->x + 1; x++) {
				if (math::is_in_range(x, 0u, width - 1) && math::is_in_range(y, 0u, height - 1) && (y == point->y) || (x == point->x)) {
					unsigned int index = y * width + x;
					if (!map_flags[index]) {
						if (get_pixel(x, y) != border_color) {
							map_flags[index] = true;
							queue.push(new PointUI(x, y));
						}
					}
				}
			}
		}

		delete point;
	}

	delete[] map_flags;
}
*/