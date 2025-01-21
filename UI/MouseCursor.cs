using Critters.Events;
using Critters.Gfx;
using Critters.IO;
using Critters.States;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;

namespace Critters.UI;

class MouseCursor : IGameComponent
{
  #region Fields

  private Vector2 _position = Vector2.Zero;
  private Image? _image = null;

  #endregion

  #region Methods

  public void Load(ResourceManager resources, EventBus eventBus)
  {
    _image = resources.Load<Image>("mouse_cursor.png");
    _image.Recolor(0, 255);
    _image.Recolor(129, Palette.GetIndex(1, 1, 1));
    // Console.WriteLine("mouse colors: {0}", string.Join(',', _image.Data.Distinct().ToArray()));

    eventBus.Subscribe<MouseMoveEventArgs>(OnMouseMove);
  }

  public void Unload(ResourceManager resources, EventBus eventBus)
  {
    eventBus.Unsubscribe<MouseMoveEventArgs>(OnMouseMove);
  }

  public void Render(RenderingContext rc, GameTime gameTime)
  {
    if (_position.X < 0 || _position.Y < 0 || _position.X >= rc.Width || _position.Y >= rc.Height)
    {
      return;
    }
    _image?.Render(rc, (int)_position.X, (int)_position.Y);
  }

  public void Update(GameTime gameTime)
  {
  }

  private void OnMouseMove(MouseMoveEventArgs e)
  {
    _position = e.Position;
    // Console.WriteLine("Mouse position: {0}, delta: {1}", e.Position, e.Delta);
  }

  #endregion
}
