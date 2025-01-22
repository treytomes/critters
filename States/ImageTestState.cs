using Critters.Events;
using Critters.Gfx;
using Critters.IO;

namespace Critters.States;

class ImageTestState : GameState
{
  #region Fields

  private Image? _image;

  #endregion

  #region Methods

  public override void Load(ResourceManager resources, EventBus eventBus)
  {
		base.Load(resources, eventBus);

    _image = resources.Load<Image>("oem437_8.png");
  }

  public override void Render(RenderingContext rc, GameTime gameTime)
  {
		base.Render(rc, gameTime);
		
    rc.Fill(rc.Palette[1, 1, 0]);
    _image!.Render(rc, 100, 100);
  }

  #endregion
}
