using Critters.Events;
using Critters.Gfx;
using Critters.IO;
using OpenTK.Mathematics;

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
				var x = xc * 10;
				var y = 0;
				var color = new RadialColor(0, 0, xc);
				var elem = new SelectableColor(this, new Vector2(x, y), color);
				_baseColors.Add(elem);
				_ui.Add(elem);
			}

			for (byte yc = 0; yc < 6; yc++)
			{
				var x = xc * 10;
				var y = 12 + yc * 10;
				var color = new RadialColor(xc, yc, 0);
				var elem = new SelectableColor(this, new Vector2(x, y), color);
				_derivedColors.Add(elem);
				_ui.Add(elem);
			}
		}
		_baseColors[0].IsSelected = true;
		_selectedBaseColor = _baseColors[0];
		
		_derivedColors[0].IsSelected = true;
		_selectedDerivedColor = _derivedColors[0];

		_selectedColor = new Rectangle(this, new Box2(10 * 6 + 2, 0, 10 * 6 + 2 + 24, 10 * 7 + 1), new RadialColor(5, 5, 5), new RadialColor(0, 0, 0));
		_ui.Add(_selectedColor);

		_colorLabel = new Label(this, "(0,0,0)=0", new Vector2(0, 10 * 7 + 3), Palette.GetIndex(5, 5, 5), Palette.GetIndex(0, 0, 0));
		_ui.Add(_colorLabel);
	}

	#endregion

	#region Properties

	public RadialColor SelectedColor
	{
		get
		{
			return _selectedDerivedColor?.DerivedColor ?? new RadialColor(0, 0, 0);
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
		}
		foreach (var c in _derivedColors)
		{
			c.Clicked += OnDerivedColorClicked;
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

	private void OnBaseColorClicked(object? sender, ButtonClickedEventArgs e)
	{
		if (_selectedBaseColor != null)
		{
			_selectedBaseColor.IsSelected = false;
		}
		_selectedBaseColor = sender as SelectableColor;
		_selectedBaseColor!.IsSelected = true;

		foreach (var c in _derivedColors)
		{
			c.BaseColor = _selectedBaseColor!.DerivedColor;
		}

		_selectedColor.FillColor = _selectedDerivedColor!.DerivedColor;
		_colorLabel.Text = $"({_selectedColor.FillColor.Red},{_selectedColor.FillColor.Green},{_selectedColor.FillColor.Blue})={_selectedColor.FillColor.Index}";
	}

	private void OnDerivedColorClicked(object? sender, ButtonClickedEventArgs e)
	{
		if (_selectedDerivedColor != null)
		{
			_selectedDerivedColor.IsSelected = false;
		}
		_selectedDerivedColor = sender as SelectableColor;
		_selectedDerivedColor!.IsSelected = true;
		_selectedColor.FillColor = _selectedDerivedColor!.DerivedColor;
		_colorLabel.Text = $"({_selectedColor.FillColor.Red},{_selectedColor.FillColor.Green},{_selectedColor.FillColor.Blue})={_selectedColor.FillColor.Index}";
	}
	
	#endregion
}
