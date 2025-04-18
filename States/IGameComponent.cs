using Critters.Gfx;
using Critters.Services;

namespace Critters.States;

interface IGameComponent
{
	void Load(IResourceManager resources, IEventBus eventBus);
	void Unload(IResourceManager resources, IEventBus eventBus);
	void Render(RenderingContext rc, GameTime gameTime);
	void Update(GameTime gameTime);
}
