using Critters.Events;
using Critters.Gfx;
using Critters.IO;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;

namespace Critters.UI;

class ColorPicker : UIElement
{
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

		for (byte xc = 0; xc < 6; xc++)
		{
			// Color Header
			{
				var x = xc * 10;
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
			for (byte yc = 0; yc < 6; yc++)
			{
				// I'm inverting these on person.  Mouse scrolling will make more sense.
				var y = 12 + xc * 10;
				var x = yc * 10;
				
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

		_selectedColor = new Rectangle(this, new Box2(10 * 6 + 2, 0, 10 * 6 + 2 + 24, 10 * 7 + 1), new RadialColor(5, 5, 5), new RadialColor(0, 0, 0));
		_ui.Add(_selectedColor);

		_colorLabel = new Label(this, "(0,0,0)=0", new Vector2(0, 10 * 7 + 3), Palette.GetIndex(5, 5, 5), Palette.GetIndex(0, 0, 0));
		_ui.Add(_colorLabel);

		_selectedColor.FillColor = _selectedDerivedColor!.DerivedColor;
		_colorLabel.Text = $"({_selectedColor.FillColor.Red},{_selectedColor.FillColor.Green},{_selectedColor.FillColor.Blue})={_selectedColor.FillColor.Index}";
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
			SelectBaseColor(_baseColors.First(x => x.DerivedColor.Blue == value.Blue));
			SelectDerivedColor(_derivedColors.First(x => x.DerivedColor.Red == value.Red && x.DerivedColor.Green == value.Green));
		}
	}

	private int SelectedBaseColorIndex
	{
		get
		{
			return _baseColors.IndexOf(_baseColors.Single(x => x.IsSelected));
		}
	}

	private int SelectedDerivedColorIndex
	{
		get
		{
			return _derivedColors.IndexOf(_derivedColors.Single(x => x.IsSelected));
		}
	}

	#endregion

	#region Methods

	public override void Load(ResourceManager resources, EventBus eventBus)
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

	public override void Unload(ResourceManager resources, EventBus eventBus)
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
		_colorLabel.Text = $"({_selectedColor.FillColor.Red},{_selectedColor.FillColor.Green},{_selectedColor.FillColor.Blue})={_selectedColor.FillColor.Index}";
	}

	private void OnBaseColorClicked(object? sender, ButtonClickedEventArgs e)
	{
		var color = sender as SelectableColor;
		if (color == null)
		{
			throw new ArgumentException("Must be a SelectableColor.", nameof(sender));
		}

		SelectBaseColor(color);
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
		_colorLabel.Text = $"({_selectedColor.FillColor.Red},{_selectedColor.FillColor.Green},{_selectedColor.FillColor.Blue})={_selectedColor.FillColor.Index}";
	}

	private void OnDerivedColorClicked(object? sender, ButtonClickedEventArgs e)
	{
		var color = sender as SelectableColor;
		if (color == null)
		{
			throw new ArgumentException("Must be a SelectableColor.", nameof(sender));
		}

		SelectDerivedColor(color);
	}

	private void OnDerivedColorScrolled(object? sender, MouseWheelEventArgs e)
	{
		var delta = Math.Sign(e.OffsetY);
		for (var n = 0; n < _derivedColors.Count(); n++)
		{
			if (_derivedColors[n].IsSelected)
			{
				var newIndex = (n + delta + _derivedColors.Count) % _derivedColors.Count;
				SelectDerivedColor(_derivedColors[newIndex]);
				break;
			}
		}
	}
	
	#endregion
}
