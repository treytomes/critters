using System.Runtime.InteropServices;
using Critters.Events;
using Critters.Gfx;
using Critters.IO;
using Critters.UI;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Critters.States;

[StructLayout(LayoutKind.Sequential)]
struct ParticleData
{
    public Vector2 Position;     // 8 bytes
    public Vector2 Velocity;     // 8 bytes
    public Vector2 Acceleration; // 8 bytes

		// 2 floats of padding to align color to a 16-byte boundary.
    float padding0;
    float padding1;

    public Vector4 Color;        // 16 bytes
    public float Lifetime;       // 4 bytes
    public float Size;           // 4 bytes

		// Add explicit padding if needed
    float padding2;
    float padding3;
}

class ParticlesSim : IDisposable
{
	#region Constants
	
	private const string PARTICLE_SHADER = @"
#version 430 core

struct Particle {
	vec2 position;
	vec2 velocity;
	vec2 acceleration;

	// 2 floats of padding to align color to a 16-byte boundary.
	float padding0;
	float padding1;

	vec4 color;
	float lifetime;
	float size;

	// 2 floats of padding
	float padding2;
	float padding3;
};

// Define work group size - best to use multiples of 32 or 64 for particles
layout(local_size_x = 128, local_size_y = 1, local_size_z = 1) in;

// Input buffer containing current particle states
layout(std430, binding = 0) buffer ParticleBuffer {
	Particle particles[];
};

// Output buffer for next particle states
layout(std430, binding = 1) buffer NextParticleBuffer {
	Particle nextParticles[];
};

// Simulation parameters
uniform int numParticles;
uniform float deltaTime;
uniform vec2 gravity;
uniform vec2 bounds;
uniform vec2 attractor;
uniform float attractorStrength;

// Simple hash function for pseudo-randomness
float hash(float n) { 
	return fract(sin(n) * 43758.5453123); 
}

void main() {
	uint index = gl_GlobalInvocationID.x;
	
	// Skip if beyond the particle count.
	if (index >= numParticles) {
		return;
	}
	
	// Read current particle
	Particle particle = particles[index];
	
	// Calculate new acceleration
	vec2 acceleration = gravity;
	
	// Add attractor force (without branching)
	vec2 direction = attractor - particle.position;
	float distance = max(length(direction), 5.0);  // Avoid division by zero and extreme forces
	direction = normalize(direction);
	
	// Force proportional to 1/distance^2 (multiplied by attractorStrength which can be 0)
	float minDistance = 10.0;
	float maxForce = 1000.0;
	float clampedDistance = max(minDistance, distance);
	vec2 attractorForce = direction * clamp(attractorStrength / (clampedDistance * clampedDistance), -maxForce, maxForce);
	// vec2 attractorForce = direction * (attractorStrength / (distance * distance));
	acceleration += attractorForce;
	
	// Clamp maximum acceleration to prevent instability
	float maxAccel = 500.0;
	float accelMagnitude = length(acceleration);
	if (accelMagnitude > maxAccel) {
			acceleration = normalize(acceleration) * maxAccel;
	}
	
	// Update velocity with acceleration
	particle.velocity += acceleration * deltaTime;
	
	// Add a bit of damping to prevent infinite acceleration
	particle.velocity *= 0.99;
	
	// Update position with velocity
	particle.position += particle.velocity * deltaTime;
	
	// Boundary collision - bounce with some energy loss
	if (particle.position.x < 0.0) {
		particle.position.x = 0.0;
		particle.velocity.x = -particle.velocity.x * 0.8;
	} else if (particle.position.x > bounds.x) {
		particle.position.x = bounds.x;
		particle.velocity.x = -particle.velocity.x * 0.8;
	}
	
	if (particle.position.y < 0.0) {
		particle.position.y = 0.0;
		particle.velocity.y = -particle.velocity.y * 0.8;
	} else if (particle.position.y > bounds.y) {
		particle.position.y = bounds.y;
		particle.velocity.y = -particle.velocity.y * 0.8;
	}

	// Apply damping after bounce logic.
	particle.velocity *= 0.99;
	
	// Update lifetime
	particle.lifetime = max(0.0, particle.lifetime - deltaTime);

	// Update color based on velocity
	float speed = length(particle.velocity);
	
	// Color shift based on speed (blue->green->red as speed increases)
	particle.color = vec4(
		min(1.0, speed / 300.0),                  // Red increases with speed
		min(1.0, 150.0 / (100.0 + speed)),        // Green highest at medium speed
		min(1.0, 100.0 / (50.0 + speed)),         // Blue highest at low speed
		min(1.0, particle.lifetime / 5.0)         // Alpha fades out as lifetime decreases
	);
	
	// Reset dead particles with randomness
	if (particle.lifetime <= 0.0) {
		float randomSeed = float(index) + deltaTime * hash(float(index) * 1.23);

		particle.acceleration = vec2(0.0);
		
		// Reset to a new particle with some randomness
		particle.position = vec2(
			bounds.x * (0.5 + 0.2 * hash(randomSeed)),
			bounds.y * (0.5 + 0.2 * hash(randomSeed))
		);
		
		particle.velocity = vec2(
			(hash(randomSeed + 2.0) * 200.0 - 100.0) * 2.0,         // Random horizontal velocity
			(-(50.0 + hash(randomSeed + 3.0)) * 150.0) * 2.0        // Random upward velocity
		);
		
		particle.lifetime = 8.0 + hash(randomSeed + 4.0) * 7.0;  // Random lifetime between 8-15s
		particle.size = 1.0 + hash(randomSeed + 5.0) * 2.0;      // Random size between 1-3
	}

	// Write to output buffer
	nextParticles[index] = particle;
}";
    
	// One particle is position(2), velocity(2), acceleration(2), color(4), lifetime(1), size(1) = 12 floats
	// + 4 floats of padding.
	// Important!  vec4 needs to be aligned to a 16-byte boundary!
	private const int FLOATS_PER_PARTICLE = 16;

	#endregion

	#region Fields

	private int _numParticles;
	private float[] _particleData;
	private ShaderProgram _computeProgram;
	private ShaderBuffer<float>[] _particleBuffers;
	private int _activeBuffer = 0;
	private bool _disposedValue;
	
	// Simulation parameters
	private float _deltaTime = 0.016f; // ~60 FPS
	private Vector2 _gravity = new Vector2(0.0f, 9.8f);
	private Vector2 _bounds;
	private Vector2 _attractor = new Vector2(0.0f, 0.0f);
	private float _attractorStrength = 0.0f;

	#endregion

	#region Constructors

	public ParticlesSim(int numParticles, int width, int height)
	{
		_numParticles = numParticles;
		_bounds = new Vector2(width, height);
		_particleData = new float[FLOATS_PER_PARTICLE * _numParticles];

		_computeProgram = ShaderProgram.ForCompute(PARTICLE_SHADER);

		_particleBuffers = [
			new ShaderBuffer<float>(_numParticles * FLOATS_PER_PARTICLE, 0),
			new ShaderBuffer<float>(_numParticles * FLOATS_PER_PARTICLE, 1),
		];

		InitializeParticles();
		
		// Upload initial data.
		_particleBuffers[0].Set(_particleData);
		_particleBuffers[1].Set(_particleData);
	}

	#endregion

	public void InitializeParticles()
	{
		var rand = new Random();
		
		for (var i = 0; i < _numParticles; i++)
		{
			var index = i * FLOATS_PER_PARTICLE;
			
			// Position - start in middle near top with more randomness
			_particleData[index] = (float)(_bounds.X * (0.5 + 0.2 * rand.NextDouble()));
			_particleData[index + 1] = (float)(_bounds.Y * (0.5 + 0.2 * rand.NextDouble()));
			
			// Velocity - random spread
			_particleData[index + 2] = (float)(rand.NextDouble() * 200.0 - 100.0) * 2.0f;
			_particleData[index + 3] = (float)-(50.0 + rand.NextDouble() * 150.0) * 2.0f;
			
			// Acceleration - initialized to zero
			_particleData[index + 4] = 0.0f;
			_particleData[index + 5] = 0.0f;
			
			// Color - initial color based on position (will be updated by shader)
			_particleData[index + 8] = (float)rand.NextDouble();  // r
			_particleData[index + 9] = (float)rand.NextDouble();  // g
			_particleData[index + 10] = (float)rand.NextDouble();  // b
			_particleData[index + 11] = 1.0f;  // a
			
			// Lifetime - more variation
			_particleData[index + 12] = (float)(8.0 + rand.NextDouble() * 7.0);
			
			// Size
			_particleData[index + 13] = (float)(1.0 + rand.NextDouble() * 2.0);
		}
	}

	public void Update(GameTime gameTime)
	{
		_computeProgram.Use();
    _deltaTime = (float)gameTime.ElapsedTime.TotalSeconds;
    
    // Explicitly bind the input and output buffers to their respective binding points
    _particleBuffers[_activeBuffer].Bind(0);       // Active buffer as input (binding 0)
    _particleBuffers[1 - _activeBuffer].Bind(1);   // Inactive buffer as output (binding 1)
    
    // Set uniforms
    _computeProgram.GetUniform1("numParticles").Set(_numParticles);
    _computeProgram.GetUniform1("deltaTime").Set(_deltaTime);
    _computeProgram.GetUniform2("gravity").Set(_gravity);
    _computeProgram.GetUniform2("bounds").Set(_bounds);
    _computeProgram.GetUniform2("attractor").Set(_attractor);
    _computeProgram.GetUniform1("attractorStrength").Set(_attractorStrength);
    
    // Dispatch compute shader
    int workGroupSize = 128; // Should match local_size_x in shader
    int numWorkGroups = (_numParticles + workGroupSize - 1) / workGroupSize;
    
    GL.DispatchCompute(numWorkGroups, 1, 1);
    
    // Memory barrier to ensure compute shader writes are visible
    GL.MemoryBarrier(MemoryBarrierFlags.ShaderStorageBarrierBit);
    
    // Switch active buffer for next frame
    _activeBuffer = 1 - _activeBuffer;
    
    // Read back data from the now-current output buffer (which will be input next frame)
    // This is the buffer that just got written to by the compute shader
    _particleData = _particleBuffers[_activeBuffer].Get();
	}

	public void SetAttractor(float x, float y, float strength)
	{
		_attractor.X = x;
		_attractor.Y = y;
		_attractorStrength = strength;
	}

	public void SetGravity(float x, float y)
	{
		_gravity.X = x;
		_gravity.Y = y;
	}

	public float[] GetParticleData()
	{
		return _particleData;
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
				_particleBuffers[0].Dispose();
				_particleBuffers[1].Dispose();
				_computeProgram.Dispose();
			}

			_disposedValue = true;
		}
	}

	~ParticlesSim()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method.
		Dispose(disposing: false);
	}

	void IDisposable.Dispose()
    {
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method.
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
    }
}

class ParticlesState : GameState
{
	#region Fields

	private Vector2 _mousePosition = Vector2.Zero;
	private ParticlesSim? _sim = null;

	#endregion

	#region Constructors

	public ParticlesState()
		: base()
	{
		UI.Add(new Label(() => _sim?.GetType().Name ?? "N/A", new Vector2(0, 0), new RadialColor(5, 5, 5)));
	}

	#endregion

	#region Properties

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
	}

	public override void LostFocus(EventBus eventBus)
	{
		eventBus.Unsubscribe<KeyEventArgs>(OnKey);
		eventBus.Unsubscribe<MouseMoveEventArgs>(OnMouseMove);
		eventBus.Unsubscribe<MouseButtonEventArgs>(OnMouseButton);

		base.LostFocus(eventBus);
	}

	private const int NUM_PARTICLES = 1024;
	private const int FLOATS_PER_PARTICLE = 16;
	public override void Render(RenderingContext rc, GameTime gameTime)
	{
		rc.Clear();

		if (_sim == null)
		{
			_sim = new ParticlesSim(NUM_PARTICLES, rc.Width, rc.Height);
			_sim.SetGravity(0, 100); // Downward gravity
		}

		var particleData = _sim.GetParticleData();
		for (var i = 0; i < NUM_PARTICLES; i++)
		{
			var srcIdx = i * FLOATS_PER_PARTICLE;

			// Position
			var position = new Vector2(particleData[srcIdx], particleData[srcIdx + 1]);
			
			// Color
			var a = MathHelper.Clamp(particleData[srcIdx + 11], 0.0f, 1.0f);

			var r = MathHelper.Clamp(particleData[srcIdx + 8] * a, 0.0f, 1.0f);
			var g = MathHelper.Clamp(particleData[srcIdx + 9] * a, 0.0f, 1.0f);
			var b = MathHelper.Clamp(particleData[srcIdx + 10] * a, 0.0f, 1.0f);
			var color = new RadialColor((byte)(r * 5), (byte)(g * 5), (byte)(b * 5)); 
			
			// Size
			var size = particleData[srcIdx + 13] * 1; // Scale size for visibility
			
			if (particleData[srcIdx + 12] <= 0)
			{
				continue;
			}

			// rc.RenderFilledCircle(position, (int)size, color);
			rc.RenderFilledRect((int)(position.X - size), (int)(position.Y - size), (int)(position.X + size), (int)(position.Y + size), color.Index);
		}

		base.Render(rc, gameTime);
	}

	public override void Update(GameTime gameTime)
	{
		// Update particle simulation
    _sim?.Update(gameTime);
		// _sim?.Update(gameTime);

		base.Update(gameTime);
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
			}
		}
		else
		{
			// switch (e.Key)
			// {
			// 	case Keys.Tab:
			// 		_sim?.SwapGenerators();
			// 		break;
			// 	case Keys.R:
			// 		_sim?.Randomize();
			// 		break;
			// }
		}
	}

	bool _isDragging = false;
	private void OnMouseMove(MouseMoveEventArgs e)
	{
		_mousePosition = e.Position;
		if (_isDragging)
		{
			_sim?.SetAttractor(_mousePosition.X, _mousePosition.Y, 5000);
		}
	}

	private void OnMouseButton(MouseButtonEventArgs e)
	{
		if (e.Button == MouseButton.Left)
		{
			if (e.Action == InputAction.Press)
			{
				_isDragging = true;
        _sim?.SetAttractor(_mousePosition.X, _mousePosition.Y, 5000);
			}
			else
			{
				_sim?.SetAttractor(0, 0, 0);
			}
		}
	}

	#endregion
}