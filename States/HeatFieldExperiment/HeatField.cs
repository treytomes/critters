using Critters.Gfx;
using Critters.World;
using OpenTK.Mathematics;

namespace Critters.States.HeatFieldExperiment;

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

	public void Render(IRenderingContext rc, Camera camera, Lamp cursor)
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
