using Critters.Gfx;
using Critters.Services;

namespace Critters.States;

class ImageTestState : GameState
{
	#region Fields

	private Image? _image;

	#endregion

	#region Constructors

	public ImageTestState(IResourceManager resources, IEventBus eventBus, IRenderingContext rc)
		: base(resources, eventBus, rc)
	{
	}

	#endregion

	#region Methods

	public override void Load()
	{
		base.Load();

		_image = Resources.Load<Image>("oem437_8.png");
	}

	public override void Render(GameTime gameTime)
	{
		base.Render(gameTime);

		RC.Fill(RC.Palette[1, 1, 0]);
		_image!.Render(RC, 100, 100);
	}

	#endregion
}
