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

	public void DrawFilledRect(Box2 box, byte paletteIndex)
	{
		DrawFilledRect(box.Min, box.Max, paletteIndex);
	}

	public void DrawFilledRect(Vector2 pnt1, Vector2 pnt2, byte paletteIndex)
	{
		DrawFilledRect((int)pnt1.X, (int)pnt1.Y, (int)pnt2.X, (int)pnt2.Y, paletteIndex);
	}

	public void DrawFilledRect(int x1, int y1, int x2, int y2, byte paletteIndex)
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

	public void DrawRect(Box2 box, byte paletteIndex)
	{
		DrawRect(box.Min, box.Max, paletteIndex);
	}

	public void DrawRect(Vector2 pnt1, Vector2 pnt2, byte paletteIndex)
	{
		DrawRect((int)pnt1.X, (int)pnt1.Y, (int)pnt2.X, (int)pnt2.Y, paletteIndex);
	}

	public void DrawRect(int x1, int y1, int x2, int y2, byte paletteIndex)
	{
		DrawHLine(x1, x2, y1, paletteIndex);
		DrawHLine(x1, x2, y2, paletteIndex);
		DrawVLine(x1, y1, y2, paletteIndex);
		DrawVLine(x2, y1, y2, paletteIndex);
	}

	public void DrawHLine(Vector2 pnt, int len, byte paletteIndex)
	{
		DrawHLine((int)pnt.X, (int)(pnt.X + len - 1), (int)pnt.Y, paletteIndex);
	}

	public void DrawHLine(int x1, int x2, int y, byte paletteIndex)
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

	public void DrawVLine(Vector2 pnt, int len, byte paletteIndex)
	{
		DrawVLine((int)pnt.X, (int)pnt.Y, (int)(pnt.Y + len - 1), paletteIndex);
	}

	public void DrawVLine(int x, int y1, int y2, byte paletteIndex)
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

	public void DrawLine(Vector2 pnt1, Vector2 pnt2, byte paletteIndex)
	{
		DrawLine((int)pnt1.X, (int)pnt1.Y, (int)pnt2.X, (int)pnt2.Y, paletteIndex);
	}

	/// <summary>
	/// Bresenham's line algorithm.
	/// </summary>
	public void DrawLine(int x1, int y1, int x2, int y2, byte paletteIndex)
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

	public void DrawCircle(Vector2 center, int radius, byte paletteIndex)
	{
		DrawCircle((int)center.X, (int)center.Y, radius, paletteIndex);
	}

	public void DrawCircle(int xc, int yc, int radius, byte paletteIndex)
	{
		int x = 0;
		int y = radius;
		int d = 3 - (radius << 1);
		while (y >= x)
		{
			DrawCirclePoints(xc, yc, x, y, paletteIndex);
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

	private void DrawCirclePoints(int xc, int yc, int x, int y, byte paletteIndex)
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

	public void DrawFilledCircle(int xc, int yc, int radius, byte paletteIndex)
	{
		int x = 0;
		int y = radius;
		int d = 3 - (radius << 1);
		while (y >= x)
		{
			DrawFilledCirclePoints(xc, yc, x, y, paletteIndex);
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

	private void DrawFilledCirclePoints(int xc, int yc, int x, int y, byte paletteIndex)
	{
		DrawHLine(xc - x, xc + x, yc + y, paletteIndex);
		DrawHLine(xc - x, xc + x, yc - y, paletteIndex);
		DrawHLine(xc - y, xc + y, yc + x, paletteIndex);
		DrawHLine(xc - y, xc + y, yc - x, paletteIndex);
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