using Critters.Events;
using Critters.Gfx;
using Critters.IO;
using Critters.UI;
using OpenTK.Mathematics;

namespace Critters.States;

class ColorPicker : UIElement
{
	#region Fields

	private List<SelectableColor> _baseColors = new List<SelectableColor>();
	private List<SelectableColor> _derivedColors = new List<SelectableColor>();
	private List<UIElement> _ui = new List<UIElement>();
	private SelectableColor? _selectedBaseColor = null;
	private SelectableColor? _selectedDerivedColor = null;

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

		var left = AbsolutePosition.X + 10 * 6 + 2;
		var right = left + 24;
		var top = AbsolutePosition.Y;
		var bottom = top + 10 * 7 + 1;
		var bounds = new Box2(left, top, right, bottom);
		rc.RenderRect(bounds, rc.Palette[5, 5, 5]);
		rc.RenderRect(bounds.Inflated(new Vector2(-1, -1)), 0);
		rc.RenderFilledRect(bounds.Inflated(new Vector2(-2, -2)), SelectedColor.Index);
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
	}

	private void OnDerivedColorClicked(object? sender, ButtonClickedEventArgs e)
	{
		if (_selectedDerivedColor != null)
		{
			_selectedDerivedColor.IsSelected = false;
		}
		_selectedDerivedColor = sender as SelectableColor;
		_selectedDerivedColor!.IsSelected = true;
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
