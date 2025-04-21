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
	private readonly IRenderingContext _renderingContext;
	private Vector2 _position = Vector2.Zero;
	private Image? _image = null;

	#endregion

	#region Constructors

	public MouseCursor(IResourceManager resources, IRenderingContext renderingContext)
	{
		_resources = resources;
		_renderingContext = renderingContext;
	}

	#endregion

	#region Methods

	public void Load()
	{
		_image = _resources.Load<Image>("mouse_cursor.png");
		_image.Recolor(0, 255);
		_image.Recolor(129, Palette.GetIndex(1, 1, 1));
	}

	public void Unload()
	{
	}

	public void Render(GameTime gameTime)
	{
		if (_position.X < 0 || _position.Y < 0 || _position.X >= _renderingContext.Width || _position.Y >= _renderingContext.Height)
		{
			return;
		}
		_image?.Render(_renderingContext, (int)_position.X, (int)_position.Y);
		Console.WriteLine("Mouse drawn!");
	}

	public void Update(GameTime gameTime)
	{
	}

	public bool MouseMove(MouseMoveEventArgs e)
	{
		_position = e.Position;
		return false;
	}

	#endregion
}
