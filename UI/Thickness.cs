namespace Critters.UI;

class Thickness
{
	public Thickness(float uniformSize)
	{
		Left = Right = Top = Bottom = uniformSize;
	}

	public Thickness(float horizontal, float vertical)
	{
		Left = Right = horizontal;
		Top = Bottom = vertical;
	}

	public Thickness(float left, float top, float right, float bottom)
	{
		Left = left;
		Top = top;
		Right = right;
		Bottom = bottom;
	}

	public float Left { get; set; } = 0;
	public float Right { get; set; } = 0;
	public float Top { get; set; } = 0;
	public float Bottom { get; set; } = 0;
}