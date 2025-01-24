using System.Runtime.CompilerServices;
using Critters.Events;
using Critters.Gfx;
using Critters.IO;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Critters.UI;

class SelectableColor : UIElement
{
	#region Constants

	private const int SIZE = 10;

	#endregion

	#region Events

	public event EventHandler<ButtonClickedEventArgs>? Clicked;

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

	public SelectableColor(UIElement? parent, Vector2 position, RadialColor offsetColor)
		: base(parent)
	{
		Position = position;
		Size = new Vector2(SIZE, SIZE);
		BaseColor = new RadialColor(0, 0, 0);
		OffsetColor = offsetColor;
		DerivedColor = _baseColor + _offsetColor;
	}

	#endregion

	#region Properties

	public RadialColor BaseColor
	{
		get
		{
			return _baseColor;
		}
		set
		{
			if (_baseColor != value)
			{
				_baseColor = value;
				OnPropertyChanged();
			}
		}
	}

	public RadialColor OffsetColor
	{
		get
		{
			return _offsetColor;
		}
		set
		{
			if (_offsetColor != value)
			{
				_offsetColor = value;
				OnPropertyChanged();
			}
		}
	}

	public RadialColor DerivedColor
	{
		get
		{
			return _derivedColor;
		}
		private set
		{
			if (_derivedColor != value)
			{
				_derivedColor = value;
				OnPropertyChanged();
			}
		}
	}

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
	}

	public override void Unload(ResourceManager resources, EventBus eventBus)
	{
		base.Unload(resources, eventBus);

		eventBus.Unsubscribe<MouseMoveEventArgs>(OnMouseMove);
		eventBus.Unsubscribe<MouseButtonEventArgs>(OnMouseButton);
	}

	public override void Render(RenderingContext rc, GameTime gameTime)
	{
		base.Render(rc, gameTime);

		var x = (int)AbsolutePosition.X;
		var y = (int)AbsolutePosition.Y;
		var borderColor = Palette.GetIndex(5, 5, 5);
		if (IsSelected)
		{
			borderColor = Palette.GetIndex(5, 0, 0);
		}
		else if (HasMouseHover)
		{
			borderColor = Palette.GetIndex(5, 3, 0);
		}
		rc.RenderRect(x, y, x + SIZE - 1, y + SIZE - 1, borderColor);
		rc.RenderFilledRect(x + 1, y + 1, x + SIZE - 2, y + SIZE - 2, DerivedColor.Index);
	}

	private void OnMouseMove(MouseMoveEventArgs e)
	{
		HasMouseHover = AbsoluteBounds.ContainsInclusive(e.Position);
	}

	private void OnMouseButton(MouseButtonEventArgs e)
	{
		if (e.Button == MouseButton.Left)
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

	protected override void OnPropertyChanged([CallerMemberName] string propertyName = "")
	{
		base.OnPropertyChanged(propertyName);

		if (propertyName == nameof(BaseColor) || propertyName == nameof(OffsetColor))
		{
			DerivedColor = BaseColor + OffsetColor;
		}
	}

	#endregion
}
