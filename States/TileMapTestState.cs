using Critters.Events;
using Critters.Gfx;
using Critters.IO;
using Critters.UI;
using Critters.World;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;

namespace Critters.States;

class TileMapTestState : GameState
{
	#region Fields

	private Label _cameraLabel;
	private Label _mouseLabel;
	private List<UIElement> _ui = new List<UIElement>();
	private Camera _camera;

	#endregion

	#region Constructors

	public TileMapTestState()
	{
		_camera = new Camera();
		
		_cameraLabel = new Label($"Camera:({(int)_camera.Position.X},{ (int)_camera.Position.Y})", new Vector2(0, 0), Palette.GetIndex(5, 5, 5), Palette.GetIndex(0, 0, 0));
		_ui.Add(_cameraLabel);
		
		_mouseLabel = new Label($"Mouse:(0,0)", new Vector2(0, 8), Palette.GetIndex(5, 5, 5), Palette.GetIndex(0, 0, 0));
		_ui.Add(_mouseLabel);
	}

	public override void Load(ResourceManager resources, EventBus eventBus)
	{
		foreach (var ui in _ui)
		{
			ui.Load(resources, eventBus);
		}
		eventBus.Subscribe<MouseMoveEventArgs>(OnMouseMove);
	}

	public override void Unload(ResourceManager resources, EventBus eventBus)
	{
		foreach (var ui in _ui)
		{
			ui.Unload(resources, eventBus);
		}
		eventBus.Unsubscribe<MouseMoveEventArgs>(OnMouseMove);
	}

	public override void Render(RenderingContext rc, GameTime gameTime)
	{
		rc.Clear();
		foreach (var ui in _ui)
		{
			ui.Render(rc, gameTime);
		}
	}

	public override void Update(GameTime gameTime)
	{
		foreach (var ui in _ui)
		{
			ui.Update(gameTime);
		}
	}

	private void OnMouseMove(MouseMoveEventArgs e)
	{
		_mouseLabel.Text = $"Mouse:({(int)e.Position.X},{(int)e.Position.Y})";
	}

	#endregion
}