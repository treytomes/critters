using System.ComponentModel;
using System.Runtime.CompilerServices;
using Critters.Events;
using Critters.Gfx;
using Critters.IO;
using Critters.Services;
using Critters.States;
using OpenTK.Mathematics;

namespace Critters.UI;

/// <summary>
/// UI element position and size is measured in tiles.
/// The PixelBounds property will convert to virtual display pixels for things like mouse hover check.
/// </summary>
class UIElement : IGameComponent, INotifyPropertyChanged
{
	#region Events

	public event PropertyChangedEventHandler? PropertyChanged;

	#endregion

	#region Fields

	private bool _isLoaded = false;
	private UIElement? _parent;
	private Vector2 _position = Vector2.Zero;
	private Vector2 _size = Vector2.Zero;
	private Thickness _padding = new(0);
	private Thickness _margin = new(0);

	#endregion

	#region Constructors

	public UIElement(UIElement? parent = null)
	{
		_parent = parent;
	}

	#endregion

	#region Properties

	public bool IsLoaded
	{
		get
		{
			return _isLoaded;
		}
		private set
		{
			if (_isLoaded != value)
			{
				_isLoaded = value;
				OnPropertyChanged();
			}
		}
	}

	public UIElement? Parent
	{
		get
		{
			return _parent;
		}
		set
		{
			if (_parent != value)
			{
				if (_parent != null)
				{
					_parent.PropertyChanged -= OnParentPropertyChanged;
				}

				_parent = value;

				if (_parent != null)
				{
					_parent.PropertyChanged += OnParentPropertyChanged;
				}

				OnPropertyChanged();
			}
		}
	}

	public Vector2 Position
	{
		get
		{
			return _position;
		}
		set
		{
			if (_position != value)
			{
				_position = value;
				OnPropertyChanged();
			}
		}
	}

	public Vector2 Size
	{
		get
		{
			return _size;
		}
		set
		{
			if (_size != value)
			{
				_size = value;
				OnPropertyChanged();
			}
		}
	}

	public Box2 Bounds
	{
		get
		{
			return new Box2(Position, Position + Size);
		}
	}

	public Thickness Padding
	{
		get
		{
			return _padding;
		}
		set
		{
			if (_padding != value)
			{
				_padding = value;
				OnPropertyChanged();
			}
		}
	}

	public Thickness Margin
	{
		get
		{
			return _margin;
		}
		set
		{
			if (_margin != value)
			{
				_margin = value;
				OnPropertyChanged();
			}
		}
	}

	public Vector2 AbsolutePosition
	{
		get
		{
			var position = new Vector2(Margin.Left, Margin.Top) + Position;
			if (Parent != null)
			{
				return Parent.AbsolutePosition + new Vector2(Parent.Padding.Left, Parent.Padding.Top) + position;
			}
			else
			{
				return position;
			}
		}
	}

	public Box2 AbsoluteBounds => new(AbsolutePosition, AbsolutePosition + Size);

	#endregion

	#region Methods

	public virtual void Load(IResourceManager resources, IEventBus eventBus)
	{
		if (IsLoaded)
		{
			return;
		}
		IsLoaded = true;
	}

	public virtual void Unload(IResourceManager resources, IEventBus eventBus)
	{
		if (!IsLoaded)
		{
			return;
		}
		IsLoaded = false;
	}

	public virtual void Render(RenderingContext rc, GameTime gameTime)
	{
	}

	public virtual void Update(GameTime gameTime)
	{
	}

	protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

		if (propertyName == nameof(Margin) || propertyName == nameof(Position))
		{
			OnPropertyChanged(nameof(AbsolutePosition));
		}

		if (propertyName == nameof(AbsolutePosition) || propertyName == nameof(Size))
		{
			OnPropertyChanged(nameof(AbsoluteBounds));
		}

		if (propertyName == nameof(Padding))
		{
			Size = new Vector2(Padding.Left + Padding.Right, Padding.Top + Padding.Bottom);
		}
	}

	protected virtual void OnParentPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(AbsolutePosition) || e.PropertyName == nameof(Padding))
		{
			OnPropertyChanged(nameof(AbsolutePosition));
		}
	}

	#endregion
}
