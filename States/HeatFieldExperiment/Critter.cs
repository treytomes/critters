using System.Runtime.CompilerServices;
using Critters.AI;
using Critters.Gfx;
using Critters.IO;
using Critters.World;
using Newtonsoft.Json;
using OpenTK.Mathematics;

namespace Critters.States.HeatFieldExperiment;

class Critter
{
	#region Constants
	
	// Movement directions: N, NE, E, SE, S, SW, W, NW
	private static readonly int[] DX = { 0, 0, 1, 1, 1, 0, -1, -1, -1 };
	private static readonly int[] DY = { 0, -1, -1, 0, 1, 1, 1, 0, -1 };

	private const float DEFAULT_RADIUS = 8;
	private const float DEFAULT_OPTIMAL_TEMPERATURE = 30;
	private const float DEFAULT_INTERNAL_TERMPERATURE = 20;

	private const float MAX_STAMINA = 100f;
	private const float MOVE_STAMINA_COST = 1f;

	/// <summary>
	/// It's not as big as it looks.
	/// </summary>
	private const float STAMINA_RECOVERY_RATE = 100f;
	
	private const int STATE_SIZE = 12;

	#endregion

	#region Fields

	private float radius = DEFAULT_RADIUS;
	private DeepQNetwork agent = new(stateSize: STATE_SIZE, actionSize: 9, hiddenSize: 64);
	private double optimalTemperature = DEFAULT_OPTIMAL_TEMPERATURE;
	
	private float stamina = 100f; // Starting with full stamina
	private bool movedLastTurn = false;

	private float health = 100f;
	private const float TEMPERATURE_DAMAGE_RATE = 10f;

	#endregion

	#region Constructors

	public Critter()
	{
	}

	private Critter(SerializableCritter other)
	{
		radius = other.Radius;
		agent = DeepQNetwork.Deserialize(other.Agent);
		Position = new Vector2(other.Position.X, other.Position.Y);
		optimalTemperature = other.OptimalTemperature;
		InternalTemperature = other.InternalTemperature;
		stamina = other.Stamina;
		health = other.Health;
	}

	#endregion

	#region Properties

	public Vector2 Position { get; set; } = Vector2.Zero;
	public double InternalTemperature { get; set; } = DEFAULT_INTERNAL_TERMPERATURE;

	public float Stamina 
	{ 
			get => stamina; 
			private set => stamina = Math.Clamp(value, 0, MAX_STAMINA); 
	}

	public float Health { get; private set; } = 100f;

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
			Position = new SerializableVector2(Position),
			InternalTemperature = InternalTemperature,
			OptimalTemperature = optimalTemperature,
			Stamina = Stamina,
			Health = health,
		};
	}

	private double[] GetStateVector(HeatField heatField)
	{
    // Update to include stamina - increasing state vector size
    var state = new double[STATE_SIZE];
    
    // Current cell temperature
    state[0] = heatField.CalculateTemperatureAtPoint(Position);
    
    // Surrounding cell temperatures
    var stateIndex = 1;
    for (var i = 0; i < 8; i++)
    {
        var nx = Position.X + DX[i];
        var ny = Position.Y + DY[i];
        state[stateIndex] = heatField.CalculateTemperatureAtPoint(new Vector2(nx * radius, ny * radius));
        stateIndex++;
    }
    
    // Internal temperature
    state[9] = InternalTemperature;
    
    // Add stamina as an input to the state
    state[10] = stamina / MAX_STAMINA; // Normalize to [0,1] range

		state[11] = health / 100f; // Normalized health
    
    return state;
	}

	// Reset method for when critter "dies"
	private void ResetCritter()
	{
			health = 100f;
			InternalTemperature = DEFAULT_INTERNAL_TERMPERATURE;
			stamina = MAX_STAMINA;
			// Optionally reset position or other stats
	}

	private double CalculateReward()
	{
    // Calculate temperature difference
    var temperatureDifference = Math.Abs(InternalTemperature - optimalTemperature);
    
    // STRONGER temperature reward function (steeper curve)
    var temperatureReward = 3.0 * Math.Exp(-temperatureDifference / 1.5);
    
    // Scale the temperature reward with MORE SIGNIFICANT penalties
    double scaledTemperatureReward;
    if (temperatureDifference < 1.0)
    {
        scaledTemperatureReward = 2.0 * temperatureReward;  // Doubled
    }
    else if (temperatureDifference < 3.0)
    {
        scaledTemperatureReward = 1.0 * temperatureReward;  // Doubled
    }
    else if (temperatureDifference < 5.0)
    {
        scaledTemperatureReward = -0.5 * temperatureReward;  // Increased penalty
    }
    else
    {
        scaledTemperatureReward = -2.5 * (1.0 - temperatureReward);  // Much stronger penalty
    }
    
    // Add EXTREME penalty for dangerous temperatures
    if (temperatureDifference > 10.0)
    {
        scaledTemperatureReward -= 5.0;  // Severe penalty for dangerous temperature differences
    }
    
    // Stamina management reward - REDUCED IMPACT
    double staminaReward = 0;
    
    // If temperature is good (close to optimal) and stamina is low, reward not moving
    if (temperatureDifference < 2.0 && stamina < MAX_STAMINA * 0.5 && !movedLastTurn) {
        staminaReward = 0.2;  // Reduced from 0.3
    }
    
    // Penalize moving when stamina is critically low
    if (stamina < MAX_STAMINA * 0.2 && movedLastTurn) {
        staminaReward = -0.3;  // Reduced from -0.4
    }
    
    // REDUCED bonus for having high stamina
    staminaReward += (stamina / MAX_STAMINA) * 0.05;  // Reduced from 0.1
    
    // Combine rewards with TEMPERATURE PRIORITY
    return (scaledTemperatureReward * 3.0) + staminaReward;  // Weight temperature 3x more
	}

	public void TrainStep(GameTime gameTime, HeatField heatField)
	{
			// Get current state
			var currentState = GetStateVector(heatField);
			
			// Select action
			var action = agent.SelectAction(currentState);
			
			var moved = false;
			
			// Apply movement only if we have enough stamina
			const int SPEED = 32;
			var dx = DX[action] * gameTime.ElapsedTime.TotalSeconds * SPEED;
			var dy = DY[action] * gameTime.ElapsedTime.TotalSeconds * SPEED;

			if ((dx != 0 || dy != 0) && stamina >= MOVE_STAMINA_COST)
			{
					var nx = Position.X + dx;
					var ny = Position.Y + dy;
					Position = new Vector2((float)nx, (float)ny);
					moved = true;
					
					// Reduce stamina when moving
					Stamina -= MOVE_STAMINA_COST;
			}
			
			// Recover stamina when not moving, if close to ideal temperature.
			if (!moved)
			{
					// Calculate temperature difference
					var temperatureDifference = Math.Abs(InternalTemperature - optimalTemperature);

					// Use an exponential decay function for recovery rate
					var temperatureReward = (float)Math.Exp(-temperatureDifference / 2.0);
					
					// Only recover stamina if temperature is reasonable
					if (temperatureDifference < 8.0) {
							Stamina += STAMINA_RECOVERY_RATE * temperatureReward * (float)gameTime.ElapsedTime.TotalSeconds;
							Console.WriteLine("Recovered stamina: {0}", STAMINA_RECOVERY_RATE * temperatureReward * (float)gameTime.ElapsedTime.TotalSeconds);
					} else {
							// Optional: Lose stamina if temperature is dangerous
							Stamina -= STAMINA_RECOVERY_RATE * 0.5f * (float)gameTime.ElapsedTime.TotalSeconds;
					}
			}
			
			movedLastTurn = moved;
			
			// Update internal temperature based on current position
			var environmentTemp = heatField.CalculateTemperatureAtPoint(Position);
			InternalTemperature = 0.9 * InternalTemperature + 0.1 * environmentTemp;

			// Get new state
			var nextState = GetStateVector(heatField);
			
			// Add to TrainStep method, after temperature update:
			// Extreme temperature causes damage (reflected in the state)
			if (Math.Abs(InternalTemperature - optimalTemperature) > 10.0)
			{
					health -= (float)gameTime.ElapsedTime.TotalSeconds * TEMPERATURE_DAMAGE_RATE;
					
					// Add a terminal state if health gets too low
					if (health <= 0)
					{
							// Store terminal state experience with large negative reward
							agent.StoreExperience(currentState, action, -20.0, nextState, true);
							ResetCritter(); // Reset the critter after "death"
							return;
					}
			}
			
			// Calculate reward
			var reward = CalculateReward();
			
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
		rc.RenderFilledCircle(Position - camera.Position, (int)radius, color);
	}

	#endregion
}
