using Critters.Gfx;
using Critters.Services;
using Critters.States.HeatFieldExperiment;
using Critters.UI;
using Critters.World;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Critters.States;

class HeatLampExperimentState : GameState
{
	#region Fields

	private Label _timeLabel;
	private Label _cameraLabel;
	private Label _mouseLabel;
	private Label _temperatureLabel;
	private Label _critterTempLabel;
	private Label _critterStaminaLabel;
	private Label _critterHealthLabel;

	private Camera _camera;
	private bool _isDraggingCamera = false;
	private Vector2 _cameraDelta = Vector2.Zero;
	private bool _cameraFastMove = false;

	/// <summary>
	/// Speed is measured in pixels per second.
	/// </summary>
	private float _cameraSpeed = 8 * 8; // 8 tiles per second

	private Vector2 _mousePosition = Vector2.Zero;
	private float _intensityFactor = 0.0f;

	private HeatField _heatField = new();
	private Critter _critter = new();

	#endregion

	#region Constructors

	public HeatLampExperimentState(IResourceManager resources, IRenderingContext rc)
		: base(resources, rc)
	{
		_camera = new Camera();

		var y = 0;

		_timeLabel = new Label(resources, rc, $"Time:0", new Vector2(0, y += 8), new RadialColor(5, 5, 5), new RadialColor(0, 0, 0));
		UI.Add(_timeLabel);

		_cameraLabel = new Label(resources, rc, $"Camera:({(int)_camera.Position.X},{(int)_camera.Position.Y})", new Vector2(0, y += 8), new RadialColor(5, 5, 5), new RadialColor(0, 0, 0));
		UI.Add(_cameraLabel);

		_mouseLabel = new Label(resources, rc, $"Mouse:(0,0)", new Vector2(0, y += 8), new RadialColor(5, 5, 5), new RadialColor(0, 0, 0));
		UI.Add(_mouseLabel);

		_temperatureLabel = new Label(resources, rc, $"Temp:0", new Vector2(0, y += 8), new RadialColor(5, 5, 5), new RadialColor(0, 0, 0));
		UI.Add(_temperatureLabel);

		_critterTempLabel = new Label(resources, rc, $"CritterTemp:0", new Vector2(0, y += 8), new RadialColor(5, 5, 5), new RadialColor(0, 0, 0));
		UI.Add(_critterTempLabel);

		_critterStaminaLabel = new Label(resources, rc, $"CritterStm:0", new Vector2(0, y += 8), new RadialColor(5, 5, 5), new RadialColor(0, 0, 0));
		UI.Add(_critterStaminaLabel);

		_critterHealthLabel = new Label(resources, rc, $"Health:0", new Vector2(0, y += 8), new RadialColor(5, 5, 5), new RadialColor(0, 0, 0));
		UI.Add(_critterHealthLabel);
	}

	#endregion

	#region Properties

	private Lamp MouseLamp
	{
		get
		{
			return new Lamp()
			{
				BaseIntensity = _intensityFactor,
				Position = _camera.Position + _mousePosition,
			};
		}
	}

	#endregion

	#region Methods

	public override void Load()
	{
		base.Load();
	}

	public override void Unload()
	{
		base.Unload();
	}

	public override void AcquireFocus()
	{
		base.AcquireFocus();

		if (Path.Exists("critter.json"))
		{
			// TODO: Button to reset position to origin?
			_critter = Critter.Load("critter.json");
			_critter.Position = Vector2.Zero;
		}
	}

	public override void LostFocus()
	{
		_critter.Save("critter.json");
		base.LostFocus();
	}

	public override void Render(GameTime gameTime)
	{
		RC.Clear();
		_camera.ViewportSize = RC.ViewportSize;

		_heatField.Render(RC, _camera, MouseLamp);
		_critter.Render(RC, _camera);

		base.Render(gameTime);
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);

		_camera.ScrollBy(_cameraDelta * (float)gameTime.ElapsedTime.TotalSeconds * _cameraSpeed * (_cameraFastMove ? 4 : 1));
		_cameraLabel.Text = StringProvider.From($"Camera:({(int)_camera.Position.X},{(int)_camera.Position.Y})");

		_heatField.Update(gameTime);

		var mouseLamp = MouseLamp;
		_heatField.Lamps.Add(mouseLamp);
		_critter.Update(gameTime, _heatField);
		_heatField.Lamps.Remove(mouseLamp);

		var temp = _heatField.CalculateTemperatureAtPoint(_camera.Position + _mousePosition) + MouseLamp.Temperature;
		_temperatureLabel.Text = StringProvider.From($"Temp:{(int)temp}");
		_timeLabel.Text = StringProvider.From($"Time:{_heatField.GetTimeString()}");
		_critterTempLabel.Text = StringProvider.From($"CritterTemp:{(int)_critter.InternalTemperature}");
		_critterStaminaLabel.Text = StringProvider.From($"CritterStm:{(int)_critter.Stamina}");
		_critterHealthLabel.Text = StringProvider.From($"Health:{(int)_critter.Health}");
	}

	public override bool KeyDown(KeyboardKeyEventArgs e)
	{
		_cameraFastMove = e.Shift;
		switch (e.Key)
		{
			case Keys.Escape:
				Leave();
				return true;
			case Keys.W:
				_cameraDelta = new Vector2(_cameraDelta.X, -1);
				return true;
			case Keys.S:
				_cameraDelta = new Vector2(_cameraDelta.X, 1);
				return true;
			case Keys.A:
				_cameraDelta = new Vector2(-1, _cameraDelta.Y);
				return true;
			case Keys.D:
				_cameraDelta = new Vector2(1, _cameraDelta.Y);
				return true;
		}
		return base.KeyDown(e);
	}

	public override bool KeyUp(KeyboardKeyEventArgs e)
	{
		_cameraFastMove = e.Shift;
		switch (e.Key)
		{
			case Keys.W:
			case Keys.S:
				_cameraDelta = new Vector2(_cameraDelta.X, 0);
				return true;
			case Keys.A:
			case Keys.D:
				_cameraDelta = new Vector2(0, _cameraDelta.Y);
				return true;
		}
		return base.KeyUp(e);
	}

	public override bool MouseMove(MouseMoveEventArgs e)
	{
		_mouseLabel.Text = StringProvider.From($"Mouse:({(int)e.Position.X},{(int)e.Position.Y})");

		if (_isDraggingCamera)
		{
			_camera.ScrollBy(-e.Delta);
		}

		_mousePosition = e.Position;
		return base.MouseMove(e);
	}

	public override bool MouseDown(MouseButtonEventArgs e)
	{
		return OnMouseButton(e);
	}

	public override bool MouseUp(MouseButtonEventArgs e)
	{
		return OnMouseButton(e);
	}

	private bool OnMouseButton(MouseButtonEventArgs e)
	{
		if (e.Button == MouseButton.Left && e.Action == InputAction.Release)
		{
			_heatField.Lamps.Add(new()
			{
				Position = _camera.Position + _mousePosition,
				BaseIntensity = _intensityFactor,
			});
			return true;
		}
		else if (e.Button == MouseButton.Middle)
		{
			_isDraggingCamera = e.IsPressed;
			return true;
		}
		return false;
	}

	public override bool MouseWheel(MouseWheelEventArgs e)
	{
		_intensityFactor += e.OffsetY * 0.01f;
		_intensityFactor = MathHelper.Clamp(_intensityFactor, 0.0f, 1.0f);
		// Console.WriteLine("_intensityFactor="+_intensityFactor);
		return base.MouseWheel(e);
	}

	#endregion
}