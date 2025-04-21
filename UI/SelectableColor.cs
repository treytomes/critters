using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using Critters.Core;
using Critters.Events;
using Critters.Gfx;
using Critters.Services;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Critters.UI;

class SelectableColor : UIElement
{
	#region Constants

	private const int SIZE = 9;

	#endregion

	#region Subjects

	private readonly Subject<ButtonClickedEventArgs> _clickedSubject = new();
	private readonly Subject<MouseWheelEventArgs> _scrolledSubject = new();

	#endregion

	#region Events (Legacy)

	// Kept for backward compatibility
	public event EventHandler<ButtonClickedEventArgs>? Clicked;
	public event EventHandler<MouseWheelEventArgs>? Scrolled;

	#endregion

	#region Observables

	public IObservable<ButtonClickedEventArgs> ClickEvents => _clickedSubject.AsObservable();
	public IObservable<MouseWheelEventArgs> ScrollEvents => _scrolledSubject.AsObservable();

	#endregion

	#region Fields

	private RadialColor _baseColor;
	private RadialColor _offsetColor;
	private RadialColor _derivedColor;
	private bool _hasMouseHover = false;
	private bool _isFocused = false;
	private bool _isSelected = false;

	#endregion

	#region Constructors

	public SelectableColor(IResourceManager resources, IRenderingContext rc, Vector2 position, RadialColor offsetColor)
		: base(resources, rc)
	{
		Position = position;
		// Set padding to 0 and content size to SIZE
		Padding = new Thickness(0);
		ContentSize = new Vector2(SIZE, SIZE);
		_baseColor = RadialColor.Black;
		_offsetColor = offsetColor;
		_derivedColor = _baseColor + _offsetColor;

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

	public RadialColor BaseColor
	{
		get => _baseColor;
		set
		{
			ThrowIfDisposed();
			if (_baseColor != value)
			{
				_baseColor = value;
				OnPropertyChanged();
			}
		}
	}

	public RadialColor OffsetColor
	{
		get => _offsetColor;
		set
		{
			ThrowIfDisposed();
			if (_offsetColor != value)
			{
				_offsetColor = value;
				OnPropertyChanged();
			}
		}
	}

	public RadialColor DerivedColor
	{
		get => _derivedColor;
		private set
		{
			ThrowIfDisposed();
			if (_derivedColor != value)
			{
				_derivedColor = value;
				OnPropertyChanged();
			}
		}
	}

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

	#endregion

	#region Methods

	public override void Load()
	{
		ThrowIfDisposed();
		base.Load();
	}

	public override void Unload()
	{
		ThrowIfDisposed();
		base.Unload();
	}

	public override void Render(GameTime gameTime)
	{
		ThrowIfDisposed();
		base.Render(gameTime);

		if (!IsVisible) return;

		var x = (int)AbsolutePosition.X;
		var y = (int)AbsolutePosition.Y;
		var borderColor = Palette.GetIndex(5, 5, 5);

		if (IsSelected)
		{
			borderColor = Palette.GetIndex(5, 0, 0);
			RC.RenderRect(x, y, (int)(x + Size.X), (int)(y + Size.Y), borderColor);
		}
		else if (HasMouseHover)
		{
			borderColor = Palette.GetIndex(5, 3, 0);
			RC.RenderRect(x, y, (int)(x + Size.X), (int)(y + Size.Y), borderColor);
		}

		RC.RenderFilledRect(x + 1, y + 1, (int)(x + Size.X - 1), (int)(y + Size.Y - 1), DerivedColor.Index);
	}

	public override bool MouseMove(MouseMoveEventArgs e)
	{
		ThrowIfDisposed();
		HasMouseHover = IsVisible && AbsoluteBounds.ContainsInclusive(e.Position);
		return false;
	}

	public override bool MouseDown(MouseButtonEventArgs e)
	{
		ThrowIfDisposed();
		if (e.Button == MouseButton.Left)
		{
			if (IsVisible && HasMouseHover)
			{
				IsFocused = true;
				return true;
			}
		}
		return base.MouseDown(e);
	}

	public override bool MouseUp(MouseButtonEventArgs e)
	{
		ThrowIfDisposed();
		if (e.Button == MouseButton.Left)
		{
			if (IsFocused && HasMouseHover)
			{
				var args = new ButtonClickedEventArgs();
				_clickedSubject.OnNext(args);
				Clicked?.Invoke(this, args);
				return true;
			}
			IsFocused = false;
		}
		return base.MouseUp(e);
	}

	public override bool MouseWheel(MouseWheelEventArgs e)
	{
		ThrowIfDisposed();
		if (IsVisible && HasMouseHover)
		{
			_scrolledSubject.OnNext(e);
			Scrolled?.Invoke(this, e);
			return true;
		}
		return base.MouseWheel(e);
	}

	public override bool KeyDown(KeyboardKeyEventArgs e)
	{
		ThrowIfDisposed();
		if (IsFocused && (e.Key == Keys.Enter || e.Key == Keys.Space))
		{
			var args = new ButtonClickedEventArgs();
			_clickedSubject.OnNext(args);
			Clicked?.Invoke(this, args);
			return true;
		}
		return base.KeyDown(e);
	}

	protected override void OnPropertyChanged([CallerMemberName] string propertyName = "")
	{
		base.OnPropertyChanged(propertyName);

		if (propertyName == nameof(BaseColor) || propertyName == nameof(OffsetColor))
		{
			DerivedColor = BaseColor + OffsetColor;
		}
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

		base.DisposeManagedResources();
	}

	#endregion
}