using Critters.AI;
using Critters.IO;

namespace Critters.States.HeatFieldExperiment;

class SerializableCritter
{
	public float Radius { get; set; }
	public required SerializableDeepQNetwork Agent { get; set; }
	public required SerializableVector2 Position { get; set; }
	public double InternalTemperature { get; set; }
	public double OptimalTemperature { get; set; }
	public float Stamina { get; set; }
	public float Health { get; set; }
}
