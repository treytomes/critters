using System.ComponentModel;
using Critters.Events;
using Critters.Gfx;
using Critters.IO;
using Critters.Services;
using Critters.UI;
using OpenTK.Mathematics;

namespace Critters.States;

/// <summary>
/// Load up an existing glyph, or draw a new one.  Select the colors, save to disk.
/// </summary>
class GlyphEditState : GameState
{
	#region Fields

	private GlyphSet<Bitmap>? _tiles = null;
	private ColorPicker _fgPicker;
	private ColorPicker _bgPicker;
	private GlyphPicker _glyphPicker;

	#endregion

	#region Constructors

	public GlyphEditState(IResourceManager resources, IRenderingContext rc)
		: base(resources, rc)
	{
		var screenWidth = 320;
		var colorPickerWidth = 79;
		var padding = 2;

		{
			var x = screenWidth - colorPickerWidth;
			var y = 0;
			UI.Add(new Label(resources, rc, "Background", new Vector2(x, y), new RadialColor(5, 5, 5), new RadialColor(0, 0, 0)));
			_bgPicker = new ColorPicker(resources, rc, new Vector2(x, y + 8));
			_bgPicker.SelectedColor = new RadialColor(3, 4, 5);
			UI.Add(_bgPicker);
		}

		{
			var x = screenWidth - colorPickerWidth * 2 - padding;
			var y = 0;
			UI.Add(new Label(resources, rc, "Foreground", new Vector2(x, y), new RadialColor(5, 5, 5), new RadialColor(0, 0, 0)));
			_fgPicker = new ColorPicker(resources, rc, new Vector2(x, y + 8));
			_fgPicker.SelectedColor = new RadialColor(5, 4, 3);
			UI.Add(_fgPicker);
		}

		// height=168
		UI.Add(new Label(resources, rc, "Glyphs", new Vector2(0, 0), new RadialColor(5, 5, 5), new RadialColor(0, 0, 0)));
		_glyphPicker = new GlyphPicker(resources, rc, new Vector2(0, 8));
		_glyphPicker.SelectedGlyphIndex = 2;
		_glyphPicker.ForegroundColor = _fgPicker.SelectedColor;
		_glyphPicker.BackgroundColor = _bgPicker.SelectedColor;
		UI.Add(_glyphPicker);
	}

	#endregion

	#region Methods

	public override void Load()
	{
		base.Load();

		var image = Resources.Load<Image>("oem437_8.png");
		_tiles = new GlyphSet<Bitmap>(new Bitmap(image), 8, 8);
	}

	public override void AcquireFocus()
	{
		base.AcquireFocus();

		_bgPicker.PropertyChanged += OnBGPickerPropertyChanged;
		_fgPicker.PropertyChanged += OnFGPickerPropertyChanged;
	}

	public override void LostFocus()
	{
		_bgPicker.PropertyChanged -= OnBGPickerPropertyChanged;
		_fgPicker.PropertyChanged -= OnFGPickerPropertyChanged;

		base.LostFocus();
	}

	public override void Render(GameTime gameTime)
	{
		RC.Fill(0);

		var scale = 16;
		new Bitmap(_tiles?[_glyphPicker.SelectedGlyphIndex], scale).Render(RC, new Vector2(RC.Width - (_tiles?.TileWidth * scale) ?? 0, 100), _fgPicker.SelectedColor, _bgPicker.SelectedColor);

		base.Render(gameTime);
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
