using Critters.Events;
using Critters.Gfx;
using Critters.IO;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;

namespace Critters.UI;

class GlyphPicker : UIElement
{
	#region Fields

	private List<SelectableGlyph> _selectableGlyphs = new List<SelectableGlyph>();
	private List<UIElement> _ui = new List<UIElement>();
	private SelectableGlyph _selectedGlyph;
	private Label _glyphLabel;

	#endregion

	#region Constructors

	public GlyphPicker(Vector2 position, byte? initialGlyph = null)
		: this(null, position, initialGlyph)
	{
	}

	public GlyphPicker(UIElement? parent, Vector2 position, byte? initialGlyph = null)
		: base(parent)
	{
		Position = position;

		byte selectedGlyphIndex = initialGlyph ?? 0;

		var resourcePath = "oem437_8.png";
		var padding = Vector2.Zero;
		var numGlyphs = 256;
		var glyphsPerRow = (int)Math.Sqrt(numGlyphs);
		var numGlyphRows = (int)(numGlyphs / glyphsPerRow);
		byte glyphIndex = 0;
		for (byte yc = 0; yc < numGlyphRows; yc++)
		{
			for (byte xc = 0; xc < glyphsPerRow; xc++)
			{
				// I'm inverting these on person.  Mouse scrolling will make more sense.
				var x = xc * 9; // tile size + (border width + padding) * 2
				var y = yc * 9;
				
				var elem = new SelectableGlyph(this, padding + new Vector2(x, y), resourcePath, glyphIndex);
				if (selectedGlyphIndex == glyphIndex)
				{
					elem.IsSelected = true;
					_selectedGlyph = elem;
				}
				_selectableGlyphs.Add(elem);
				_ui.Add(elem);

				glyphIndex++;
			}
		}

		_selectedGlyph = _selectableGlyphs[selectedGlyphIndex];

		_glyphLabel = new Label(this, "index=0", new Vector2(0, 9 * numGlyphRows + 2), Palette.GetIndex(5, 5, 5), Palette.GetIndex(0, 0, 0));
		_ui.Add(_glyphLabel);

		_glyphLabel.Text = $"glyph index={_selectedGlyph.GlyphIndex}";
	}

	#endregion

	#region Properties

	public byte SelectedGlyphIndex
	{
		get
		{
			return _selectedGlyph!.GlyphIndex;
		}
		set
		{
			SelectGlyph(_selectableGlyphs.First(x => x.GlyphIndex == value));
		}
	}

	#endregion

	#region Methods

	public override void Load(ResourceManager resources, EventBus eventBus)
	{
		base.Load(resources, eventBus);

		foreach (var c in _selectableGlyphs)
		{
			c.Clicked += OnGlyphClicked;
			c.Scrolled += OnGlyphScrolled;
		}

		foreach (var ui in _ui)
		{
			ui.Load(resources, eventBus);
		}
	}

	public override void Unload(ResourceManager resources, EventBus eventBus)
	{
		base.Unload(resources, eventBus);

		foreach (var c in _selectableGlyphs)
		{
			c.Clicked -= OnGlyphClicked;
			c.Scrolled -= OnGlyphScrolled;
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

		rc.RenderFilledRect(AbsolutePosition, AbsolutePosition + new Vector2(9 * 16, 9 * 16), new RadialColor(5, 5, 5).Index);

		foreach (var ui in _ui)
		{
			ui.Render(rc, gameTime);
		}
	}

	private void SelectGlyph(SelectableGlyph c)
	{
		if (_selectedGlyph != null)
		{
			_selectedGlyph.IsSelected = false;
		}
		_selectedGlyph = c;
		_selectedGlyph.IsSelected = true;

		_glyphLabel.Text = $"glyph index={_selectedGlyph.GlyphIndex}";
	}

	private void OnGlyphClicked(object? sender, ButtonClickedEventArgs e)
	{
		var glyph = sender as SelectableGlyph;
		if (glyph == null)
		{
			throw new ArgumentException($"Must be a {nameof(SelectableGlyph)}.", nameof(sender));
		}

		SelectGlyph(glyph);
	}

	private void OnGlyphScrolled(object? sender, MouseWheelEventArgs e)
	{
		var delta = Math.Sign(e.OffsetY);
		for (var n = 0; n < _selectableGlyphs.Count; n++)
		{
			if (_selectableGlyphs[n].IsSelected)
			{
				var newIndex = (n + delta + _selectableGlyphs.Count) % _selectableGlyphs.Count;
				SelectGlyph(_selectableGlyphs[newIndex]);
				break;
			}
		}
	}
	
	#endregion
}
