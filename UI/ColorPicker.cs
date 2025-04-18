using Critters.Events;
using Critters.Gfx;
using Critters.IO;
using Critters.Services;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;

namespace Critters.UI;

class ColorPicker : UIElement
{
	#region Constants

	private const int BUTTON_SIZE = 8;
	private const int BUTTON_PADDING = 1;
	private const int GRID_SIZE = 6;

	#endregion

	#region Fields

	private List<SelectableColor> _baseColors = new List<SelectableColor>();
	private List<SelectableColor> _derivedColors = new List<SelectableColor>();
	private List<UIElement> _ui = new List<UIElement>();
	private SelectableColor? _selectedBaseColor = null;
	private SelectableColor? _selectedDerivedColor = null;
	private Rectangle _selectedColor;
	private Label _colorLabel;

	#endregion

	#region Constructors

	public ColorPicker(Vector2 position, RadialColor? initialColor = null)
		: this(null, position, initialColor)
	{
	}

	public ColorPicker(UIElement? parent, Vector2 position, RadialColor? initialColor = null)
		: base(parent)
	{
		Position = position;

		var selectedBaseColor = initialColor.HasValue ? new RadialColor(0, 0, initialColor.Value.Blue) : new RadialColor(0, 0, 0);

		for (byte xc = 0; xc < GRID_SIZE; xc++)
		{
			// Color Header
			{
				var x = xc * (BUTTON_SIZE + BUTTON_PADDING);
				var y = 0;
				var color = new RadialColor(0, 0, xc);
				var elem = new SelectableColor(this, new Vector2(x, y), color);
				if (initialColor?.Blue == xc)
				{
					elem.IsSelected = true;
				}
				_baseColors.Add(elem);
				_ui.Add(elem);
			}

			// Color Grid
			for (byte yc = 0; yc < GRID_SIZE; yc++)
			{
				// I'm inverting these on person.  Mouse scrolling will make more sense.
				var y = (BUTTON_SIZE + BUTTON_PADDING + 2) + xc * (BUTTON_SIZE + BUTTON_PADDING);
				var x = yc * (BUTTON_SIZE + BUTTON_PADDING);

				var color = new RadialColor(xc, yc, 0);
				var elem = new SelectableColor(this, new Vector2(x, y), color);
				elem.BaseColor = selectedBaseColor;
				if (initialColor?.Red == xc && initialColor?.Green == yc)
				{
					elem.IsSelected = true;
				}
				_derivedColors.Add(elem);
				_ui.Add(elem);
			}
		}

		if (!_baseColors.Any(x => x.IsSelected))
		{
			_baseColors[0].IsSelected = true;
			_selectedBaseColor = _baseColors[0];
		}
		else
		{
			_selectedBaseColor = _baseColors.First(x => x.IsSelected);
		}

		if (!_derivedColors.Any(x => x.IsSelected))
		{
			_derivedColors[0].IsSelected = true;
			_selectedDerivedColor = _derivedColors[0];
		}
		else
		{
			_selectedDerivedColor = _derivedColors.First(x => x.IsSelected);
		}

		_selectedColor = new Rectangle(this, new Box2((BUTTON_SIZE + BUTTON_PADDING) * GRID_SIZE + 2, 0, (BUTTON_SIZE + BUTTON_PADDING) * GRID_SIZE + 2 + 22, (BUTTON_SIZE + BUTTON_PADDING) * (GRID_SIZE + 1) + 2), new RadialColor(5, 5, 5), new RadialColor(0, 0, 0));
		_ui.Add(_selectedColor);

		_colorLabel = new Label(this, "000==0", new Vector2(0, (BUTTON_SIZE + BUTTON_PADDING) * (GRID_SIZE + 1) + 4), new RadialColor(5, 5, 5), new RadialColor(0, 0, 0));
		_ui.Add(_colorLabel);

		_selectedColor.FillColor = _selectedDerivedColor!.DerivedColor;
		_colorLabel.Text = StringProvider.From($"{_selectedColor.FillColor.Red}{_selectedColor.FillColor.Green}{_selectedColor.FillColor.Blue}=={_selectedColor.FillColor.Index}");
	}

	#endregion

	#region Properties

	public RadialColor SelectedColor
	{
		get
		{
			return _selectedDerivedColor?.DerivedColor ?? new RadialColor(0, 0, 0);
		}
		set
		{
			if (SelectedColor != value)
			{
				SelectBaseColor(_baseColors.First(x => x.DerivedColor.Blue == value.Blue));
				SelectDerivedColor(_derivedColors.First(x => x.DerivedColor.Red == value.Red && x.DerivedColor.Green == value.Green));
				OnPropertyChanged();
			}
		}
	}

	#endregion

	#region Methods

	public override void Load(IResourceManager resources, IEventBus eventBus)
	{
		base.Load(resources, eventBus);

		foreach (var c in _baseColors)
		{
			c.Clicked += OnBaseColorClicked;
			c.Scrolled += OnBaseColorScrolled;
		}
		foreach (var c in _derivedColors)
		{
			c.Clicked += OnDerivedColorClicked;
			c.Scrolled += OnDerivedColorScrolled;
		}

		foreach (var ui in _ui)
		{
			ui.Load(resources, eventBus);
		}
	}

	public override void Unload(IResourceManager resources, IEventBus eventBus)
	{
		base.Unload(resources, eventBus);

		foreach (var c in _baseColors)
		{
			c.Clicked -= OnBaseColorClicked;
			c.Scrolled -= OnBaseColorScrolled;
		}
		foreach (var c in _derivedColors)
		{
			c.Clicked -= OnDerivedColorClicked;
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

		rc.RenderFilledRect(new Box2(AbsolutePosition, new Vector2(AbsolutePosition.X + (BUTTON_SIZE + BUTTON_PADDING) * GRID_SIZE, AbsolutePosition.Y + (BUTTON_SIZE + BUTTON_PADDING))), new RadialColor(5, 5, 5));
		rc.RenderFilledRect(new Box2(new Vector2(AbsolutePosition.X, AbsolutePosition.Y + (BUTTON_SIZE + BUTTON_PADDING + 2)), new Vector2(AbsolutePosition.X + (BUTTON_SIZE + BUTTON_PADDING) * GRID_SIZE, AbsolutePosition.Y + 2 + (GRID_SIZE + 1) * (BUTTON_SIZE + BUTTON_PADDING))), new RadialColor(5, 5, 5));

		foreach (var ui in _ui)
		{
			ui.Render(rc, gameTime);
		}
	}

	private void SelectBaseColor(SelectableColor c)
	{
		if (_selectedBaseColor != null)
		{
			_selectedBaseColor.IsSelected = false;
		}
		_selectedBaseColor = c;
		_selectedBaseColor.IsSelected = true;

		foreach (var derivedColor in _derivedColors)
		{
			derivedColor.BaseColor = _selectedBaseColor!.DerivedColor;
		}

		_selectedColor.FillColor = _selectedDerivedColor!.DerivedColor;
		_colorLabel.Text = StringProvider.From($"{_selectedColor.FillColor.Red}{_selectedColor.FillColor.Green}{_selectedColor.FillColor.Blue}=={_selectedColor.FillColor.Index}");
	}

	private void OnBaseColorClicked(object? sender, ButtonClickedEventArgs e)
	{
		var color = sender as SelectableColor;
		if (color == null)
		{
			throw new ArgumentException($"Must be a {nameof(SelectableColor)}.", nameof(sender));
		}

		SelectBaseColor(color);
		OnPropertyChanged(nameof(SelectedColor));
	}

	private void OnBaseColorScrolled(object? sender, MouseWheelEventArgs e)
	{
		var delta = Math.Sign(e.OffsetY);
		for (var n = 0; n < _baseColors.Count(); n++)
		{
			if (_baseColors[n].IsSelected)
			{
				var newIndex = (n + delta + _baseColors.Count) % _baseColors.Count;
				SelectBaseColor(_baseColors[newIndex]);
				OnPropertyChanged(nameof(SelectedColor));
				break;
			}
		}
	}

	private void SelectDerivedColor(SelectableColor c)
	{
		if (_selectedDerivedColor != null)
		{
			_selectedDerivedColor.IsSelected = false;
		}
		_selectedDerivedColor = c;
		_selectedDerivedColor!.IsSelected = true;
		_selectedColor.FillColor = _selectedDerivedColor!.DerivedColor;
		_colorLabel.Text = StringProvider.From($"{_selectedColor.FillColor.Red}{_selectedColor.FillColor.Green}{_selectedColor.FillColor.Blue}=={_selectedColor.FillColor.Index}");
	}

	private void OnDerivedColorClicked(object? sender, ButtonClickedEventArgs e)
	{
		var color = sender as SelectableColor;
		if (color == null)
		{
			throw new ArgumentException("Must be a SelectableColor.", nameof(sender));
		}

		SelectDerivedColor(color);
		OnPropertyChanged(nameof(SelectedColor));
	}

	private void OnDerivedColorScrolled(object? sender, MouseWheelEventArgs e)
	{
		var delta = -Math.Sign(e.OffsetY);
		for (var n = 0; n < _derivedColors.Count(); n++)
		{
			if (_derivedColors[n].IsSelected)
			{
				var newIndex = (n + delta + _derivedColors.Count) % _derivedColors.Count;
				SelectDerivedColor(_derivedColors[newIndex]);
				OnPropertyChanged(nameof(SelectedColor));
				break;
			}
		}
	}

	#endregion
}
