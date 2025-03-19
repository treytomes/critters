using Critters.AI;
using Critters.Events;
using Critters.Gfx;
using Critters.IO;
using Critters.UI;
using Critters.World;
using Newtonsoft.Json;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Critters.States;

class SerializableCritter
{
	public float Radius { get; set; }
	public required SerializableDeepQNetwork Agent { get; set; }
	public required SerializableVector2 Position { get; set; }
	public double InternalTemperature { get; set; }
	public double OptimalTemperature { get; set; }
}

class Critter
{
	#region Constants
	
	// Movement directions: N, NE, E, SE, S, SW, W, NW
	private static readonly int[] DX = { 0, 1, 1, 1, 0, -1, -1, -1 };
	private static readonly int[] DY = { -1, -1, 0, 1, 1, 1, 0, -1 };

	private const float DEFAULT_RADOIS = 8;
	private const float DEFAULT_OPTIMAL_TEMPERATURE = 30;
	private const float DEFAULT_INTERNAL_TERMPERATURE = 20;

	#endregion

	#region Fields

	private float radius = DEFAULT_RADOIS;
	private DeepQNetwork agent = new(stateSize: 10, actionSize: 8, hiddenSize: 64);
	private Vector2 position = Vector2.Zero;
	private double optimalTemperature = DEFAULT_OPTIMAL_TEMPERATURE;
	
	#endregion

	#region Constructors

	public Critter()
	{
	}

	private Critter(SerializableCritter other)
	{
		radius = other.Radius;
		agent = DeepQNetwork.Deserialize(other.Agent);
		position = new Vector2(other.Position.X, other.Position.Y);
		optimalTemperature = other.OptimalTemperature;
		InternalTemperature = other.InternalTemperature;
	}

	#endregion

	#region Properties

	public double InternalTemperature { get; set; } = DEFAULT_INTERNAL_TERMPERATURE;

	#endregion

	#region Methods

	public static Critter Load(string path)
	{
		var json = File.ReadAllText(path);
		var info = JsonConvert.DeserializeObject<SerializableCritter>(json);
		return new Critter(info!);
	}

	public void Save(string path)
	{
		var json = JsonConvert.SerializeObject(Serialize());
		File.WriteAllText(path, json);
	}

	public static Critter Deserialize(SerializableCritter other)
	{
		return new Critter(other);
	}

	public SerializableCritter Serialize()
	{
		return new SerializableCritter()
		{
			Agent = agent.Serialize(),
			Position = new SerializableVector2(position),
			InternalTemperature = InternalTemperature,
			OptimalTemperature = optimalTemperature,
		};
	}

	private double[] GetStateVector(HeatField heatField)
	{
		var state = new double[10];
		
		// Current cell temperature
		state[0] = heatField.CalculateTemperatureAtPoint(position);
		
		// Surrounding cell temperatures
		var stateIndex = 1;
		for (var i = 0; i < 8; i++)
		{
			var nx = position.X + DX[i];
			var ny = position.Y + DY[i];
			state[stateIndex] = heatField.CalculateTemperatureAtPoint(new Vector2(nx * radius, ny * radius));
			stateIndex++;
		}
		
		// Internal temperature
		state[9] = InternalTemperature;
		
		return state;
	}

	private double CalculateReward()
	{
		// Reward based on how close internal temperature is to optimal
		var temperatureDifference = Math.Abs(InternalTemperature - optimalTemperature);
		
		// Higher reward for closer to optimal temperature
		if (temperatureDifference < 0)
		{
			return 1.0;
		}
		else if (temperatureDifference < 2)
		{
			return 0.5;
		}
		else if (temperatureDifference < 3)
		{
			return 0.1;
		}
		else if (temperatureDifference > 5)
		{
			return -1.0;  // Significant penalty for being too far off
		}
		else
		{
			return -0.1;  // Small penalty otherwise
		}
	}

	public void TrainStep(GameTime gameTime, HeatField heatField)
	{
		// Get current state
		var currentState = GetStateVector(heatField);
		
		// Select action
		var action = agent.SelectAction(currentState);
		
		// Apply movement
		var nx = position.X + DX[action] * gameTime.ElapsedTime.TotalSeconds * 32;
		var ny = position.Y + DY[action] * gameTime.ElapsedTime.TotalSeconds * 32;
		
		var moved = false;
		if (nx != 0 || ny != 0)
		{
			position = new Vector2((float)nx, (float)ny);
			moved = true;
		}
		
		// Update internal temperature based on current position
		var environmentTemp = heatField.CalculateTemperatureAtPoint(position);
		InternalTemperature = 0.9 * InternalTemperature + 0.1 * environmentTemp;
		
		// Get new state
		var nextState = GetStateVector(heatField);
		
		// Calculate reward
		var reward = CalculateReward();
		
		// Small penalty for not moving
		if (!moved)
		{
			reward -= 0.2;
		}
		else
		{
			// TODO: Should there be a penalty for moving instead?
		}
		
		// Store experience
		agent.StoreExperience(currentState, action, reward, nextState, false);
		
		// Train the agent
		agent.Train();
	}

	public void Update(GameTime gameTime, HeatField heatField)
	{
		TrainStep(gameTime, heatField);
	}

	public void Render(RenderingContext rc, Camera camera)
	{
		var color = new RadialColor(0, 5, 0);
		rc.RenderFilledCircle(position - camera.Position, (int)radius, color);
	}

	#endregion
}

class Lamp
{
	#region Constants

	private const float INTENSITY_FACTOR = 60;
	private const int MAX_COLOR_INTENSITY = 6;
	private const float BASE_INTENSITY = MAX_COLOR_INTENSITY * INTENSITY_FACTOR;
	private const float DECAY_RATE = 0.001f; // How quickly the candle burns out
	private const float BASE_FLICKER_MAGNITUDE = 0.1f; // Base flicker amount
	private const float FLICKER_SPEED = 3.0f; // Speed of the flickering effect
	
	// These constants control how flickering increases as intensity decreases
	private const float MIN_INTENSITY_THRESHOLD = 0.2f; // Below this intensity, flickering is maximized
	private const float MAX_INTENSITY_THRESHOLD = 0.5f; // Above this intensity, flickering is minimized
	private const float MIN_FLICKER_MULTIPLIER = 0.1f; // Minimal flickering for bright lamps
	private const float MAX_FLICKER_MULTIPLIER = 0.3f; // Maximum flickering for dim lamps

	#endregion

	#region Fields

	private float _flickerOffset = 0.0f;
	private float _timeAccumulator = 0.0f;

	public static readonly List<RadialColor> Colors = [
			new RadialColor(0, 0, 0),
			new RadialColor(1, 0, 0),
			new RadialColor(2, 1, 0),
			new RadialColor(3, 2, 1),
			new RadialColor(4, 3, 2),
			new RadialColor(5, 4, 3)
	];

	#endregion

	#region Properties

	public Vector2 Position { get; set; }
	public float BaseIntensity { get; set; } // Renamed from Intensity
	
	// New property for the actual intensity with flickering effect
	public float Intensity 
	{ 
			get => Math.Max(0, BaseIntensity + _flickerOffset); 
	}

	public bool IsAlive
	{
		get
		{
			return BaseIntensity > 0;
		}
	}

	public float Temperature
	{
		get
		{
			return BASE_INTENSITY * BASE_INTENSITY * Intensity;
		}
	}

	#endregion

	#region Methods

	public void Render(RenderingContext rc, Camera camera)
	{
		var pos = Position - camera.Position;
		rc.RenderOrderedDitheredCircle(pos, (int)((MAX_COLOR_INTENSITY - 1) * INTENSITY_FACTOR * Intensity), Colors[MAX_COLOR_INTENSITY - 5], Intensity);
		rc.RenderOrderedDitheredCircle(pos, (int)((MAX_COLOR_INTENSITY - 2) * INTENSITY_FACTOR * Intensity), Colors[MAX_COLOR_INTENSITY - 4], Intensity, Colors[MAX_COLOR_INTENSITY - 5]);
		rc.RenderOrderedDitheredCircle(pos, (int)((MAX_COLOR_INTENSITY - 3) * INTENSITY_FACTOR * Intensity), Colors[MAX_COLOR_INTENSITY - 3], Intensity, Colors[MAX_COLOR_INTENSITY - 4]);
		rc.RenderOrderedDitheredCircle(pos, (int)((MAX_COLOR_INTENSITY - 4) * INTENSITY_FACTOR * Intensity), Colors[MAX_COLOR_INTENSITY - 2], Intensity, Colors[MAX_COLOR_INTENSITY - 3]);
		rc.RenderOrderedDitheredCircle(pos, (int)((MAX_COLOR_INTENSITY - 5) * INTENSITY_FACTOR * Intensity), Colors[MAX_COLOR_INTENSITY - 1], Intensity, Colors[MAX_COLOR_INTENSITY - 2]);
	}

	public void Update(GameTime gameTime)
	{
		var deltaTime = (float)gameTime.ElapsedTime.TotalSeconds;
        
		// Update the accumulator
		_timeAccumulator += deltaTime;
		
		// Calculate how much to flicker based on current intensity
		var flickerMultiplier = CalculateFlickerMultiplier();
		
		// Generate the basic flicker effect using multiple sine waves
		var basicFlicker = (float)(
			(Math.Sin(_timeAccumulator * FLICKER_SPEED) * 0.5) + 
			(Math.Sin(_timeAccumulator * FLICKER_SPEED * 2.3) * 0.3) + 
			(Math.Sin(_timeAccumulator * FLICKER_SPEED * 5.7) * 0.2)
		) * BASE_FLICKER_MAGNITUDE * BaseIntensity;
		
		// Apply the flicker multiplier to make dim lamps flicker more
		_flickerOffset = basicFlicker * flickerMultiplier;
		
		// Occasionally add a random flicker (more common for dimmer lamps)
		var randomFlickerChance = 0.02f + (0.08f * (flickerMultiplier / MAX_FLICKER_MULTIPLIER));
		if (Random.Shared.NextDouble() < randomFlickerChance)
		{
			// The random flicker also scales with the multiplier
			_flickerOffset -= (float)Random.Shared.NextDouble() * BASE_FLICKER_MAGNITUDE * 0.7f * BaseIntensity * flickerMultiplier;
		}
		
		// Complex decay model:
    // 1. Normal decay rate in the middle range
    // 2. Slower at the beginning (new candle with solid wax)
    // 3. Faster at the end (wick burning out)
    float decayModifier;
    
    if (BaseIntensity > 0.7f) {
        // Slow initial burn as the candle gets going
        decayModifier = 0.7f;
    } else if (BaseIntensity < 0.2f) {
        // Accelerated final burn as the wick is consumed
        decayModifier = 1.5f + ((0.2f - BaseIntensity) * 5f); // Increasingly faster
    } else {
        // Normal burn rate in the middle
        decayModifier = 1.0f;
    }
    
    // Apply the modified decay rate
    BaseIntensity = Math.Max(0, BaseIntensity - DECAY_RATE * decayModifier * deltaTime);
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

	/// <summary>
	/// Calculate how much flickering to apply based on current intensity.
	/// </summary>
	private float CalculateFlickerMultiplier()
	{
			// For high intensity, use minimal flickering
		if (BaseIntensity >= MAX_INTENSITY_THRESHOLD)
		{
			return MIN_FLICKER_MULTIPLIER;
		}
				
		// For low intensity, use maximum flickering
		if (BaseIntensity <= MIN_INTENSITY_THRESHOLD)
		{
			return MAX_FLICKER_MULTIPLIER;
		}
				
		// For intermediate intensities, scale linearly
		float normalizedIntensity = (BaseIntensity - MIN_INTENSITY_THRESHOLD) / (MAX_INTENSITY_THRESHOLD - MIN_INTENSITY_THRESHOLD);

		// Invert the normalized intensity (lower intensity = higher flicker)
		float invertedNormalized = 1.0f - normalizedIntensity;
		
		// Scale between min and max flicker multipliers
		return MIN_FLICKER_MULTIPLIER + invertedNormalized * (MAX_FLICKER_MULTIPLIER - MIN_FLICKER_MULTIPLIER);
	}

	#endregion
}

class HeatField
{
	#region Constants

	private const float DEFAULT_AMBIENT_TEMPERATURE = 10.0f;
	private const float DAY_LENGTH_MINUTES = 10.0f; // 10 minutes = 1 full day
	private const float DAY_NIGHT_TEMPERATURE_VARIATION = 15.0f; // Temperature difference between day and night
	private const float BASE_DAYTIME_TEMPERATURE = DEFAULT_AMBIENT_TEMPERATURE + 10.0f; // Midday temperature
	private const float DAWN_DUSK_HOUR = 6.0f; // Hours from midnight to dawn/from dusk to midnight

	#endregion
	
	#region Fields

	/// <summary>
	/// Bayer 4x4 dithering matrix
	/// </summary>
	private static readonly int[,] bayerMatrix = new int[,] {
		{  0, 12,  3, 15 },
		{  8,  4, 11,  7 },
		{  2, 14,  1, 13 },
		{ 10,  6,  9,  5 }
	};

	// Track the current time of day
	private float _currentDayTime = 12.0f; // Start at noon
	private float _dayTimeTotalSeconds = DAY_LENGTH_MINUTES * 60.0f; // Total seconds in a day

	#endregion

	#region Constructors

	public HeatField()
	{
		Lamps = new();
	}

	#endregion

	#region Properties

	public float AmbientTemperature { get; private set; } = DEFAULT_AMBIENT_TEMPERATURE;

	public List<Lamp> Lamps { get; }
	
	// Current hour of the day (0-23.999)
	public float CurrentHour => _currentDayTime;
	
	// Returns whether it's currently daytime (between sunrise and sunset)
	public bool IsDaytime => CurrentHour > DAWN_DUSK_HOUR && CurrentHour < (24.0f - DAWN_DUSK_HOUR);

	#endregion

	#region Methods

	public void Render(RenderingContext rc, Camera camera, Lamp cursor)
	{
		// foreach (var lamp in _lamps)
		// {
		// 	lamp.Render(rc, _camera);
		// }
		// new Lamp() { Position = _camera.Position + _mousePosition, Intensity = _intensityFactor }.Render(rc, _camera);

		for (var dy = 0; dy < rc.ViewportSize.Y; dy++)
		{
			for (var dx = 0; dx < rc.ViewportSize.X; dx++)
			{
				var ppos = new Vector2(dx, dy);
				var pos = camera.Position + ppos;

				Lamps.Add(cursor);
				var intensity = CalculateTemperatureAtPoint(pos) / 100.0f;
				Lamps.Remove(cursor);

				// Calculate dithering probability.
				intensity = MathHelper.Clamp(intensity, 0.0f, 1.0f);

				// Get the appropriate threshold from the Bayer matrix (0-15, normalized to 0.0-1.0)
				var bayerX = MathHelper.Modulus(Math.Abs((int)pos.X), 4);
				var bayerY = MathHelper.Modulus(Math.Abs((int)pos.Y), 4);
				var threshold = bayerMatrix[bayerY, bayerX] / 16.0f;
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
	}

	public void Update(GameTime gameTime)
	{
		// Update all lamps
		foreach (var lamp in Lamps)
		{
			lamp.Update(gameTime);
		}

		// Remove expired lamps
		var n = 0;
		while (n < Lamps.Count)
		{
			if (!Lamps[n].IsAlive)
			{
				Lamps.RemoveAt(n);
			}
			else
			{
				n++;
			}
		}
		
		// Update the day/night cycle
		UpdateDayNightCycle(gameTime);
	}
	
	private void UpdateDayNightCycle(GameTime gameTime)
	{
		// Advance the time of day
		float deltaSeconds = (float)gameTime.ElapsedTime.TotalSeconds;
		_currentDayTime = (_currentDayTime + (24.0f * deltaSeconds / _dayTimeTotalSeconds)) % 24.0f;
		
		// Calculate ambient temperature based on time of day
		// Using a cosine curve to create a smooth transition
		
		// Map the 24-hour day to a 0-2π value for the cosine function
		// Offset by π so that midnight is the coldest point
		float timeRadians = (((_currentDayTime / 24.0f) * MathF.PI * 2.0f) + MathF.PI) % (MathF.PI * 2.0f);
		
		// Calculate temperature: BASE_TEMP + variation * normalized cosine (-1 to 1)
		// Cosine gives -1 at midnight, +1 at noon
		float temperatureVariation = MathF.Cos(timeRadians) * (DAY_NIGHT_TEMPERATURE_VARIATION / 2.0f);
		
		// Add extra smoothing for dawn and dusk transitions
		float smoothTransitionFactor = 1.0f;
		if (_currentDayTime < DAWN_DUSK_HOUR) // Pre-dawn
		{
			float transitionProgress = _currentDayTime / DAWN_DUSK_HOUR;
			smoothTransitionFactor = 0.8f + (transitionProgress * 0.2f); // Slower warming in early morning
		}
		else if (_currentDayTime > (24.0f - DAWN_DUSK_HOUR)) // Post-dusk
		{
			float transitionProgress = (24.0f - _currentDayTime) / DAWN_DUSK_HOUR;
			smoothTransitionFactor = 0.8f + (transitionProgress * 0.2f); // Slower cooling in early evening
		}
		
		temperatureVariation *= smoothTransitionFactor;
		
		// Set the new ambient temperature
		AmbientTemperature = BASE_DAYTIME_TEMPERATURE + temperatureVariation;
	}

	public float CalculateTemperatureAtPoint(Vector2 position)
	{
		var totalTemperature = 0f;
		
		foreach (var lamp in Lamps)
		{
			// Skip lamps that aren't contributing heat
			if (!lamp.IsAlive || lamp.Intensity <= 0)
			{
				continue;
			}
					
			// Calculate distance from point to heat source
			var distance = Vector2.Distance(position, lamp.Position);
			
			// Avoid division by zero and limit maximum heat at source
			var effectiveDistance = Math.Max(1.0f, distance);
			
			// Calculate heat contribution using inverse-square law
			// (heat decreases with the square of the distance)
			var heatContribution = lamp.Temperature / (effectiveDistance * effectiveDistance);
			
			// // Optional: Apply a cutoff distance beyond which heat is negligible
			// var cutoffDistance = 100f; // Adjust based on your scale
			// if (distance > cutoffDistance)
			// {
			// 		var falloffFactor = Math.Max(0, 1 - ((distance - cutoffDistance) / cutoffDistance));
			// 		heatContribution *= falloffFactor * falloffFactor; // Smooth falloff
			// }
			
			totalTemperature += heatContribution;
		}
		
		// Add ambient temperature
		totalTemperature += AmbientTemperature;
		
		return totalTemperature;
	}
	
	/// <summary>
	/// Sets the current time of day
	/// </summary>
	/// <param name="hour">Hour value (0-23.999)</param>
	public void SetTimeOfDay(float hour)
	{
		_currentDayTime = MathHelper.Modulus(hour, 24.0f);
		// Update temperature immediately
		UpdateDayNightCycle(new GameTime()); 
	}
	
	/// <summary>
	/// Returns a string representation of the current time of day
	/// </summary>
	public string GetTimeString()
	{
		var hours = (int)_currentDayTime;
		var minutes = (int)((_currentDayTime - hours) * 60);
		var amPm = hours < 12 ? "AM" : "PM";
		hours = hours % 12;
		if (hours == 0)
		{
			hours = 12; // 12-hour format
		}
		return $"{hours:D}:{minutes:D2}{amPm}";
	}
	
	/// <summary>
	/// Adjusts the length of the day cycle
	/// </summary>
	/// <param name="dayLengthMinutes">Number of real-time minutes per in-game day</param>
	public void SetDayLength(float dayLengthMinutes)
	{
		_dayTimeTotalSeconds = dayLengthMinutes * 60.0f;
	}
	
	#endregion
}

class HeatLampExperimentState : GameState
{
	#region Fields

	private Label _timeLabel;
	private Label _cameraLabel;
	private Label _mouseLabel;
	private Label _temperatureLabel;
	private Label _critterTempLabel;

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

	public HeatLampExperimentState()
		: base()
	{
		_camera = new Camera();

		var y = 0;
		
		_timeLabel = new Label($"Time:0", new Vector2(0, y += 8), new RadialColor(5, 5, 5), new RadialColor(0, 0, 0));
		UI.Add(_timeLabel);
		
		_cameraLabel = new Label($"Camera:({(int)_camera.Position.X},{ (int)_camera.Position.Y})", new Vector2(0, y += 8), new RadialColor(5, 5, 5), new RadialColor(0, 0, 0));
		UI.Add(_cameraLabel);
		
		_mouseLabel = new Label($"Mouse:(0,0)", new Vector2(0, y += 8), new RadialColor(5, 5, 5), new RadialColor(0, 0, 0));
		UI.Add(_mouseLabel);
		
		_temperatureLabel = new Label($"Temp:0", new Vector2(0, y += 8), new RadialColor(5, 5, 5), new RadialColor(0, 0, 0));
		UI.Add(_temperatureLabel);
		
		_critterTempLabel = new Label($"CritterTemp:0", new Vector2(0, y += 8), new RadialColor(5, 5, 5), new RadialColor(0, 0, 0));
		UI.Add(_critterTempLabel);
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

		if (Path.Exists("critter.json"))
		{
			_critter = Critter.Load("critter.json");
		}
	}

	public override void LostFocus(EventBus eventBus)
	{
		_critter.Save("critter.json");

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

		_heatField.Render(rc, _camera, MouseLamp);
		_critter.Render(rc, _camera);

		base.Render(rc, gameTime);
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);
		
		_camera.ScrollBy(_cameraDelta * (float)gameTime.ElapsedTime.TotalSeconds * _cameraSpeed * (_cameraFastMove ? 4 : 1));
		_cameraLabel.Text = $"Camera:({(int)_camera.Position.X},{ (int)_camera.Position.Y})";

		_heatField.Update(gameTime);

		var mouseLamp = MouseLamp;
		_heatField.Lamps.Add(mouseLamp);
		_critter.Update(gameTime, _heatField);
		_heatField.Lamps.Remove(mouseLamp);

		var temp = _heatField.CalculateTemperatureAtPoint(_camera.Position + _mousePosition) + MouseLamp.Temperature;
		_temperatureLabel.Text = $"Temp:{(int)temp}";
		_timeLabel.Text = $"Time:{_heatField.GetTimeString()}";
		_critterTempLabel.Text = $"CritterTemp:{(int)_critter.InternalTemperature}";
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
			_heatField.Lamps.Add(new()
			{
				Position = _camera.Position + _mousePosition,
				BaseIntensity = _intensityFactor,
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