using Critters.Events;
using Critters.Gfx;
using Critters.IO;
using OpenTK.Mathematics;

namespace Critters.UI;

class Label : UIElement
{
	#region Fields

	private Font? _font;

	#endregion

	#region Constructors

	public Label(string text, Vector2 position, byte fgColor, byte bgColor)
	{
		Text = text;
		Position = position;
		ForegroundColor = fgColor;
		BackgroundColor = bgColor;
	}

	#endregion

	#region Properties

	public string Text { get; set; }
	public Vector2 Position { get; set; }
	public Box2 Bounds => new(Position, _font?.MeasureString(Text) ?? Vector2.Zero);
	public byte ForegroundColor { get; set; }
	public byte BackgroundColor { get; set; }

	#endregion

	#region Methods

	public override void Load(ResourceManager resources, EventBus eventBus)
	{
    var image = resources.Load<Image>("oem437_8.png");
		var bmp = new Bitmap(image);
    var tiles = new GlyphSet<Bitmap>(bmp, 8, 8);
    _font = new Font(tiles);
	}

	public override void Unload(ResourceManager resources, EventBus eventBus)
	{
	}

	public override void Render(RenderingContext rc, GameTime gameTime)
	{
		_font?.WriteString(rc, Text, Position, ForegroundColor, BackgroundColor);
	}

	public override void Update(GameTime gameTime)
	{
	}

	#endregion
}
