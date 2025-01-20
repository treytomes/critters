using Critters.Events;
using Critters.Gfx;
using Critters.IO;

namespace Critters.States;

class FontTestState : GameState
{
  #region Fields

  private GlyphSet<Bitmap>? _tiles;
  private Font? _font;

  #endregion

  #region Methods

  public override void Load(ResourceManager resources, EventBus eventBus)
  {
    var image = resources.Load<Image>("oem437_8.png");
    _tiles = new GlyphSet<Bitmap>(new Bitmap(image), 8, 8);
    _font = new Font(_tiles);
  }

  public override void Render(RenderingContext rc, GameTime gameTime)
  {
    rc.Fill(rc.Palette[1, 1, 0]);

    _tiles?[1].Draw(rc, 100, 100, rc.Palette[5, 5, 5], 255);

    _font?.WriteString(rc, "Hello world!", 150, 120, rc.Palette[5, 4, 3], rc.Palette[0, 0, 0]);
  }

  #endregion
}
