using Critters.Gfx;
using Critters.World;
using OpenTK.Mathematics;

namespace Critters.States.HeatFieldExperiment;

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
