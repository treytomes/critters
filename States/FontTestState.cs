using Critters.Gfx;
using Critters.Services;

namespace Critters.States;

class FontTestState : GameState
{
	#region Fields

	private GlyphSet<Bitmap>? _tiles;
	private Font? _font;

	#endregion

	#region Constructors

	public FontTestState(IResourceManager resources, IRenderingContext rc)
		: base(resources, rc)
	{
	}

	#endregion

	#region Methods

	public override void Load()
	{
		base.Load();

		var image = Resources.Load<Image>("oem437_8.png");
		_tiles = new GlyphSet<Bitmap>(new Bitmap(image), 8, 8);
		_font = new Font(_tiles);
	}

	public override void Render(GameTime gameTime)
	{
		base.Render(gameTime);

		RC.Fill(RC.Palette[1, 1, 0]);

		_tiles?[1].Render(RC, 100, 100, RC.Palette[5, 5, 5], 255);

		_font?.WriteString(RC, "Hello world!", 150, 120, RC.Palette[5, 4, 3], RC.Palette[0, 0, 0]);
	}

	#endregion
}
