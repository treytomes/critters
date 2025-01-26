using System.ComponentModel;
using Critters.Gfx;
using OpenTK.Mathematics;

namespace Critters.UI;

class Rectangle : UIElement
{
	#region Fields

	private RadialColor _borderColor;
	private RadialColor _fillColor;

	#endregion

	#region Constructors

	public Rectangle(Box2 bounds, RadialColor borderColor, RadialColor fillColor)
		: this(null, bounds, borderColor, fillColor)
	{
	}

	public Rectangle(UIElement? parent, Box2 bounds, RadialColor borderColor, RadialColor fillColor)
		: base(parent)
	{
		Position = bounds.Min;
		Size = bounds.Size;
		_borderColor = borderColor;
		_fillColor = fillColor;
	}

	#endregion

	#region Properties

	public RadialColor BorderColor
	{
		get => _borderColor;
		set
		{
			_borderColor = value;
			OnPropertyChanged();
		}
	}

	public RadialColor FillColor
	{
		get => _fillColor;
		set
		{
			_fillColor = value;
			OnPropertyChanged();
		}
	}

	#endregion

	#region Methods

	public override void Render(RenderingContext rc, GameTime gameTime)
	{
		base.Render(rc, gameTime);

		rc.RenderFilledRect(AbsoluteBounds, FillColor);
		rc.RenderRect(AbsoluteBounds, BorderColor);
	}

	#endregion
}