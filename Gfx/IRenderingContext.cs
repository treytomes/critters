using OpenTK.Mathematics;

namespace Critters.Gfx;

interface IRenderingContext : IDisposable
{
	/// <summary>  
	/// Gets the width of the rendering context in pixels.  
	/// </summary>  
	public int Width { get; }

	/// <summary>  
	/// Gets the height of the rendering context in pixels.  
	/// </summary>  
	public int Height { get; }

	/// <summary>  
	/// Gets the size of the viewport as a Vector2.  
	/// </summary>  
	public Vector2 ViewportSize { get; }

	/// <summary>  
	/// Gets a box representing the bounds of the rendering context.  
	/// </summary>  
	public Box2 Bounds { get; }

	/// <summary>  
	/// Gets the color palette used by the rendering context.  
	/// </summary>  
	public Palette Palette { get; }

	/// <summary>  
	/// Convert actual screen coordinates to virtual coordinates.  
	/// </summary>  
	/// <param name="actualPoint">The point in actual screen coordinates.</param>  
	/// <returns>The corresponding point in virtual display coordinates.</returns>  
	public Vector2 ActualToVirtualPoint(Vector2 actualPoint);

	/// <summary>  
	/// Convert virtual coordinates to actual screen coordinates.  
	/// </summary>  
	/// <param name="virtualPoint">The point in virtual display coordinates.</param>  
	/// <returns>The corresponding point in actual screen coordinates.</returns>  
	public Vector2 VirtualToActualPoint(Vector2 virtualPoint);

	/// <summary>  
	/// Fills the entire rendering context with the specified color.  
	/// </summary>  
	/// <param name="paletteIndex">The palette index to fill with.</param>  
	public void Fill(byte paletteIndex);

	/// <summary>  
	/// Fills the entire rendering context with the specified color.  
	/// </summary>  
	/// <param name="color">The color to fill with.</param>  
	public void Fill(RadialColor color);

	/// <summary>  
	/// Clears the rendering context (fills with color index 0).  
	/// </summary>  
	public void Clear();

	/// <summary>  
	/// Sets a pixel at the specified position to the specified color.  
	/// </summary>  
	/// <param name="pnt">The position of the pixel.</param>  
	/// <param name="color">The color to set.</param>  
	public void SetPixel(Vector2 pnt, RadialColor color);

	/// <summary>  
	/// Sets a pixel at the specified position to the specified palette index.  
	/// </summary>  
	/// <param name="pnt">The position of the pixel.</param>  
	/// <param name="paletteIndex">The palette index to set.</param>  
	public void SetPixel(Vector2 pnt, byte paletteIndex);

	/// <summary>  
	/// Sets a pixel at the specified coordinates to the specified palette index.  
	/// </summary>  
	/// <param name="x">The x-coordinate of the pixel.</param>  
	/// <param name="y">The y-coordinate of the pixel.</param>  
	/// <param name="paletteIndex">The palette index to set.</param>  
	public void SetPixel(int x, int y, byte paletteIndex);

	/// <summary>  
	/// Gets the palette index of the pixel at the specified position.  
	/// </summary>  
	/// <param name="pnt">The position of the pixel.</param>  
	/// <returns>The palette index of the pixel, or 0 if out of bounds.</returns>  
	public byte GetPixel(Vector2 pnt);

	/// <summary>  
	/// Gets the palette index of the pixel at the specified coordinates.  
	/// </summary>  
	/// <param name="x">The x-coordinate of the pixel.</param>  
	/// <param name="y">The y-coordinate of the pixel.</param>  
	/// <returns>The palette index of the pixel, or 0 if out of bounds.</returns>  
	public byte GetPixel(int x, int y);

	/// <summary>  
	/// Renders a filled rectangle with the specified bounds and color.  
	/// </summary>  
	/// <param name="bounds">The bounds of the rectangle.</param>  
	/// <param name="color">The color to fill with.</param>  
	public void RenderFilledRect(Box2 bounds, RadialColor color);

	/// <summary>  
	/// Renders a filled rectangle with the specified bounds and palette index.  
	/// </summary>  
	/// <param name="box">The bounds of the rectangle.</param>  
	/// <param name="paletteIndex">The palette index to fill with.</param>  
	public void RenderFilledRect(Box2 box, byte paletteIndex);

	/// <summary>  
	/// Renders a filled rectangle with the specified corners and palette index.  
	/// </summary>  
	/// <param name="pnt1">The first corner of the rectangle.</param>  
	/// <param name="pnt2">The opposite corner of the rectangle.</param>  
	/// <param name="paletteIndex">The palette index to fill with.</param>  
	public void RenderFilledRect(Vector2 pnt1, Vector2 pnt2, byte paletteIndex);

	/// <summary>  
	/// Renders a filled rectangle with the specified corners and palette index.  
	/// </summary>  
	/// <param name="x1">The x-coordinate of the first corner.</param>  
	/// <param name="y1">The y-coordinate of the first corner.</param>  
	/// <param name="x2">The x-coordinate of the opposite corner.</param>  
	/// <param name="y2">The y-coordinate of the opposite corner.</param>  
	/// <param name="paletteIndex">The palette index to fill with.</param>  
	public void RenderFilledRect(int x1, int y1, int x2, int y2, byte paletteIndex);

	/// <summary>  
	/// Renders a rectangle outline with the specified bounds, color, and thickness.  
	/// </summary>  
	/// <param name="bounds">The bounds of the rectangle.</param>  
	/// <param name="color">The color of the outline.</param>
	/// <param name="thickness">The thickness of the outline (default is 1).</param>
	void RenderRect(Box2 bounds, RadialColor color, float thickness = 1);

	/// <summary>  
	/// Renders a rectangle outline with the specified bounds, palette index, and thickness.  
	/// </summary>  
	/// <param name="bounds">The bounds of the rectangle.</param>  
	/// <param name="paletteIndex">The palette index of the outline.</param>
	/// <param name="thickness">The thickness of the outline (default is 1).</param>
	void RenderRect(Box2 bounds, byte paletteIndex, float thickness = 1);

	/// <summary>  
	/// Renders a rectangle outline with the specified corners, palette index, and thickness.  
	/// </summary>  
	/// <param name="pnt1">The first corner of the rectangle.</param>  
	/// <param name="pnt2">The opposite corner of the rectangle.</param>  
	/// <param name="paletteIndex">The palette index of the outline.</param>
	/// <param name="thickness">The thickness of the outline (default is 1).</param>
	void RenderRect(Vector2 pnt1, Vector2 pnt2, byte paletteIndex, float thickness = 1);

	/// <summary>  
	/// Renders a rectangle outline with the specified corners, palette index, and thickness.  
	/// </summary>  
	/// <param name="x1">The x-coordinate of the first corner.</param>  
	/// <param name="y1">The y-coordinate of the first corner.</param>  
	/// <param name="x2">The x-coordinate of the opposite corner.</param>  
	/// <param name="y2">The y-coordinate of the opposite corner.</param>  
	/// <param name="paletteIndex">The palette index of the outline.</param>
	/// <param name="thickness">The thickness of the outline (default is 1).</param>
	void RenderRect(int x1, int y1, int x2, int y2, byte paletteIndex, float thickness = 1);

	/// <summary>  
	/// Renders a horizontal line with the specified starting point, length, and palette index.  
	/// </summary>  
	/// <param name="pnt">The starting point of the line.</param>  
	/// <param name="len">The length of the line in pixels.</param>  
	/// <param name="paletteIndex">The palette index of the line.</param>  
	public void RenderHLine(Vector2 pnt, int len, byte paletteIndex);

	/// <summary>  
	/// Renders a horizontal line with the specified endpoints and palette index.  
	/// </summary>  
	/// <param name="x1">The x-coordinate of the start point.</param>  
	/// <param name="x2">The x-coordinate of the end point.</param>  
	/// <param name="y">The y-coordinate of the line.</param>  
	/// <param name="paletteIndex">The palette index of the line.</param>  
	public void RenderHLine(int x1, int x2, int y, byte paletteIndex);

	/// <summary>  
	/// Renders a vertical line with the specified starting point, length, and palette index.  
	/// </summary>  
	/// <param name="pnt">The starting point of the line.</param>  
	/// <param name="len">The length of the line in pixels.</param>  
	/// <param name="paletteIndex">The palette index of the line.</param>  
	public void RenderVLine(Vector2 pnt, int len, byte paletteIndex);

	/// <summary>  
	/// Renders a vertical line with the specified endpoints and palette index.  
	/// </summary>  
	/// <param name="x">The x-coordinate of the line.</param>  
	/// <param name="y1">The y-coordinate of the start point.</param>  
	/// <param name="y2">The y-coordinate of the end point.</param>  
	/// <param name="paletteIndex">The palette index of the line.</param>  
	public void RenderVLine(int x, int y1, int y2, byte paletteIndex);

	/// <summary>  
	/// Renders a line between two points with the specified palette index.  
	/// </summary>  
	/// <param name="pnt1">The start point of the line.</param>  
	/// <param name="pnt2">The end point of the line.</param>  
	/// <param name="paletteIndex">The palette index of the line.</param>  
	public void RenderLine(Vector2 pnt1, Vector2 pnt2, byte paletteIndex);

	/// <summary>  
	/// Renders a line between two points with the specified color.  
	/// </summary>  
	/// <param name="pnt1">The start point of the line.</param>  
	/// <param name="pnt2">The end point of the line.</param>  
	/// <param name="color">The color of the line.</param>  
	public void RenderLine(Vector2 pnt1, Vector2 pnt2, RadialColor color);

	/// <summary>  
	/// Renders a line between two points with the specified palette index using Bresenham's algorithm.  
	/// </summary>  
	/// <param name="x1">The x-coordinate of the start point.</param>  
	/// <param name="y1">The y-coordinate of the start point.</param>  
	/// <param name="x2">The x-coordinate of the end point.</param>  
	/// <param name="y2">The y-coordinate of the end point.</param>  
	/// <param name="paletteIndex">The palette index of the line.</param>  
	public void RenderLine(int x1, int y1, int x2, int y2, byte paletteIndex);

	/// <summary>  
	/// Renders a circle with ordered dithering for a soft edge effect.  
	/// </summary>  
	/// <param name="center">The center of the circle.</param>  
	/// <param name="radius">The radius of the circle.</param>  
	/// <param name="color">The primary color of the circle.</param>  
	/// <param name="falloffStart">The point at which the falloff begins (0.0-1.0).</param>  
	/// <param name="secondaryColor">Optional secondary color for the dithered region.</param>  
	public void RenderOrderedDitheredCircle(Vector2 center, int radius, RadialColor color, float falloffStart = 0.6f, RadialColor? secondaryColor = null);

	/// <summary>  
	/// Renders a circle outline with the specified center, radius, and color.  
	/// </summary>  
	/// <param name="center">The center of the circle.</param>  
	/// <param name="radius">The radius of the circle.</param>  
	/// <param name="color">The color of the circle.</param>  
	public void RenderCircle(Vector2 center, int radius, RadialColor color);

	/// <summary>  
	/// Renders a circle outline with the specified center, radius, and palette index.  
	/// </summary>  
	/// <param name="center">The center of the circle.</param>  
	/// <param name="radius">The radius of the circle.</param>  
	/// <param name="paletteIndex">The palette index of the circle.</param>  
	public void RenderCircle(Vector2 center, int radius, byte paletteIndex);

	/// <summary>  
	/// Renders a circle outline with the specified center, radius, and palette index using Midpoint Circle Algorithm.  
	/// </summary>  
	/// <param name="xc">The x-coordinate of the center.</param>  
	/// <param name="yc">The y-coordinate of the center.</param>  
	/// <param name="radius">The radius of the circle.</param>  
	/// <param name="paletteIndex">The palette index of the circle.</param>  
	public void RenderCircle(int xc, int yc, int radius, byte paletteIndex);

	/// <summary>  
	/// Renders a filled circle with the specified center, radius, and color.  
	/// </summary>  
	/// <param name="position">The center of the circle.</param>  
	/// <param name="radius">The radius of the circle.</param>  
	/// <param name="color">The color to fill with.</param>  
	public void RenderFilledCircle(Vector2 position, int radius, RadialColor color);

	/// <summary>  
	/// Renders a filled circle with the specified center, radius, and palette index.  
	/// </summary>  
	/// <param name="xc">The x-coordinate of the center.</param>  
	/// <param name="yc">The y-coordinate of the center.</param>  
	/// <param name="radius">The radius of the circle.</param>  
	/// <param name="paletteIndex">The palette index to fill with.</param>  
	public void RenderFilledCircle(int xc, int yc, int radius, byte paletteIndex);

	/// <summary>  
	/// Performs a flood fill starting at the specified point with the specified palette index.  
	/// </summary>  
	/// <param name="pnt">The starting point for the fill.</param>  
	/// <param name="paletteIndex">The palette index to fill with.</param>  
	public void FloodFill(Vector2 pnt, byte paletteIndex);

	/// <summary>  
	/// Performs a flood fill starting at the specified coordinates with the specified palette index.  
	/// </summary>  
	/// <param name="x">The x-coordinate of the starting point.</param>  
	/// <param name="y">The y-coordinate of the starting point.</param>  
	/// <param name="paletteIndex">The palette index to fill with.</param>  
	public void FloodFill(int x, int y, byte paletteIndex);

	/// <summary>
	/// Renders a filled triangle with the specified vertices and color.
	/// </summary>
	/// <param name="p1">The first vertex of the triangle.</param>
	/// <param name="p2">The second vertex of the triangle.</param>
	/// <param name="p3">The third vertex of the triangle.</param>
	/// <param name="color">The color to fill with.</param>
	public void RenderFilledTriangle(Vector2 p1, Vector2 p2, Vector2 p3, RadialColor color);

	/// <summary>
	/// Renders a filled triangle with the specified vertices and color.
	/// </summary>
	/// <param name="x1">The x-coordinate of the first vertex.</param>
	/// <param name="y1">The y-coordinate of the first vertex.</param>
	/// <param name="x2">The x-coordinate of the second vertex.</param>
	/// <param name="y2">The y-coordinate of the second vertex.</param>
	/// <param name="x3">The x-coordinate of the third vertex.</param>
	/// <param name="y3">The y-coordinate of the third vertex.</param>
	/// <param name="paletteIndex">The palette index to fill with.</param>
	public void RenderFilledTriangle(int x1, int y1, int x2, int y2, int x3, int y3, byte paletteIndex);

	/// <summary>
	/// Renders a triangle outline with the specified vertices and color.
	/// </summary>
	/// <param name="p1">The first vertex of the triangle.</param>
	/// <param name="p2">The second vertex of the triangle.</param>
	/// <param name="p3">The third vertex of the triangle.</param>
	/// <param name="color">The color of the triangle.</param>
	public void RenderTriangle(Vector2 p1, Vector2 p2, Vector2 p3, RadialColor color);

	/// <summary>
	/// Renders a triangle outline with the specified vertices and color.
	/// </summary>
	/// <param name="x1">The x-coordinate of the first vertex.</param>
	/// <param name="y1">The y-coordinate of the first vertex.</param>
	/// <param name="x2">The x-coordinate of the second vertex.</param>
	/// <param name="y2">The y-coordinate of the second vertex.</param>
	/// <param name="x3">The x-coordinate of the third vertex.</param>
	/// <param name="y3">The y-coordinate of the third vertex.</param>
	/// <param name="paletteIndex">The palette index of the triangle.</param>
	public void RenderTriangle(int x1, int y1, int x2, int y2, int x3, int y3, byte paletteIndex);

	/// <summary>  
	/// Updates the virtual display with the current pixel data if it has changed.  
	/// </summary>  
	public void Present();
}
