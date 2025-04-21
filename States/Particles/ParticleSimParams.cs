// States\Particles\ParticlesSimParams.cs

using OpenTK.Mathematics;

namespace Critters.States.Particles;

/// <summary>
/// Simulation parameters for the particle system
/// </summary>
class ParticleSimParams
{
	public Vector2 Gravity { get; set; } = Vector2.Zero;
	public Vector2 Bounds { get; set; } = new Vector2(800, 600);
	public Vector2 Attractor { get; set; } = Vector2.Zero;
	public float AttractorStrength { get; set; } = 0.0f;
	public float DampingFactor { get; set; } = 0.99f;
	public float BounceEnergyLoss { get; set; } = 0.8f;
	public float MaxAcceleration { get; set; } = 500.0f;
	public float MaxForce { get; set; } = 1000.0f;
	public float MinAttractorDistance { get; set; } = 10.0f;
}