using Critters.Gfx;

namespace Critters.States;

class PatternTestState : GameState
{
  public override void Render(RenderingContext rc, GameTime gameTime)
  {
    for (int y = 0; y < rc.Height; y++)
    {
      for (int x = 0; x < rc.Width; x++)
      {
        var r = (byte)((x | y) % 6);
        var g = (byte)((x ^ y) % 6);
        var b = (byte)((x & y) % 6);
        rc.SetPixel(x, y, rc.Palette[r, g, b]);
      }
    }
  }
}
