using System;
using Critters.Gfx;
using Critters.Services;
using OpenTK.Mathematics;

namespace Critters.UI;

/// <summary>
/// A rectangle UI element that can be filled and/or bordered with color.
/// </summary>
class Rectangle : UIElement
{
	#region Fields

	private RadialColor? _borderColor;
	private RadialColor? _fillColor;
	private float _borderThickness = 1.0f;

	#endregion

	#region Constructors

	/// <summary>
	/// Creates a new rectangle UI element.
	/// </summary>
	/// <param name="resources">The resource manager.</param>
	/// <param name="rc">The rendering context.</param>
	/// <param name="fillColor">The fill color for the rectangle (null for transparent).</param>
	/// <param name="borderColor">The border color for the rectangle (null for no border).</param>
	public Rectangle(IResourceManager resources, IRenderingContext rc,
		RadialColor? fillColor, RadialColor? borderColor)
		: base(resources, rc)
	{
		_fillColor = fillColor;
		_borderColor = borderColor;
	}

	/// <summary>
	/// Creates a new rectangle UI element with specified bounds.
	/// </summary>
	/// <param name="resources">The resource manager.</param>
	/// <param name="rc">The rendering context.</param>
	/// <param name="bounds">The bounds of the rectangle.</param>
	/// <param name="fillColor">The fill color for the rectangle (null for transparent).</param>
	/// <param name="borderColor">The border color for the rectangle (null for no border).</param>
	public Rectangle(IResourceManager resources, IRenderingContext rc,
		Box2 bounds, RadialColor? fillColor, RadialColor? borderColor)
		: base(resources, rc)
	{
		Position = bounds.Min;
		ContentSize = bounds.Size;
		_fillColor = fillColor;
		_borderColor = borderColor;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets or sets the border color of the rectangle. Null means no border.
	/// </summary>
	public RadialColor? BorderColor
	{
		get => _borderColor;
		set
		{
			ThrowIfDisposed();
			if (!Equals(_borderColor, value))
			{
				_borderColor = value;
				OnPropertyChanged();
			}
		}
	}

	/// <summary>
	/// Gets or sets the fill color of the rectangle. Null means transparent.
	/// </summary>
	public RadialColor? FillColor
	{
		get => _fillColor;
		set
		{
			ThrowIfDisposed();
			if (!Equals(_fillColor, value))
			{
				_fillColor = value;
				OnPropertyChanged();
			}
		}
	}

	/// <summary>
	/// Gets or sets the border thickness of the rectangle.
	/// </summary>
	public float BorderThickness
	{
		get => _borderThickness;
		set
		{
			ThrowIfDisposed();
			if (_borderThickness != value)
			{
				_borderThickness = Math.Max(0, value);
				OnPropertyChanged();
			}
		}
	}

	#endregion

	#region Methods

	/// <summary>
	/// Renders the rectangle.
	/// </summary>
	/// <param name="gameTime">The current game time.</param>
	public override void Render(GameTime gameTime)
	{
		ThrowIfDisposed();

		if (!IsVisible)
		{
			return;
		}

		// Render the rectangle fill and border
		if (_fillColor.HasValue)
		{
			RC.RenderFilledRect(AbsoluteBounds, _fillColor.Value);
		}

		if (_borderColor.HasValue && BorderThickness > 0)
		{
			RC.RenderRect(AbsoluteBounds, _borderColor.Value, BorderThickness);
		}

		// Render children after the rectangle itself
		base.Render(gameTime);
	}

	#endregion
}