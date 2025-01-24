using System.ComponentModel;
using System.Runtime.CompilerServices;
using Critters.Events;
using Critters.Gfx;
using Critters.IO;
using Critters.UI;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Critters.States;

class SelectableColor : UIElement
{
	#region Constants

	private const int SIZE = 10;

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
		rc.RenderFilledRect(x + 1, y + 1, x + SIZE - 2, y + SIZE - 2, DerivedColor.GetIndex());
	}

	private void OnMouseMove(MouseMoveEventArgs e)
	{
		HasMouseHover = AbsoluteBounds.ContainsInclusive(e.Position);
	}

	private void OnMouseButton(MouseButtonEventArgs e)
	{
		IsSelected = false;

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
					IsSelected = true;
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

class ColorPicker : UIElement
{
	#region Fields

	private List<SelectableColor> _baseColors = new List<SelectableColor>();
	private List<SelectableColor> _derivedColors = new List<SelectableColor>();
	private List<UIElement> _ui = new List<UIElement>();
	private RadialColor _selectedBaseColor = new RadialColor(0, 0, 0);
	private RadialColor _selectedDerivedColor = new RadialColor(0, 0, 0);

	#endregion

	#region Constructors

	public ColorPicker(Vector2 position)
		: this(null, position)
	{
	}

	public ColorPicker(UIElement? parent, Vector2 position)
		: base(parent)
	{
		Position = position;

		for (byte xc = 0; xc < 6; xc++)
		{
			{
				var x = (int)AbsolutePosition.X + xc * 10;
				var y = (int)AbsolutePosition.Y;
				var color = new RadialColor(0, 0, xc);
				var elem = new SelectableColor(this, new Vector2(x, y), color);
				_baseColors.Add(elem);
				_ui.Add(elem);
			}

			for (byte yc = 0; yc < 6; yc++)
			{
				var x = (int)AbsolutePosition.X + xc * 10;
				var y = (int)AbsolutePosition.Y + 12 + yc * 10;
				var color = new RadialColor(xc, yc, 0);
				var elem = new SelectableColor(this, new Vector2(x, y), color);
				_derivedColors.Add(elem);
				_ui.Add(elem);
			}
		}
	}

	#endregion

	#region Properties

	public RadialColor SelectedBaseColor
	{
		get
		{
			return _selectedBaseColor;
		}
		private set
		{
			if (_selectedBaseColor != value)
			{
				_selectedBaseColor = value;
				OnPropertyChanged();
			}
		}
	}

	public RadialColor SelectedDerivedColor
	{
		get
		{
			return _selectedDerivedColor;
		}
		private set
		{
			if (_selectedDerivedColor != value)
			{
				_selectedDerivedColor = value;
				OnPropertyChanged();
			}
		}
	}

	#endregion

	#region Methods

	public override void Load(ResourceManager resources, EventBus eventBus)
	{
		base.Load(resources, eventBus);

		foreach (var c in _baseColors)
		{
			c.PropertyChanged += OnBaseColorPropertyChanged;
		}
		foreach (var c in _derivedColors)
		{
			c.PropertyChanged += OnDerivedColorPropertyChanged;
		}

		foreach (var ui in _ui)
		{
			ui.Load(resources, eventBus);
		}
	}

	public override void Unload(ResourceManager resources, EventBus eventBus)
	{
		base.Unload(resources, eventBus);

		foreach (var c in _baseColors)
		{
			c.PropertyChanged -= OnBaseColorPropertyChanged;
		}
		foreach (var c in _derivedColors)
		{
			c.PropertyChanged -= OnDerivedColorPropertyChanged;
		}

		foreach (var ui in _ui)
		{
			ui.Unload(resources, eventBus);
		}
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);

		foreach (var ui in _ui)
		{
			ui.Update(gameTime);
		}
	}

	public override void Render(RenderingContext rc, GameTime gameTime)
	{
		base.Render(rc, gameTime);

		foreach (var ui in _ui)
		{
			ui.Render(rc, gameTime);
		}
	}

	private void OnBaseColorPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (sender is SelectableColor c)
		{
			if (c.IsSelected)
			{
				SelectedBaseColor = c.DerivedColor;
				foreach (var dc in _derivedColors)
				{
					dc.BaseColor = SelectedBaseColor;
				}
			}
		}
	}

	private void OnDerivedColorPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (sender is SelectableColor c)
		{
			if (c.IsSelected)
			{
				SelectedDerivedColor = c.DerivedColor;
			}
		}
	}

	#endregion
}

/// <summary>
/// Load up an existing glyph, or draw a new one.  Select the colors, save to disk.
/// </summary>
class GlyphEditState : GameState
{
	#region Fields

	private List<UIElement> _ui = new List<UIElement>();
	
	#endregion

	#region Constructors

	public GlyphEditState()
	{
		var label = new Label("Color", new Vector2(0, 0), Palette.GetIndex(5, 5, 5), Palette.GetIndex(0, 0, 0));
		_ui.Add(label);

		var colorPicker = new ColorPicker(new Vector2(0, 8));
		_ui.Add(colorPicker);
	}

	#endregion

	#region Properties

	#endregion

	#region Methods

	public override void Load(ResourceManager resources, EventBus eventBus)
	{
		base.Load(resources, eventBus);

		foreach (var ui in _ui)
		{
			ui.Load(resources, eventBus);
		}
	}

	public override void Unload(ResourceManager resources, EventBus eventBus)
	{
		base.Unload(resources, eventBus);

		foreach (var ui in _ui)
		{
			ui.Unload(resources, eventBus);
		}
	}


	public override void Render(RenderingContext rc, GameTime gameTime)
	{
		base.Render(rc, gameTime);

		rc.Fill(0);

		foreach (var ui in _ui)
		{
			ui.Render(rc, gameTime);
		}
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);

		foreach (var ui in _ui)
		{
			ui.Update(gameTime);
		}
	}

	#endregion
}
