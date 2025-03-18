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
	private const int MAX_COLOR_INTENSITY = 6;
	
	public static readonly List<RadialColor> Colors = [
			new RadialColor(0, 0, 0),
			new RadialColor(1, 0, 0),
			new RadialColor(2, 1, 0),
			new RadialColor(3, 2, 1),
			new RadialColor(4, 3, 2),
			new RadialColor(5, 4, 3)
	];

	public Vector2 Position { get; set; }
	public float Intensity { get; set; }

	public void Render(RenderingContext rc, Camera camera)
	{
		var pos = Position - camera.Position;
		rc.RenderOrderedDitheredCircle(pos, (int)((MAX_COLOR_INTENSITY - 1) * 10 * Intensity), Colors[MAX_COLOR_INTENSITY - 5], Intensity);
		rc.RenderOrderedDitheredCircle(pos, (int)((MAX_COLOR_INTENSITY - 2) * 10 * Intensity), Colors[MAX_COLOR_INTENSITY - 4], Intensity, Colors[MAX_COLOR_INTENSITY - 5]);
		rc.RenderOrderedDitheredCircle(pos, (int)((MAX_COLOR_INTENSITY - 3) * 10 * Intensity), Colors[MAX_COLOR_INTENSITY - 3], Intensity, Colors[MAX_COLOR_INTENSITY - 4]);
		rc.RenderOrderedDitheredCircle(pos, (int)((MAX_COLOR_INTENSITY - 4) * 10 * Intensity), Colors[MAX_COLOR_INTENSITY - 2], Intensity, Colors[MAX_COLOR_INTENSITY - 3]);
		rc.RenderOrderedDitheredCircle(pos, (int)((MAX_COLOR_INTENSITY - 5) * 10 * Intensity), Colors[MAX_COLOR_INTENSITY - 1], Intensity, Colors[MAX_COLOR_INTENSITY - 2]);
	}

	/// <summary>
	/// </summary>
	/// <param name="intensity">A value in [0, 1].</param>
	/// <returns></returns>
	public static int IntensityToColorIndex(float intensity)
	{
		return (int)(intensity * 5.0f);
	}

	/// <summary>
	/// </summary>
	/// <param name="intensity">A value in [0, 1].</param>
	/// <returns></returns>
	public static RadialColor IntensityToColor(float intensity)
	{
		return Colors[IntensityToColorIndex(intensity)];
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

	/// <summary>
	/// Bayer 4x4 dithering matrix
	/// </summary>
	private static readonly int[,] bayerMatrix = new int[,] {
			{  0, 12,  3, 15 },
			{  8,  4, 11,  7 },
			{  2, 14,  1, 13 },
			{ 10,  6,  9,  5 }
	};

	public override void Render(RenderingContext rc, GameTime gameTime)
	{
		rc.Clear();
		_camera.ViewportSize = rc.ViewportSize;

		// foreach (var lamp in _lamps)
		// {
		// 	lamp.Render(rc, _camera);
		// }
		// new Lamp() { Position = _camera.Position + _mousePosition, Intensity = _intensityFactor }.Render(rc, _camera);

		const float BASE_INTENSITY = 100.0f;
		for (var dy = 0; dy < rc.ViewportSize.Y; dy++)
		{
			for (var dx = 0; dx < rc.ViewportSize.X; dx++)
			{
				var ppos = new Vector2(dx, dy);
				var pos = _camera.Position + ppos;

				// Calculate the intensity contribution of each lamp.
				var intensity = 0.0f;
				foreach (var lamp in _lamps)
				{
					// var squaredDistance = Vector2.DistanceSquared(pos, lamp.Position);
					// intensity += lamp.Intensity * BASE_INTENSITY / squaredDistance;
					var distance = Vector2.Distance(pos, lamp.Position);
					intensity += lamp.Intensity * BASE_INTENSITY / distance;
				}

				// And also the mouse.
				{
					// var squaredDistance = Vector2.DistanceSquared(pos, _mousePosition);
					// intensity += _intensityFactor * BASE_INTENSITY / squaredDistance;
					var distance = Vector2.Distance(pos, _camera.Position + _mousePosition);
					intensity += _intensityFactor * BASE_INTENSITY / distance;
				}

				// Calculate dithering probability.
				intensity = MathHelper.Clamp(intensity, 0.0f, 1.0f);

				// Get the appropriate threshold from the Bayer matrix (0-15, normalized to 0.0-1.0)
				// var bayerX = Math.Abs((int)pos.X) % 4;
				// var bayerY = Math.Abs((int)pos.Y) % 4;
				var bayerX = MathHelper.Modulus(Math.Abs((int)pos.X), 4);
				var bayerY = MathHelper.Modulus(Math.Abs((int)pos.Y), 4);
				var threshold = bayerMatrix[bayerY, bayerX] / 16.0f;
				// if (Random.Shared.NextDouble() < intensity)
				var colorIndex = Lamp.IntensityToColorIndex(intensity);
				if (intensity >= threshold)
				{
					var color = Lamp.Colors[colorIndex];
					if (color.Index != 0)
					{
						rc.SetPixel(ppos, color);
					}
				}
				else
				{
					colorIndex -= 1;
					if (colorIndex > 0)
					{
						var color = Lamp.Colors[colorIndex];
						if (color.Index != 0)
						{
							rc.SetPixel(ppos, color);
						}
					}
				}
			}
		}

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
				Position = _camera.Position + _mousePosition,
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