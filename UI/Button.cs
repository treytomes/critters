using System.Runtime.CompilerServices;
using Critters.Events;
using Critters.Gfx;
using Critters.Services;
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

	public Button(UIElement? parent, IResourceManager resources, IEventBus eventBus, IRenderingContext rc, Vector2 position, ButtonStyle style = ButtonStyle.Raised)
		: base(parent, resources, eventBus, rc)
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

	#region Properties

	public object? Metadata { get; set; } = null;

	#endregion

	#region Methods

	public override void Load()
	{
		base.Load();

		if (Content != null)
		{
			Content.Load();
		}

		EventBus.Subscribe<MouseMoveEventArgs>(OnMouseMove);
		EventBus.Subscribe<MouseButtonEventArgs>(OnMouseClick);
	}

	public override void Unload()
	{
		base.Unload();

		EventBus.Unsubscribe<MouseMoveEventArgs>(OnMouseMove);
		EventBus.Unsubscribe<MouseButtonEventArgs>(OnMouseClick);
	}

	protected override void RenderSelf(GameTime gameTime)
	{
		if (_style == ButtonStyle.Flat)
		{
			RenderFlat(gameTime);
		}
		else if (_style == ButtonStyle.Raised)
		{
			RenderRaised(gameTime);
		}

		Content?.Render(gameTime);
	}

	private void RenderFlat(GameTime gameTime)
	{
		var color = RC.Palette[2, 2, 2];
		if (_hasMouseFocus)
		{
			color = RC.Palette[4, 4, 4];
		}
		else if (_hasMouseHover)
		{
			color = RC.Palette[3, 3, 3];
		}
		RC.RenderFilledRect(AbsoluteBounds, color);
	}

	private void RenderRaised(GameTime gameTime)
	{
		if (!_hasMouseFocus)
		{
			// Render the drop-shadow.
			RC.RenderFilledRect(AbsoluteBounds.Min + new Vector2(SHADOW_OFFSET, SHADOW_OFFSET), AbsoluteBounds.Max + new Vector2(SHADOW_OFFSET, SHADOW_OFFSET), 0);
		}

		var color = RC.Palette[2, 2, 2];
		if (_hasMouseFocus)
		{
			color = RC.Palette[4, 4, 4];
		}
		else if (_hasMouseHover)
		{
			color = RC.Palette[3, 3, 3];
		}
		RC.RenderFilledRect(AbsoluteBounds, color);
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