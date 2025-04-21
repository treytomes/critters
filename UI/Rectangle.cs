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

	public Rectangle(UIElement? parent, Services.IResourceManager resources, Services.IEventBus eventBus, IRenderingContext rc, Box2 bounds, RadialColor borderColor, RadialColor fillColor)
		: base(parent, resources, eventBus, rc)
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

	public override void Render(GameTime gameTime)
	{
		base.Render(gameTime);

		RC.RenderFilledRect(AbsoluteBounds, FillColor);
		RC.RenderRect(AbsoluteBounds, BorderColor);
	}

	#endregion
}