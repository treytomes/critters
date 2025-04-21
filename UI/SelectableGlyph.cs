using System.Reactive.Linq;
using System.Reactive.Subjects;
using Critters.Core;
using Critters.Events;
using Critters.Gfx;
using Critters.Services;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;

namespace Critters.UI;

class SelectableGlyph : UIElement
{
	#region Subjects

	private readonly Subject<ButtonClickedEventArgs> _clickedSubject = new();
	private readonly Subject<MouseWheelEventArgs> _scrolledSubject = new();

	#endregion

	#region Events (Legacy)

	public event EventHandler<ButtonClickedEventArgs>? Clicked;
	public event EventHandler<MouseWheelEventArgs>? Scrolled;

	#endregion

	#region Observables

	public IObservable<ButtonClickedEventArgs> ClickEvents => _clickedSubject.AsObservable();
	public IObservable<MouseWheelEventArgs> ScrollEvents => _scrolledSubject.AsObservable();

	#endregion

	#region Fields

	private bool _hasMouseHover = false;
	private bool _isFocused = false;
	private bool _isSelected = false;
	private readonly string _glyphResourcePath;
	private GlyphSet<Bitmap>? _glyphs;
	private byte _glyphIndex;
	private RadialColor _foregroundColor = new(5, 5, 0);
	private RadialColor _backgroundColor = new(0, 0, 5);

	#endregion

	#region Constructors

	public SelectableGlyph(IResourceManager resources, IRenderingContext rc, Vector2 position, string glyphResourcePath, byte glyphIndex)
		: base(resources, rc)
	{
		_glyphResourcePath = glyphResourcePath ?? throw new ArgumentNullException(nameof(glyphResourcePath));
		_glyphIndex = glyphIndex;
		Position = position;

		// Setup disposal
		DisposalEvents.Subscribe(e =>
		{
			if (e.Stage == DisposalStage.Started)
			{
				_clickedSubject.OnCompleted();
				_scrolledSubject.OnCompleted();
			}
		});
	}

	#endregion

	#region Properties

	public bool HasMouseHover
	{
		get => _hasMouseHover;
		private set
		{
			ThrowIfDisposed();
			if (_hasMouseHover != value)
			{
				_hasMouseHover = value;
				OnPropertyChanged();
			}
		}
	}

	public bool IsFocused
	{
		get => _isFocused;
		private set
		{
			ThrowIfDisposed();
			if (_isFocused != value)
			{
				_isFocused = value;
				OnPropertyChanged();
			}
		}
	}

	public bool IsSelected
	{
		get => _isSelected;
		set
		{
			ThrowIfDisposed();
			if (_isSelected != value)
			{
				_isSelected = value;
				OnPropertyChanged();
			}
		}
	}

	public RadialColor ForegroundColor
	{
		get => _foregroundColor;
		set
		{
			ThrowIfDisposed();
			if (_foregroundColor != value)
			{
				_foregroundColor = value;
				OnPropertyChanged();
			}
		}
	}

	public RadialColor BackgroundColor
	{
		get => _backgroundColor;
		set
		{
			ThrowIfDisposed();
			if (_backgroundColor != value)
			{
				_backgroundColor = value;
				OnPropertyChanged();
			}
		}
	}

	public byte GlyphIndex
	{
		get => _glyphIndex;
		set
		{
			ThrowIfDisposed();
			if (_glyphIndex != value)
			{
				_glyphIndex = value;
				OnPropertyChanged();
			}
		}
	}

	#endregion

	#region Methods

	public override void Load()
	{
		ThrowIfDisposed();

		if (IsLoaded)
			return;

		base.Load();

		var image = Resources.Load<Image>(_glyphResourcePath);
		_glyphs = new GlyphSet<Bitmap>(new Bitmap(image), 8, 8);

		// Set content size based on glyph dimensions
		// Add 2 pixels for the border (1 pixel on each side)
		ContentSize = new Vector2(_glyphs.TileWidth, _glyphs.TileHeight);
		Padding = new Thickness(1);
	}

	public override void Unload()
	{
		ThrowIfDisposed();

		if (!IsLoaded)
			return;

		_glyphs = null;

		base.Unload();
	}

	public override void Render(GameTime gameTime)
	{
		ThrowIfDisposed();

		if (!IsVisible)
			return;

		base.Render(gameTime);

		var x = (int)AbsolutePosition.X;
		var y = (int)AbsolutePosition.Y;

		// Draw selection/hover border
		if (IsSelected)
		{
			var borderColor = Palette.GetIndex(5, 0, 0);
			RC.RenderRect(x, y, (int)(x + Size.X), (int)(y + Size.Y), borderColor);
		}
		else if (HasMouseHover)
		{
			var borderColor = Palette.GetIndex(5, 3, 0);
			RC.RenderRect(x, y, (int)(x + Size.X), (int)(y + Size.Y), borderColor);
		}

		// Draw the glyph with padding offset
		if (_glyphs != null)
		{
			var contentX = x + Padding.Left;
			var contentY = y + Padding.Top;
			_glyphs[GlyphIndex].Render(RC, new Vector2(contentX, contentY), ForegroundColor, BackgroundColor);
		}
	}

	public override bool MouseMove(MouseMoveEventArgs e)
	{
		ThrowIfDisposed();

		var wasHovering = HasMouseHover;
		HasMouseHover = IsVisible && AbsoluteBounds.ContainsInclusive(e.Position);

		// Return true if state changed or is hovering
		return HasMouseHover || (wasHovering != HasMouseHover);
	}

	public override bool MouseDown(MouseButtonEventArgs e)
	{
		ThrowIfDisposed();

		if (!IsVisible)
			return false;

		if (e.Button == OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Left && HasMouseHover)
		{
			IsFocused = true;
			return true;
		}

		return false;
	}

	public override bool MouseUp(MouseButtonEventArgs e)
	{
		ThrowIfDisposed();

		if (!IsVisible)
			return false;

		if (e.Button == OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Left)
		{
			if (IsFocused && HasMouseHover)
			{
				var args = new ButtonClickedEventArgs();
				_clickedSubject.OnNext(args);
				Clicked?.Invoke(this, args);
				IsFocused = false;
				return true;
			}

			IsFocused = false;
		}

		return false;
	}

	public override bool MouseWheel(MouseWheelEventArgs e)
	{
		ThrowIfDisposed();

		if (!IsVisible)
			return false;

		if (HasMouseHover)
		{
			_scrolledSubject.OnNext(e);
			Scrolled?.Invoke(this, e);
			return true;
		}

		return false;
	}

	public override bool KeyDown(KeyboardKeyEventArgs e)
	{
		ThrowIfDisposed();

		if (!IsVisible)
			return false;

		if (IsFocused && (e.Key == OpenTK.Windowing.GraphicsLibraryFramework.Keys.Enter || e.Key == OpenTK.Windowing.GraphicsLibraryFramework.Keys.Space))
		{
			var args = new ButtonClickedEventArgs();
			_clickedSubject.OnNext(args);
			Clicked?.Invoke(this, args);
			return true;
		}

		return base.KeyDown(e);
	}

	#endregion

	#region Disposable Implementation

	protected override void DisposeManagedResources()
	{
		// Clear event handlers
		Clicked = null;
		Scrolled = null;

		// Dispose subjects
		_clickedSubject.Dispose();
		_scrolledSubject.Dispose();

		// Clean up resources
		_glyphs = null;

		base.DisposeManagedResources();
	}

	#endregion
}