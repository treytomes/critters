using Critters.Events;
using Critters.Gfx;
using Critters.IO;
using Critters.UI;
using Critters.World;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Critters.States;

class Lamp
{
	public Vector2 Position { get; set; }
	public float Intensity { get; set; }

	public void Render(RenderingContext rc)
	{
		rc.RenderOrderedDitheredCircle(Position, (int)(50 * Intensity), new RadialColor(1, 0, 0), Intensity);
		rc.RenderOrderedDitheredCircle(Position, (int)(40 * Intensity), new RadialColor(2, 1, 0), Intensity, new RadialColor(1, 0, 0));
		rc.RenderOrderedDitheredCircle(Position, (int)(30 * Intensity), new RadialColor(3, 2, 1), Intensity, new RadialColor(2, 1, 0));
		rc.RenderOrderedDitheredCircle(Position, (int)(20 * Intensity), new RadialColor(4, 3, 2), Intensity, new RadialColor(3, 2, 1));
		rc.RenderOrderedDitheredCircle(Position, (int)(10 * Intensity), new RadialColor(5, 4, 3), Intensity, new RadialColor(4, 3, 2));
	}
}

class HeatLampExperimentState : GameState
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

	private Vector2 _mousePosition = Vector2.Zero;
	private float _intensityFactor = 0.6f;

	private List<Lamp> _lamps = new();

	#endregion

	#region Constructors

	public HeatLampExperimentState()
		: base()
	{
		_camera = new Camera();
		
		_cameraLabel = new Label($"Camera:({(int)_camera.Position.X},{ (int)_camera.Position.Y})", new Vector2(0, 0), new RadialColor(5, 5, 5), new RadialColor(0, 0, 0));
		UI.Add(_cameraLabel);
		
		_mouseLabel = new Label($"Mouse:(0,0)", new Vector2(0, 8), new RadialColor(5, 5, 5), new RadialColor(0, 0, 0));
		UI.Add(_mouseLabel);
	}

	#endregion

	#region Methods
	
	public override void Load(ResourceManager resources, EventBus eventBus)
	{
		base.Load(resources, eventBus);
	}

	public override void Unload(ResourceManager resources, EventBus eventBus)
	{
		base.Unload(resources, eventBus);
	}

	public override void AcquireFocus(EventBus eventBus)
	{
		base.AcquireFocus(eventBus);

		eventBus.Subscribe<KeyEventArgs>(OnKey);
		eventBus.Subscribe<MouseMoveEventArgs>(OnMouseMove);
		eventBus.Subscribe<MouseButtonEventArgs>(OnMouseButton);
		eventBus.Subscribe<MouseWheelEventArgs>(OnMouseWheel);
	}

	public override void LostFocus(EventBus eventBus)
	{
		eventBus.Unsubscribe<KeyEventArgs>(OnKey);
		eventBus.Unsubscribe<MouseMoveEventArgs>(OnMouseMove);
		eventBus.Unsubscribe<MouseButtonEventArgs>(OnMouseButton);
		eventBus.Unsubscribe<MouseWheelEventArgs>(OnMouseWheel);

		base.LostFocus(eventBus);
	}

	public override void Render(RenderingContext rc, GameTime gameTime)
	{
		rc.Clear();
		_camera.ViewportSize = rc.ViewportSize;

		for (var dy = 0; dy < rc.ViewportSize.Y; dy++)
		{
			for (var dx = 0; dx < rc.ViewportSize.X; dx++)
			{
				var x = _camera.Position.X + dx;
				var y = _camera.Position.Y + dy;
				var pos = new Vector2(x, y);

				// Calculate the intensity contribution of each lamp.
				var intensity = 0.0f;
				foreach (var lamp in _lamps)
				{
					// var squaredDistance = Vector2.DistanceSquared(pos, lamp.Position);
					// intensity += lamp.Intensity * 10 / squaredDistance;
					var distance = Vector2.Distance(pos, lamp.Position);
					intensity += lamp.Intensity * 25 / distance;
				}

				// And also the mouse.
				{
					// var squaredDistance = Vector2.DistanceSquared(pos, _mousePosition);
					// intensity += _intensityFactor * 10 / squaredDistance;
					var distance = Vector2.Distance(pos, _mousePosition);
					intensity += _intensityFactor * 25 / distance;
				}

				// Calculate dithering probability.
				intensity = MathHelper.Clamp(intensity, 0.0f, 1.0f);
				if (Random.Shared.NextDouble() < intensity)
				{
					var colorIntensity = (byte)(intensity * 5.0f);
					switch (colorIntensity)
					{
						case 5:
							rc.SetPixel(pos, new RadialColor(5, 4, 3));
							break;
						case 4:
							rc.SetPixel(pos, new RadialColor(4, 3, 2));
							break;
						case 3:
							rc.SetPixel(pos, new RadialColor(3, 2, 1));
							break;
						case 2:
							rc.SetPixel(pos, new RadialColor(2, 1, 0));
							break;
						case 1:
							rc.SetPixel(pos, new RadialColor(1, 0, 0));
							break;
						default:
							rc.SetPixel(pos, new RadialColor(0, 0, 0));
							break;
					}
				}
			}
		}

		// rc.RenderOrderedDitheredCircle(_mousePosition, (int)(50 * _intensityFactor), new RadialColor(1, 0, 0), _intensityFactor);
		// rc.RenderOrderedDitheredCircle(_mousePosition, (int)(40 * _intensityFactor), new RadialColor(2, 1, 0), _intensityFactor, new RadialColor(1, 0, 0));
		// rc.RenderOrderedDitheredCircle(_mousePosition, (int)(30 * _intensityFactor), new RadialColor(3, 2, 1), _intensityFactor, new RadialColor(2, 1, 0));
		// rc.RenderOrderedDitheredCircle(_mousePosition, (int)(20 * _intensityFactor), new RadialColor(4, 3, 2), _intensityFactor, new RadialColor(3, 2, 1));
		// rc.RenderOrderedDitheredCircle(_mousePosition, (int)(10 * _intensityFactor), new RadialColor(5, 4, 3), _intensityFactor, new RadialColor(4, 3, 2));

		base.Render(rc, gameTime);
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);
		
		_camera.ScrollBy(_cameraDelta * (float)gameTime.ElapsedTime.TotalSeconds * _cameraSpeed * (_cameraFastMove ? 4 : 1));
		_cameraLabel.Text = $"Camera:({(int)_camera.Position.X},{ (int)_camera.Position.Y})";
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
		_mouseLabel.Text = $"Mouse:({(int)e.Position.X},{(int)e.Position.Y})";

		if (_isDraggingCamera)
		{
			_camera.ScrollBy(-e.Delta);
		}

		_mousePosition = e.Position;
	}

	private void OnMouseButton(MouseButtonEventArgs e)
	{
		if (e.Button == MouseButton.Left && e.Action == InputAction.Release)
		{
			_lamps.Add(new()
			{
				Position = _mousePosition,
				Intensity = _intensityFactor,
			});
		}
		if (e.Button == MouseButton.Middle)
		{
			_isDraggingCamera = e.IsPressed;
		}
	}

	private void OnMouseWheel(MouseWheelEventArgs e)
	{
		_intensityFactor += e.OffsetY * 0.01f;
		_intensityFactor = MathHelper.Clamp(_intensityFactor, 0.0f, 1.0f);
		Console.WriteLine("_intensityFactor="+_intensityFactor);
	}

	#endregion
}