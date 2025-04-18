using Critters.Events;
using Critters.Gfx;
using Critters.IO;
using Critters.Services;
using Critters.UI;
using Critters.World;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Critters.States;

class SimplexNoiseState : GameState
{
	#region Fields

	private Label _cameraLabel;
	private Label _mouseLabel;

	private Camera _camera;
	private bool _isDraggingCamera = false;
	private Vector2 _cameraDelta = Vector2.Zero;
	private bool _cameraFastMove = false;

	/// <summary>
	/// Speed is measured in pixels per second.
	/// </summary>
	private float _cameraSpeed = 8 * 8; // 8 tiles per second

	#endregion

	#region Constructors

	public SimplexNoiseState()
		: base()
	{
		_camera = new Camera();

		_cameraLabel = new Label($"Camera:({(int)_camera.Position.X},{(int)_camera.Position.Y})", new Vector2(0, 0), new RadialColor(5, 5, 5), new RadialColor(0, 0, 0));
		UI.Add(_cameraLabel);

		_mouseLabel = new Label($"Mouse:(0,0)", new Vector2(0, 8), new RadialColor(5, 5, 5), new RadialColor(0, 0, 0));
		UI.Add(_mouseLabel);
	}

	#endregion

	#region Methods

	public override void Load(IResourceManager resources, IEventBus eventBus)
	{
		base.Load(resources, eventBus);
	}

	public override void Unload(IResourceManager resources, IEventBus eventBus)
	{
		base.Unload(resources, eventBus);
	}

	public override void AcquireFocus(IEventBus eventBus)
	{
		base.AcquireFocus(eventBus);

		eventBus.Subscribe<KeyEventArgs>(OnKey);
		eventBus.Subscribe<MouseMoveEventArgs>(OnMouseMove);
		eventBus.Subscribe<MouseButtonEventArgs>(OnMouseButton);
	}

	public override void LostFocus(IEventBus eventBus)
	{
		eventBus.Unsubscribe<KeyEventArgs>(OnKey);
		eventBus.Unsubscribe<MouseMoveEventArgs>(OnMouseMove);
		eventBus.Unsubscribe<MouseButtonEventArgs>(OnMouseButton);

		base.LostFocus(eventBus);
	}

	public override void Render(RenderingContext rc, GameTime gameTime)
	{
		rc.Clear();
		_camera.ViewportSize = rc.ViewportSize;

		for (var dy = 0; dy < _camera.ViewportSize.Y; dy++)
		{
			for (var dx = 0; dx < _camera.ViewportSize.X; dx++)
			{
				var x = _camera.Position.X + dx;
				var y = _camera.Position.Y + dy;

				// var noise = new SimplexNoise(42);

				// Get noise from [-1, 1].
				// Generate a single noise value at coordinates (x, y)
				// float value = noise.Noise(x, y);  // Value between -1 and 1

				// Generate fractal (multi-octave) noise for more natural-looking results
				// float value = noise.FractalNoise(x, y, scale: 1f, octaves: 6, persistence: 0.5f);
				// value *= noise.FractalNoise(x, y, scale: 0.1f, octaves: 3, persistence: 2f);
				var v1 = Noise.CalcPixel2D((int)x, (int)y, 0.001f);
				var v2 = Noise.CalcPixel2D((int)(x * 2), (int)(y * 4), 0.03f);
				var v3 = Noise.CalcPixel3D((int)(x * 2), (int)(y * 8), (int)(x * y), 0.03f);
				var value = (v1 * 0.8) + (v2 * 0.1) + (v3 * 0.1);
				value = MathHelper.Clamp(value / 255.0f, 0.0f, 1.0f);

				// Convert noise to [0, 1].
				// value = (value + 1) / 2;

				// Convert noise to [0, 5].
				value *= 5;

				// Convert noise to a byte.
				var c = (byte)value;

				var color = new RadialColor(c, c, c);
				rc.SetPixel(new Vector2(dx, dy), color);
			}
		}

		base.Render(rc, gameTime);
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);

		_camera.ScrollBy(_cameraDelta * (float)gameTime.ElapsedTime.TotalSeconds * _cameraSpeed * (_cameraFastMove ? 4 : 1));
		_cameraLabel.Text = StringProvider.From($"Camera:({(int)_camera.Position.X},{(int)_camera.Position.Y})");
	}

	private void OnKey(KeyEventArgs e)
	{
		if (e.IsPressed)
		{
			switch (e.Key)
			{
				case Keys.Escape:
					Leave();
					break;
				case Keys.W:
					_cameraDelta = new Vector2(_cameraDelta.X, -1);
					break;
				case Keys.S:
					_cameraDelta = new Vector2(_cameraDelta.X, 1);
					break;
				case Keys.A:
					_cameraDelta = new Vector2(-1, _cameraDelta.Y);
					break;
				case Keys.D:
					_cameraDelta = new Vector2(1, _cameraDelta.Y);
					break;
			}
		}
		else
		{
			switch (e.Key)
			{
				case Keys.W:
				case Keys.S:
					_cameraDelta = new Vector2(_cameraDelta.X, 0);
					break;
				case Keys.A:
				case Keys.D:
					_cameraDelta = new Vector2(0, _cameraDelta.Y);
					break;
			}
		}

		_cameraFastMove = e.Shift;
	}

	private void OnMouseMove(MouseMoveEventArgs e)
	{
		_mouseLabel.Text = StringProvider.From($"Mouse:({(int)e.Position.X},{(int)e.Position.Y})");

		if (_isDraggingCamera)
		{
			_camera.ScrollBy(-e.Delta);
		}
	}

	private void OnMouseButton(MouseButtonEventArgs e)
	{
		if (e.Button == MouseButton.Middle)
		{
			_isDraggingCamera = e.IsPressed;
		}
	}

	#endregion
}