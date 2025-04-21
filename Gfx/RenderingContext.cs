using Microsoft.Extensions.Logging;
using OpenTK.Mathematics;

namespace Critters.Gfx;

/// <summary>  
/// Provides a high-level drawing API for rendering to a VirtualDisplay.  
/// </summary>  
class RenderingContext : IRenderingContext
{
	#region Constants  

	private const int BPP = 1; // Bytes per pixel  

	#endregion

	#region Fields  

	private readonly IVirtualDisplay _display;
	private readonly ILogger<RenderingContext> _logger;
	private bool _isDirty = true;
	private readonly byte[] _data;
	private bool _disposed;

	#endregion

	#region Constructors  

	/// <summary>  
	/// Creates a new rendering context for the specified virtual display.  
	/// </summary>  
	/// <param name="display">The virtual display to render to.</param>  
	/// <exception cref="ArgumentNullException">Thrown if display is null.</exception>  
	public RenderingContext(IVirtualDisplay display, ILogger<RenderingContext> logger)
	{
		_display = display ?? throw new ArgumentNullException(nameof(display));
		_logger = logger;
		_data = new byte[display.Width * display.Height * BPP];
	}

	#endregion

	#region Properties  

	/// <summary>  
	/// Gets the width of the rendering context in pixels.  
	/// </summary>  
	public int Width => _display.Width;

	/// <summary>  
	/// Gets the height of the rendering context in pixels.  
	/// </summary>  
	public int Height => _display.Height;

	/// <summary>  
	/// Gets the size of the viewport as a Vector2.  
	/// </summary>  
	public Vector2 ViewportSize => new(Width, Height);

	/// <summary>  
	/// Gets a box representing the bounds of the rendering context.  
	/// </summary>  
	public Box2 Bounds => new(0, 0, Width, Height);

	/// <summary>  
	/// Gets the color palette used by the rendering context.  
	/// </summary>  
	public Palette Palette => _display.Palette;

	#endregion

	#region Methods  

	/// <summary>  
	/// Convert actual screen coordinates to virtual coordinates.  
	/// </summary>  
	/// <param name="actualPoint">The point in actual screen coordinates.</param>  
	/// <returns>The corresponding point in virtual display coordinates.</returns>  
	public Vector2 ActualToVirtualPoint(Vector2 actualPoint)
	{
		return _display.ActualToVirtualPoint(actualPoint);
	}

	/// <summary>  
	/// Convert virtual coordinates to actual screen coordinates.  
	/// </summary>  
	/// <param name="virtualPoint">The point in virtual display coordinates.</param>  
	/// <returns>The corresponding point in actual screen coordinates.</returns>  
	public Vector2 VirtualToActualPoint(Vector2 virtualPoint)
	{
		return _display.VirtualToActualPoint(virtualPoint);
	}

	/// <summary>  
	/// Fills the entire rendering context with the specified color.  
	/// </summary>  
	/// <param name="paletteIndex">The palette index to fill with.</param>  
	public void Fill(byte paletteIndex)
	{
		Array.Fill(_data, paletteIndex);
		_isDirty = true;
	}

	/// <summary>  
	/// Fills the entire rendering context with the specified color.  
	/// </summary>  
	/// <param name="color">The color to fill with.</param>  
	public void Fill(RadialColor color)
	{
		Fill(color.Index);
	}

	/// <summary>  
	/// Clears the rendering context (fills with color index 0).  
	/// </summary>  
	public void Clear()
	{
		Fill(0);
	}

	/// <summary>  
	/// Sets a pixel at the specified position to the specified color.  
	/// </summary>  
	/// <param name="pnt">The position of the pixel.</param>  
	/// <param name="color">The color to set.</param>  
	public void SetPixel(Vector2 pnt, RadialColor color)
	{
		SetPixel((int)pnt.X, (int)pnt.Y, color.Index);
	}

	/// <summary>  
	/// Sets a pixel at the specified position to the specified palette index.  
	/// </summary>  
	/// <param name="pnt">The position of the pixel.</param>  
	/// <param name="paletteIndex">The palette index to set.</param>  
	public void SetPixel(Vector2 pnt, byte paletteIndex)
	{
		SetPixel((int)pnt.X, (int)pnt.Y, paletteIndex);
	}

	/// <summary>  
	/// Sets a pixel at the specified coordinates to the specified palette index.  
	/// </summary>  
	/// <param name="x">The x-coordinate of the pixel.</param>  
	/// <param name="y">The y-coordinate of the pixel.</param>  
	/// <param name="paletteIndex">The palette index to set.</param>  
	public void SetPixel(int x, int y, byte paletteIndex)
	{
		if (x < 0 || x >= Width || y < 0 || y >= Height)
		{
			return;
		}

		int index = (y * Width + x) * BPP;
		if (index < 0 || index >= _data.Length)
		{
			return;
		}

		_data[index] = paletteIndex;
		_isDirty = true;
	}

	/// <summary>  
	/// Gets the palette index of the pixel at the specified position.  
	/// </summary>  
	/// <param name="pnt">The position of the pixel.</param>  
	/// <returns>The palette index of the pixel, or 0 if out of bounds.</returns>  
	public byte GetPixel(Vector2 pnt)
	{
		return GetPixel((int)pnt.X, (int)pnt.Y);
	}

	/// <summary>  
	/// Gets the palette index of the pixel at the specified coordinates.  
	/// </summary>  
	/// <param name="x">The x-coordinate of the pixel.</param>  
	/// <param name="y">The y-coordinate of the pixel.</param>  
	/// <returns>The palette index of the pixel, or 0 if out of bounds.</returns>  
	public byte GetPixel(int x, int y)
	{
		if (x < 0 || x >= Width || y < 0 || y >= Height)
		{
			return 0;
		}

		int index = (y * Width + x) * BPP;
		if (index < 0 || index >= _data.Length)
		{
			return 0;
		}

		return _data[index];
	}

	/// <summary>  
	/// Renders a filled rectangle with the specified bounds and color.  
	/// </summary>  
	/// <param name="bounds">The bounds of the rectangle.</param>  
	/// <param name="color">The color to fill with.</param>  
	public void RenderFilledRect(Box2 bounds, RadialColor color)
	{
		RenderFilledRect(bounds, color.Index);
	}

	/// <summary>  
	/// Renders a filled rectangle with the specified bounds and palette index.  
	/// </summary>  
	/// <param name="box">The bounds of the rectangle.</param>  
	/// <param name="paletteIndex">The palette index to fill with.</param>  
	public void RenderFilledRect(Box2 box, byte paletteIndex)
	{
		RenderFilledRect(box.Min, box.Max, paletteIndex);
	}

	/// <summary>  
	/// Renders a filled rectangle with the specified corners and palette index.  
	/// </summary>  
	/// <param name="pnt1">The first corner of the rectangle.</param>  
	/// <param name="pnt2">The opposite corner of the rectangle.</param>  
	/// <param name="paletteIndex">The palette index to fill with.</param>  
	public void RenderFilledRect(Vector2 pnt1, Vector2 pnt2, byte paletteIndex)
	{
		RenderFilledRect((int)pnt1.X, (int)pnt1.Y, (int)pnt2.X, (int)pnt2.Y, paletteIndex);
	}

	/// <summary>  
	/// Renders a filled rectangle with the specified corners and palette index.  
	/// </summary>  
	/// <param name="x1">The x-coordinate of the first corner.</param>  
	/// <param name="y1">The y-coordinate of the first corner.</param>  
	/// <param name="x2">The x-coordinate of the opposite corner.</param>  
	/// <param name="y2">The y-coordinate of the opposite corner.</param>  
	/// <param name="paletteIndex">The palette index to fill with.</param>  
	public void RenderFilledRect(int x1, int y1, int x2, int y2, byte paletteIndex)
	{
		// Ensure x1 <= x2 and y1 <= y2  
		if (x1 > x2)
		{
			(x1, x2) = (x2, x1);
		}
		if (y1 > y2)
		{
			(y1, y2) = (y2, y1);
		}

		// Clip to bounds  
		x1 = Math.Max(0, Math.Min(Width - 1, x1));
		y1 = Math.Max(0, Math.Min(Height - 1, y1));
		x2 = Math.Max(0, Math.Min(Width - 1, x2));
		y2 = Math.Max(0, Math.Min(Height - 1, y2));

		// Optimized direct buffer access for filled rectangle  
		for (int y = y1; y <= y2; y++)
		{
			int rowOffset = y * Width * BPP;
			for (int x = x1; x <= x2; x++)
			{
				_data[rowOffset + x * BPP] = paletteIndex;
			}
		}

		_isDirty = true;
	}

	/// <summary>  
	/// Renders a rectangle outline with the specified bounds and color.  
	/// </summary>  
	/// <param name="bounds">The bounds of the rectangle.</param>  
	/// <param name="color">The color of the outline.</param>  
	public void RenderRect(Box2 bounds, RadialColor color)
	{
		RenderRect(bounds, color.Index);
	}

	/// <summary>  
	/// Renders a rectangle outline with the specified bounds and palette index.  
	/// </summary>  
	/// <param name="bounds">The bounds of the rectangle.</param>  
	/// <param name="paletteIndex">The palette index of the outline.</param>  
	public void RenderRect(Box2 bounds, byte paletteIndex)
	{
		RenderRect(bounds.Min, bounds.Max, paletteIndex);
	}

	/// <summary>  
	/// Renders a rectangle outline with the specified corners and palette index.  
	/// </summary>  
	/// <param name="pnt1">The first corner of the rectangle.</param>  
	/// <param name="pnt2">The opposite corner of the rectangle.</param>  
	/// <param name="paletteIndex">The palette index of the outline.</param>  
	public void RenderRect(Vector2 pnt1, Vector2 pnt2, byte paletteIndex)
	{
		RenderRect((int)pnt1.X, (int)pnt1.Y, (int)pnt2.X, (int)pnt2.Y, paletteIndex);
	}

	/// <summary>  
	/// Renders a rectangle outline with the specified corners and palette index.  
	/// </summary>  
	/// <param name="x1">The x-coordinate of the first corner.</param>  
	/// <param name="y1">The y-coordinate of the first corner.</param>  
	/// <param name="x2">The x-coordinate of the opposite corner.</param>  
	/// <param name="y2">The y-coordinate of the opposite corner.</param>  
	/// <param name="paletteIndex">The palette index of the outline.</param>  
	public void RenderRect(int x1, int y1, int x2, int y2, byte paletteIndex)
	{
		RenderHLine(x1, x2, y1, paletteIndex);
		RenderHLine(x1, x2, y2, paletteIndex);
		RenderVLine(x1, y1, y2, paletteIndex);
		RenderVLine(x2, y1, y2, paletteIndex);
	}

	/// <summary>  
	/// Renders a horizontal line with the specified starting point, length, and palette index.  
	/// </summary>  
	/// <param name="pnt">The starting point of the line.</param>  
	/// <param name="len">The length of the line in pixels.</param>  
	/// <param name="paletteIndex">The palette index of the line.</param>  
	public void RenderHLine(Vector2 pnt, int len, byte paletteIndex)
	{
		RenderHLine((int)pnt.X, (int)(pnt.X + len - 1), (int)pnt.Y, paletteIndex);
	}

	/// <summary>  
	/// Renders a horizontal line with the specified endpoints and palette index.  
	/// </summary>  
	/// <param name="x1">The x-coordinate of the start point.</param>  
	/// <param name="x2">The x-coordinate of the end point.</param>  
	/// <param name="y">The y-coordinate of the line.</param>  
	/// <param name="paletteIndex">The palette index of the line.</param>  
	public void RenderHLine(int x1, int x2, int y, byte paletteIndex)
	{
		if (y < 0 || y >= Height)
			return;

		if (x1 > x2)
			(x1, x2) = (x2, x1);

		x1 = Math.Max(0, x1);
		x2 = Math.Min(Width - 1, x2);

		// Optimized direct buffer access  
		int offset = y * Width * BPP;
		for (int x = x1; x <= x2; x++)
		{
			_data[offset + x * BPP] = paletteIndex;
		}

		_isDirty = true;
	}

	/// <summary>  
	/// Renders a vertical line with the specified starting point, length, and palette index.  
	/// </summary>  
	/// <param name="pnt">The starting point of the line.</param>  
	/// <param name="len">The length of the line in pixels.</param>  
	/// <param name="paletteIndex">The palette index of the line.</param>  
	public void RenderVLine(Vector2 pnt, int len, byte paletteIndex)
	{
		RenderVLine((int)pnt.X, (int)pnt.Y, (int)(pnt.Y + len - 1), paletteIndex);
	}

	/// <summary>  
	/// Renders a vertical line with the specified endpoints and palette index.  
	/// </summary>  
	/// <param name="x">The x-coordinate of the line.</param>  
	/// <param name="y1">The y-coordinate of the start point.</param>  
	/// <param name="y2">The y-coordinate of the end point.</param>  
	/// <param name="paletteIndex">The palette index of the line.</param>  
	public void RenderVLine(int x, int y1, int y2, byte paletteIndex)
	{
		if (x < 0 || x >= Width)
			return;

		if (y1 > y2)
			(y1, y2) = (y2, y1);

		y1 = Math.Max(0, y1);
		y2 = Math.Min(Height - 1, y2);

		// Direct buffer access  
		for (int y = y1; y <= y2; y++)
		{
			_data[y * Width * BPP + x * BPP] = paletteIndex;
		}

		_isDirty = true;
	}

	/// <summary>  
	/// Renders a line between two points with the specified palette index.  
	/// </summary>  
	/// <param name="pnt1">The start point of the line.</param>  
	/// <param name="pnt2">The end point of the line.</param>  
	/// <param name="paletteIndex">The palette index of the line.</param>  
	public void RenderLine(Vector2 pnt1, Vector2 pnt2, byte paletteIndex)
	{
		RenderLine((int)pnt1.X, (int)pnt1.Y, (int)pnt2.X, (int)pnt2.Y, paletteIndex);
	}

	/// <summary>  
	/// Renders a line between two points with the specified color.  
	/// </summary>  
	/// <param name="pnt1">The start point of the line.</param>  
	/// <param name="pnt2">The end point of the line.</param>  
	/// <param name="color">The color of the line.</param>  
	public void RenderLine(Vector2 pnt1, Vector2 pnt2, RadialColor color)
	{
		RenderLine((int)pnt1.X, (int)pnt1.Y, (int)pnt2.X, (int)pnt2.Y, color.Index);
	}

	/// <summary>  
	/// Renders a line between two points with the specified palette index using Bresenham's algorithm.  
	/// </summary>  
	/// <param name="x1">The x-coordinate of the start point.</param>  
	/// <param name="y1">The y-coordinate of the start point.</param>  
	/// <param name="x2">The x-coordinate of the end point.</param>  
	/// <param name="y2">The y-coordinate of the end point.</param>  
	/// <param name="paletteIndex">The palette index of the line.</param>  
	public void RenderLine(int x1, int y1, int x2, int y2, byte paletteIndex)
	{
		// Bresenham's line algorithm  
		int dx = Math.Abs(x2 - x1);
		int sx = x1 < x2 ? 1 : -1;
		int dy = -Math.Abs(y2 - y1);
		int sy = y1 < y2 ? 1 : -1;
		int err = dx + dy;

		while (true)
		{
			SetPixel(x1, y1, paletteIndex);
			if (x1 == x2 && y1 == y2)
				break;

			int e2 = 2 * err;
			if (e2 >= dy)
			{
				if (x1 == x2)
					break;
				err += dy;
				x1 += sx;
			}
			if (e2 <= dx)
			{
				if (y1 == y2)
					break;
				err += dx;
				y1 += sy;
			}
		}
	}

	/// <summary>  
	/// Renders a circle with ordered dithering for a soft edge effect.  
	/// </summary>  
	/// <param name="center">The center of the circle.</param>  
	/// <param name="radius">The radius of the circle.</param>  
	/// <param name="color">The primary color of the circle.</param>  
	/// <param name="falloffStart">The point at which the falloff begins (0.0-1.0).</param>  
	/// <param name="secondaryColor">Optional secondary color for the dithered region.</param>  
	public void RenderOrderedDitheredCircle(Vector2 center, int radius, RadialColor color, float falloffStart = 0.6f, RadialColor? secondaryColor = null)
	{
		// Bayer 4x4 dithering matrix  
		var bayerMatrix = new int[,] {
			{  0, 12,  3, 15 },
			{  8,  4, 11,  7 },
			{  2, 14,  1, 13 },
			{ 10,  6,  9,  5 }
		};

		var innerRadiusSquared = (radius * falloffStart) * (radius * falloffStart);
		var outerRadiusSquared = radius * radius;

		// Calculate bounds for the circle and clip to the display area  
		int minX = Math.Max(0, (int)(center.X - radius));
		int maxX = Math.Min(Width - 1, (int)(center.X + radius));
		int minY = Math.Max(0, (int)(center.Y - radius));
		int maxY = Math.Min(Height - 1, (int)(center.Y + radius));

		for (var y = minY; y <= maxY; y++)
		{
			int rowOffset = y * Width * BPP;
			for (var x = minX; x <= maxX; x++)
			{
				int dx = x - (int)center.X;
				int dy = y - (int)center.Y;
				var distanceSquared = dx * dx + dy * dy;

				if (distanceSquared > outerRadiusSquared)
					continue;

				if (distanceSquared <= innerRadiusSquared)
				{
					_data[rowOffset + x * BPP] = color.Index;
					continue;
				}

				// Calculate dithering threshold from 0.0 to 1.0  
				var normalizedDistance = (distanceSquared - innerRadiusSquared) / (outerRadiusSquared - innerRadiusSquared);

				// Get the appropriate threshold from the Bayer matrix (0-15, normalized to 0.0-1.0)  
				var bayerX = Math.Abs(dx) % 4;
				var bayerY = Math.Abs(dy) % 4;
				var threshold = bayerMatrix[bayerY, bayerX] / 16.0f;

				// Draw pixel if the normalized distance is less than the threshold  
				if (normalizedDistance < threshold)
				{
					_data[rowOffset + x * BPP] = color.Index;
				}
				else if (secondaryColor.HasValue)
				{
					_data[rowOffset + x * BPP] = secondaryColor.Value.Index;
				}
			}
		}

		_isDirty = true;
	}

	/// <summary>  
	/// Renders a circle outline with the specified center, radius, and color.  
	/// </summary>  
	/// <param name="center">The center of the circle.</param>  
	/// <param name="radius">The radius of the circle.</param>  
	/// <param name="color">The color of the circle.</param>  
	public void RenderCircle(Vector2 center, int radius, RadialColor color)
	{
		RenderCircle(center, radius, color.Index);
	}

	/// <summary>  
	/// Renders a circle outline with the specified center, radius, and palette index.  
	/// </summary>  
	/// <param name="center">The center of the circle.</param>  
	/// <param name="radius">The radius of the circle.</param>  
	/// <param name="paletteIndex">The palette index of the circle.</param>  
	public void RenderCircle(Vector2 center, int radius, byte paletteIndex)
	{
		RenderCircle((int)center.X, (int)center.Y, radius, paletteIndex);
	}

	/// <summary>  
	/// Renders a circle outline with the specified center, radius, and palette index using Midpoint Circle Algorithm.  
	/// </summary>  
	/// <param name="xc">The x-coordinate of the center.</param>  
	/// <param name="yc">The y-coordinate of the center.</param>  
	/// <param name="radius">The radius of the circle.</param>  
	/// <param name="paletteIndex">The palette index of the circle.</param>  
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

	/// <summary>  
	/// Helper method to render the eight symmetrical points of a circle.  
	/// </summary>  
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

	/// <summary>  
	/// Renders a filled circle with the specified center, radius, and color.  
	/// </summary>  
	/// <param name="position">The center of the circle.</param>  
	/// <param name="radius">The radius of the circle.</param>  
	/// <param name="color">The color to fill with.</param>  
	public void RenderFilledCircle(Vector2 position, int radius, RadialColor color)
	{
		RenderFilledCircle((int)position.X, (int)position.Y, radius, color.Index);
	}

	/// <summary>  
	/// Renders a filled circle with the specified center, radius, and palette index.  
	/// </summary>  
	/// <param name="xc">The x-coordinate of the center.</param>  
	/// <param name="yc">The y-coordinate of the center.</param>  
	/// <param name="radius">The radius of the circle.</param>  
	/// <param name="paletteIndex">The palette index to fill with.</param>  
	public void RenderFilledCircle(int xc, int yc, int radius, byte paletteIndex)
	{
		// Clip to bounds for optimization  
		int minX = Math.Max(0, xc - radius);
		int maxX = Math.Min(Width - 1, xc + radius);
		int minY = Math.Max(0, yc - radius);
		int maxY = Math.Min(Height - 1, yc + radius);

		int radiusSquared = radius * radius;

		// Use a more efficient algorithm that avoids redundant calculations  
		for (int y = minY; y <= maxY; y++)
		{
			int dy = y - yc;
			int dy2 = dy * dy;
			int rowOffset = y * Width * BPP;

			for (int x = minX; x <= maxX; x++)
			{
				int dx = x - xc;
				if (dx * dx + dy2 <= radiusSquared)
				{
					_data[rowOffset + x * BPP] = paletteIndex;
				}
			}
		}

		_isDirty = true;
	}

	/// <summary>  
	/// Performs a flood fill starting at the specified point with the specified palette index.  
	/// </summary>  
	/// <param name="pnt">The starting point for the fill.</param>  
	/// <param name="paletteIndex">The palette index to fill with.</param>  
	public void FloodFill(Vector2 pnt, byte paletteIndex)
	{
		FloodFill((int)pnt.X, (int)pnt.Y, paletteIndex);
	}

	/// <summary>  
	/// Performs a flood fill starting at the specified coordinates with the specified palette index.  
	/// </summary>  
	/// <param name="x">The x-coordinate of the starting point.</param>  
	/// <param name="y">The y-coordinate of the starting point.</param>  
	/// <param name="paletteIndex">The palette index to fill with.</param>  
	public void FloodFill(int x, int y, byte paletteIndex)
	{
		// Check if the starting point is valid  
		if (x < 0 || x >= Width || y < 0 || y >= Height)
			return;

		byte targetColor = GetPixel(x, y);

		// If the target color is already the fill color, nothing to do  
		if (targetColor == paletteIndex)
			return;

		// Use a more memory-efficient algorithm with a stack instead of a queue  
		// and use a span-filling approach to reduce the number of stack operations  
		Stack<(int X, int Y)> stack = new Stack<(int X, int Y)>();
		stack.Push((x, y));

		while (stack.Count > 0)
		{
			var (px, py) = stack.Pop();

			// Find the leftmost and rightmost pixels of the current span  
			int leftX = px;
			while (leftX > 0 && GetPixel(leftX - 1, py) == targetColor)
				leftX--;

			int rightX = px;
			while (rightX < Width - 1 && GetPixel(rightX + 1, py) == targetColor)
				rightX++;

			// Fill the span  
			for (int i = leftX; i <= rightX; i++)
				SetPixel(i, py, paletteIndex);

			// Check spans above and below  
			CheckFillSpan(leftX, rightX, py - 1, targetColor, paletteIndex, stack);
			CheckFillSpan(leftX, rightX, py + 1, targetColor, paletteIndex, stack);
		}
	}

	/// <summary>  
	/// Helper method for flood fill to check and queue spans to fill.  
	/// </summary>  
	private void CheckFillSpan(int leftX, int rightX, int y, byte targetColor, byte fillColor, Stack<(int X, int Y)> stack)
	{
		if (y < 0 || y >= Height)
			return;

		bool inSpan = false;

		for (int x = leftX; x <= rightX; x++)
		{
			if (!inSpan && GetPixel(x, y) == targetColor)
			{
				stack.Push((x, y));
				inSpan = true;
			}
			else if (inSpan && GetPixel(x, y) != targetColor)
			{
				inSpan = false;
			}
		}
	}

	/// <summary>  
	/// Updates the virtual display with the current pixel data if it has changed.  
	/// </summary>  
	public void Present()
	{
		if (_isDirty)
		{
			_display.UpdatePixels(_data);
			_isDirty = false;
		}
	}

	/// <summary>  
	/// Releases all resources used by the RenderingContext.  
	/// </summary>  
	public void Dispose()
	{
		if (!_disposed)
		{
			// No need to dispose _display as it's not owned by this class  
			_disposed = true;
		}
	}

	#endregion
}