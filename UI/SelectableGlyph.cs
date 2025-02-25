using Critters.Events;
using Critters.Gfx;
using Critters.IO;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;

namespace Critters.UI;

class SelectableGlyph : UIElement
{
	#region Events

	public event EventHandler<ButtonClickedEventArgs>? Clicked;
	public event EventHandler<MouseWheelEventArgs>? Scrolled;

	#endregion

	#region Fields

	private bool _hasMouseHover = false;
	private bool _isFocused = false;
	private bool _isSelected = false;
	private string _glyphResourcePath;
	private GlyphSet<Bitmap>? _glyphs;
	public byte GlyphIndex;

	#endregion

	#region Constructors

	public SelectableGlyph(UIElement? parent, Vector2 position, string glyphResourcePath, byte glyphIndex)
		: base(parent)
	{
		Position = position;
		_glyphResourcePath = glyphResourcePath;
		GlyphIndex = glyphIndex;
	}

	#endregion

	#region Properties

	public bool HasMouseHover
	{
		get
		{
			return _hasMouseHover;
		}
		private set
		{
			if (_hasMouseHover != value)
			{
				_hasMouseHover = value;
				OnPropertyChanged();
			}
		}
	}

	public bool IsFocused
	{
		get
		{
			return _isFocused;
		}
		private set
		{
			if (_isFocused != value)
			{
				_isFocused = value;
				OnPropertyChanged();
			}
		}
	}

	public bool IsSelected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			if (_isSelected != value)
			{
				_isSelected = value;
				OnPropertyChanged();
			}
		}
	}

	#endregion

	#region Methods

	public override void Load(ResourceManager resources, EventBus eventBus)
	{
		base.Load(resources, eventBus);

		eventBus.Subscribe<MouseMoveEventArgs>(OnMouseMove);
		eventBus.Subscribe<MouseButtonEventArgs>(OnMouseButton);
		eventBus.Subscribe<MouseWheelEventArgs>(OnMouseWheel);

    var image = resources.Load<Image>(_glyphResourcePath);
    _glyphs = new GlyphSet<Bitmap>(new Bitmap(image), 8, 8);
		Size = new Vector2(_glyphs.TileWidth, _glyphs.TileHeight) + Vector2.One;
	}

	public override void Unload(ResourceManager resources, EventBus eventBus)
	{
		base.Unload(resources, eventBus);

		eventBus.Unsubscribe<MouseMoveEventArgs>(OnMouseMove);
		eventBus.Unsubscribe<MouseButtonEventArgs>(OnMouseButton);
		eventBus.Unsubscribe<MouseWheelEventArgs>(OnMouseWheel);
	}

	public override void Render(RenderingContext rc, GameTime gameTime)
	{
		base.Render(rc, gameTime);

		var x = (int)AbsolutePosition.X;
		var y = (int)AbsolutePosition.Y;
		if (IsSelected)
		{
			var borderColor = Palette.GetIndex(5, 0, 0);
			rc.RenderRect(x, y, (int)(x + Size.X), (int)(y + Size.Y), borderColor);
		}
		else if (HasMouseHover)
		{
			var borderColor = Palette.GetIndex(5, 3, 0);
			rc.RenderRect(x, y, (int)(x + Size.X), (int)(y + Size.Y), borderColor);
		}
		// rc.RenderFilledRect(x + 1, y + 1, (int)(x + Size.X - 2), (int)(y + Size.Y) - 2, 0);
		var fgColor = new RadialColor(5, 5, 0);
		var bgColor = new RadialColor(0, 0, 5);
		_glyphs?[GlyphIndex].Render(rc, new Vector2(x, y) + Vector2.One, fgColor, bgColor);
	}

	private void OnMouseMove(MouseMoveEventArgs e)
	{
		HasMouseHover = AbsoluteBounds.ContainsInclusive(e.Position);
	}

	private void OnMouseButton(MouseButtonEventArgs e)
	{
		if (e.Button == OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Left)
		{
			if (e.IsPressed)
			{
				if (HasMouseHover)
				{
					IsFocused = true;
				}
			}
			else
			{
				if (IsFocused && HasMouseHover)
				{
					Clicked?.Invoke(this, new ButtonClickedEventArgs());
				}
				IsFocused = false;
			}
		}
	}

	private void OnMouseWheel(MouseWheelEventArgs e)
	{
		if (HasMouseHover)
		{
			Scrolled?.Invoke(this, e);
		}
	}

	// protected override void OnPropertyChanged([CallerMemberName] string propertyName = "")
	// {
	// 	base.OnPropertyChanged(propertyName);
	// }

	#endregion
}
