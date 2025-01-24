using System.Runtime.CompilerServices;
using Critters.Events;
using Critters.Gfx;
using Critters.IO;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Critters.UI;

// TODO: Button styles for 3D or Flat.

enum ButtonStyle
{
	Flat,
	Raised,
}

class Button : ContentPresenter
{
	#region Constants

	private const int SHADOW_OFFSET = 2;

	#endregion

	#region Events

	public event EventHandler<ButtonClickedEventArgs>? Clicked;

	#endregion

	#region Fields

	private readonly ButtonStyle _style;
	private bool _hasMouseHover = false;
	private bool _hasMouseFocus = false;

	#endregion

	#region Constructors

	public Button(Vector2 position, ButtonStyle style = ButtonStyle.Raised)
		: this(null, position, style)
	{
	}
	
	public Button(UIElement? parent, Vector2 position, ButtonStyle style = ButtonStyle.Raised)
		: base(parent)
	{
		Position = position;
		if (_style == ButtonStyle.Flat)
		{
			Padding = new(3, 1, 0, 0);
		}
		else
		{
			Padding = new(8, 10, 8, 10);
		}
		_style = style;
	}

	#endregion

	#region Methods

	public override void Load(ResourceManager resources, EventBus eventBus)
	{
		base.Load(resources, eventBus);

		if (Content != null)
		{
			Content.Load(resources, eventBus);
		}

		eventBus.Subscribe<MouseMoveEventArgs>(OnMouseMove);
		eventBus.Subscribe<MouseButtonEventArgs>(OnMouseClick);
	}

	public override void Unload(ResourceManager resources, EventBus eventBus)
	{
		base.Unload(resources, eventBus);

		eventBus.Unsubscribe<MouseMoveEventArgs>(OnMouseMove);
	}

	protected override void RenderSelf(RenderingContext rc, GameTime gameTime)
	{
		if (_style == ButtonStyle.Flat)
		{
			RenderFlat(rc, gameTime);
		}
		else if (_style == ButtonStyle.Raised)
		{
			RenderRaised(rc, gameTime);
		}
		
		Content?.Render(rc, gameTime);
	}

	private void RenderFlat(RenderingContext rc, GameTime gameTime)
	{
		var color = rc.Palette[2, 2, 2];
		if (_hasMouseFocus)
		{
			color = rc.Palette[4, 4, 4];
		}
		else if (_hasMouseHover)
		{
			color = rc.Palette[3, 3, 3];
		}
		rc.RenderFilledRect(AbsoluteBounds, color);
	}

	private void RenderRaised(RenderingContext rc, GameTime gameTime)
	{
		if (!_hasMouseFocus)
		{
			// Render the drop-shadow.
			rc.RenderFilledRect(AbsoluteBounds.Min + new Vector2(SHADOW_OFFSET, SHADOW_OFFSET), AbsoluteBounds.Max + new Vector2(SHADOW_OFFSET, SHADOW_OFFSET), 0);
		}
		
		var color = rc.Palette[2, 2, 2];
		if (_hasMouseFocus)
		{
			color = rc.Palette[4, 4, 4];
		}
		else if (_hasMouseHover)
		{
			color = rc.Palette[3, 3, 3];
		}
		rc.RenderFilledRect(AbsoluteBounds, color);
	}

	protected override void OnPropertyChanged([CallerMemberName] string propertyName = "")
	{
		base.OnPropertyChanged(propertyName);

		if (propertyName == nameof(Content))
		{
			var contentSize = Content?.Size ?? Vector2.Zero;
			Size = contentSize + new Vector2(Padding.Left + Padding.Right, Padding.Top + Padding.Bottom);
		}
	}

	private void OnMouseMove(MouseMoveEventArgs e)
	{
		_hasMouseHover = AbsoluteBounds.ContainsInclusive(e.Position);
	}

	private void OnMouseClick(MouseButtonEventArgs e)
	{
		if (e.Button == MouseButton.Left)
		{
			if (e.Action == InputAction.Press)
			{
				if (_hasMouseHover)
				{
					if (_style == ButtonStyle.Raised)
					{
						Margin = new(SHADOW_OFFSET, SHADOW_OFFSET, 0, 0);
					}
					_hasMouseFocus = true;
				}
			}
			else if (e.Action == InputAction.Release)
			{
				if (_hasMouseFocus && _hasMouseHover)
				{
					Clicked?.Invoke(this, new ButtonClickedEventArgs());
				}
				if (_style == ButtonStyle.Raised)
				{
					Margin = new(0);
				}
				_hasMouseFocus = false;
			}
		}
	}

	#endregion
}