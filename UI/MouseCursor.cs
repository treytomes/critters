using Critters.Gfx;
using Critters.Services;
using Critters.States;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;

namespace Critters.UI;

class MouseCursor : IGameComponent
{
	#region Fields

	private readonly IResourceManager _resources;
	private readonly IEventBus _eventBus;
	private readonly IRenderingContext _renderingContext;
	private Vector2 _position = Vector2.Zero;
	private Image? _image = null;

	#endregion

	#region Constructors

	public MouseCursor(IResourceManager resources, IEventBus eventBus, IRenderingContext renderingContext)
	{
		_resources = resources;
		_eventBus = eventBus;
		_renderingContext = renderingContext;
	}

	#endregion

	#region Methods

	public void Load()
	{
		_image = _resources.Load<Image>("mouse_cursor.png");
		_image.Recolor(0, 255);
		_image.Recolor(129, Palette.GetIndex(1, 1, 1));
		_eventBus.Subscribe<MouseMoveEventArgs>(OnMouseMove);
	}

	public void Unload()
	{
		_eventBus.Unsubscribe<MouseMoveEventArgs>(OnMouseMove);
	}

	public void Render(GameTime gameTime)
	{
		if (_position.X < 0 || _position.Y < 0 || _position.X >= _renderingContext.Width || _position.Y >= _renderingContext.Height)
		{
			return;
		}
		_image?.Render(_renderingContext, (int)_position.X, (int)_position.Y);
	}

	public void Update(GameTime gameTime)
	{
	}

	private void OnMouseMove(MouseMoveEventArgs e)
	{
		_position = e.Position;
	}

	#endregion
}
