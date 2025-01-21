using Critters.Events;
using Critters.Gfx;
using Critters.IO;

namespace Critters.States;

interface IGameComponent
{
  void Load(ResourceManager resources, EventBus eventBus);
  void Unload(ResourceManager resources, EventBus eventBus);
  void Render(RenderingContext rc, GameTime gameTime);
  void Update(GameTime gameTime);
}
