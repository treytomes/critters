using System.ComponentModel;
using Critters.Events;
using Critters.Gfx;
using Critters.IO;
using Critters.UI;
using OpenTK.Mathematics;

namespace Critters.States;

/// <summary>
/// Load up an existing glyph, or draw a new one.  Select the colors, save to disk.
/// </summary>
class GlyphEditState : GameState
{
	#region Fields

	private List<UIElement> _ui = new List<UIElement>();
	private GlyphSet<Bitmap>? _tiles = null;
	private ColorPicker _fgPicker;
	private ColorPicker _bgPicker;
	private GlyphPicker _glyphPicker;
	
	#endregion

	#region Constructors

	public GlyphEditState()
	{
		var screenWidth = 320;
		var colorPickerWidth = 79;
		var padding = 2;

		{
			var x = screenWidth - colorPickerWidth;
			var y = 0;
			_ui.Add(new Label("Background", new Vector2(x, y), Palette.GetIndex(5, 5, 5), Palette.GetIndex(0, 0, 0)));
			_bgPicker = new ColorPicker(new Vector2(x, y + 8));
			_bgPicker.SelectedColor = new RadialColor(3, 4, 5);
			_ui.Add(_bgPicker);
		}

		{
			var x = screenWidth - colorPickerWidth * 2 - padding;
			var y = 0;
			_ui.Add(new Label("Foreground", new Vector2(x, y), Palette.GetIndex(5, 5, 5), Palette.GetIndex(0, 0, 0)));
			_fgPicker = new ColorPicker(new Vector2(x, y + 8));
			_fgPicker.SelectedColor = new RadialColor(5, 4, 3);
			_ui.Add(_fgPicker);
		}

		// height=168
		_ui.Add(new Label("Glyphs", new Vector2(0, 0), Palette.GetIndex(5, 5, 5), Palette.GetIndex(0, 0, 0)));
		_glyphPicker = new GlyphPicker(new Vector2(0, 8));
		_glyphPicker.SelectedGlyphIndex = 2;
		_glyphPicker.ForegroundColor = _fgPicker.SelectedColor;
		_glyphPicker.BackgroundColor = _bgPicker.SelectedColor;
		_ui.Add(_glyphPicker);
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

		_bgPicker.PropertyChanged += OnBGPickerPropertyChanged;
		_fgPicker.PropertyChanged += OnFGPickerPropertyChanged;

    var image = resources.Load<Image>("oem437_8.png");
    _tiles = new GlyphSet<Bitmap>(new Bitmap(image), 8, 8);
	}

	public override void Unload(ResourceManager resources, EventBus eventBus)
	{
		base.Unload(resources, eventBus);

		foreach (var ui in _ui)
		{
			ui.Unload(resources, eventBus);
		}

		_bgPicker.PropertyChanged -= OnBGPickerPropertyChanged;
		_fgPicker.PropertyChanged -= OnFGPickerPropertyChanged;
	}


	public override void Render(RenderingContext rc, GameTime gameTime)
	{
		base.Render(rc, gameTime);

		rc.Fill(0);

		foreach (var ui in _ui)
		{
			ui.Render(rc, gameTime);
		}

		var scale = 16;
		new Bitmap(_tiles?[_glyphPicker.SelectedGlyphIndex], scale).Render(rc, new Vector2(rc.Width - (_tiles?.TileWidth * scale) ?? 0, 100), _fgPicker.SelectedColor, _bgPicker.SelectedColor);
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);

		foreach (var ui in _ui)
		{
			ui.Update(gameTime);
		}
	}

	private void OnBGPickerPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(_bgPicker.SelectedColor))
		{
			_glyphPicker.BackgroundColor = _bgPicker.SelectedColor;
		}
	}

	private void OnFGPickerPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(_fgPicker.SelectedColor))
		{
			_glyphPicker.ForegroundColor = _fgPicker.SelectedColor;
		}
	}

	#endregion
}
