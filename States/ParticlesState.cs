using Critters.Events;
using Critters.Gfx;
using Critters.IO;
using Critters.States.ConwayLife;
using Critters.UI;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Critters.States;

// C# class for the particle system
public class ParticlesSim : IDisposable
{
    private const string PARTICLE_SHADER = @"
#version 430 core

struct Particle {
    vec2 position;
    vec2 velocity;
    vec2 acceleration;
    vec4 color;
    float lifetime;
    float size;
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
uniform float deltaTime;
uniform vec2 gravity;
uniform vec2 bounds;
uniform vec2 attractor;
uniform float attractorStrength;

void main() {
    uint index = gl_GlobalInvocationID.x;
    
    // Skip if beyond the particle count
    if (index >= particles.length()) {
        return;
    }
    
    // Read current particle
    Particle particle = particles[index];
    
    // Calculate new acceleration
    vec2 acceleration = gravity;
    
    // Add attractor force
    if (attractorStrength != 0.0) {
        vec2 direction = attractor - particle.position;
        float distance = max(length(direction), 5.0);  // Avoid division by zero and extreme forces
        direction = normalize(direction);
        
        // Force proportional to 1/distance²
        acceleration += direction * (attractorStrength / (distance * distance));
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
    }
    else if (particle.position.x > bounds.x) {
        particle.position.x = bounds.x;
        particle.velocity.x = -particle.velocity.x * 0.8;
    }
    
    if (particle.position.y < 0.0) {
        particle.position.y = 0.0;
        particle.velocity.y = -particle.velocity.y * 0.8;
    }
    else if (particle.position.y > bounds.y) {
        particle.position.y = bounds.y;
        particle.velocity.y = -particle.velocity.y * 0.8;
    }
    
    // Update lifetime
    particle.lifetime -= deltaTime;
    
    // Update color based on velocity
    float speed = length(particle.velocity);
    
    // Color shift based on speed (blue->green->red as speed increases)
    particle.color = vec4(
        min(1.0, speed / 300.0),                  // Red increases with speed
        min(1.0, 150.0 / (100.0 + speed)),        // Green highest at medium speed
        min(1.0, 100.0 / (50.0 + speed)),         // Blue highest at low speed
        min(1.0, particle.lifetime / 5.0)         // Alpha fades out as lifetime decreases
    );
    
    // Reset dead particles
    if (particle.lifetime <= 0.0) {
        // Reset to a new particle at the center
        particle.position = vec2(bounds.x * 0.5, bounds.y * 0.8);
        particle.velocity = vec2(
            (gl_GlobalInvocationID.x % 200) - 100.0,  // Spread velocities based on ID
            -100.0 - (gl_GlobalInvocationID.x % 100)
        );
        particle.lifetime = 10.0 + (gl_GlobalInvocationID.x % 5);
    }
    
    // Write to output buffer
    nextParticles[index] = particle;
}
";

    private int _numParticles;
    private float[] _particleData;
    private int _computeShader;
    private int _computeProgram;
    private int _currentParticleSSBO;
    private int _nextParticleSSBO;
    private bool _disposedValue;
    
    // Simulation parameters
    private float _deltaTime = 0.016f; // ~60 FPS
    private Vector2 _gravity = new Vector2(0.0f, 9.8f);
    private Vector2 _bounds;
    private Vector2 _attractor = new Vector2(0.0f, 0.0f);
    private float _attractorStrength = 0.0f;
    
    // One particle is position(2), velocity(2), acceleration(2), color(4), lifetime(1), size(1) = 12 floats
    private const int FLOATS_PER_PARTICLE = 12;

    public ParticlesSim(int numParticles, int width, int height)
    {
        _numParticles = numParticles;
        _bounds = new Vector2(width, height);
        _particleData = new float[FLOATS_PER_PARTICLE * _numParticles];

        // Compile shader
        _computeShader = GL.CreateShader(ShaderType.ComputeShader);
        GL.ShaderSource(_computeShader, PARTICLE_SHADER);
        GL.CompileShader(_computeShader);

        GL.GetShader(_computeShader, ShaderParameter.CompileStatus, out int success);
        if (success == 0)
        {
            string infoLog = GL.GetShaderInfoLog(_computeShader);
            Console.WriteLine($"ERROR::COMPUTE_SHADER::COMPILATION_FAILED\n{infoLog}");
        }

        // Create program
        _computeProgram = GL.CreateProgram();
        GL.AttachShader(_computeProgram, _computeShader);
        GL.LinkProgram(_computeProgram);

        GL.GetProgram(_computeProgram, GetProgramParameterName.LinkStatus, out success);
        if (success == 0)
        {
            string infoLog = GL.GetProgramInfoLog(_computeProgram);
            Console.WriteLine($"ERROR::PROGRAM::LINKING_FAILED\n{infoLog}");
        }

        // Create buffers
        GL.GenBuffers(1, out _currentParticleSSBO);
        GL.GenBuffers(1, out _nextParticleSSBO);

        // Size in bytes
        int bufferSize = sizeof(float) * FLOATS_PER_PARTICLE * _numParticles;

        // Initialize buffers
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _currentParticleSSBO);
        GL.BufferData(BufferTarget.ShaderStorageBuffer, bufferSize, IntPtr.Zero, BufferUsageHint.DynamicCopy);
        GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, _currentParticleSSBO);

        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _nextParticleSSBO);
        GL.BufferData(BufferTarget.ShaderStorageBuffer, bufferSize, IntPtr.Zero, BufferUsageHint.DynamicCopy);
        GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, _nextParticleSSBO);

        // Initialize particles
        InitializeParticles();
    }

    public void InitializeParticles()
    {
        Random rand = new Random();
        
        for (int i = 0; i < _numParticles; i++)
        {
            int index = i * FLOATS_PER_PARTICLE;
            
            // Position - start in middle near top
            _particleData[index] = (float)(_bounds.X * 0.5 + (rand.NextDouble() - 0.5) * 50);
            _particleData[index + 1] = (float)(_bounds.Y * 0.8 + (rand.NextDouble() - 0.5) * 20);
            
            // Velocity - random spread
            _particleData[index + 2] = (float)((rand.NextDouble() - 0.5) * 200);
            _particleData[index + 3] = (float)(-50 - rand.NextDouble() * 100);
            
            // Acceleration - initialized to zero
            _particleData[index + 4] = 0.0f;
            _particleData[index + 5] = 0.0f;
            
            // Color - initial color based on position (will be updated by shader)
            _particleData[index + 6] = (float)rand.NextDouble();  // r
            _particleData[index + 7] = (float)rand.NextDouble();  // g
            _particleData[index + 8] = (float)rand.NextDouble();  // b
            _particleData[index + 9] = 1.0f;  // a
            
            // Lifetime
            _particleData[index + 10] = (float)(5.0 + rand.NextDouble() * 5.0);
            
            // Size
            _particleData[index + 11] = (float)(1.0 + rand.NextDouble() * 2.0);
        }
        
        // Upload initial data
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _currentParticleSSBO);
        GL.BufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, 
                        sizeof(float) * _particleData.Length, _particleData);
    }

    public void Update(float deltaTime)
    {
        _deltaTime = deltaTime;
        
        // Use compute shader
        GL.UseProgram(_computeProgram);
        
        // Set uniforms
        int deltaTimeLoc = GL.GetUniformLocation(_computeProgram, "deltaTime");
        int gravityLoc = GL.GetUniformLocation(_computeProgram, "gravity");
        int boundsLoc = GL.GetUniformLocation(_computeProgram, "bounds");
        int attractorLoc = GL.GetUniformLocation(_computeProgram, "attractor");
        int strengthLoc = GL.GetUniformLocation(_computeProgram, "attractorStrength");
        
        GL.Uniform1(deltaTimeLoc, _deltaTime);
        GL.Uniform2(gravityLoc, _gravity.X, _gravity.Y);
        GL.Uniform2(boundsLoc, _bounds.X, _bounds.Y);
        GL.Uniform2(attractorLoc, _attractor.X, _attractor.Y);
        GL.Uniform1(strengthLoc, _attractorStrength);
        
        // Dispatch compute shader
        int workGroupSize = 128; // This should match the local_size_x in the shader
        int numWorkGroups = (_numParticles + workGroupSize - 1) / workGroupSize; // Ceiling division
        
        GL.DispatchCompute(numWorkGroups, 1, 1);
        GL.MemoryBarrier(MemoryBarrierFlags.ShaderStorageBarrierBit);
        
        // Read back particle data
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _nextParticleSSBO);
        GL.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, 
                          sizeof(float) * _particleData.Length, _particleData);
        
        // Swap buffers for next update
        int temp = _currentParticleSSBO;
        _currentParticleSSBO = _nextParticleSSBO;
        _nextParticleSSBO = temp;
        
        // Update binding points
        GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, _currentParticleSSBO);
        GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, _nextParticleSSBO);
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

    public void Dispose()
    {
        if (!_disposedValue)
        {
            GL.DeleteBuffer(_currentParticleSSBO);
            GL.DeleteBuffer(_nextParticleSSBO);
            GL.DeleteProgram(_computeProgram);
            GL.DeleteShader(_computeShader);
            
            _disposedValue = true;
        }
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

	public override void Render(RenderingContext rc, GameTime gameTime)
	{
		if (_sim == null)
		{
			_sim = new ParticlesSim(128, rc.Width, rc.Height);
			_sim.Randomize();
		}

		_sim.Render(rc);

		base.Render(rc, gameTime);
	}

	public override void Update(GameTime gameTime)
	{
		_sim?.Update(gameTime);

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
			switch (e.Key)
			{
				case Keys.Tab:
					_sim?.SwapGenerators();
					break;
				case Keys.R:
					_sim?.Randomize();
					break;
			}
		}
	}

	private void OnMouseMove(MouseMoveEventArgs e)
	{
		_mousePosition = e.Position;
	}

	private void OnMouseButton(MouseButtonEventArgs e)
	{
		if (e.Button == MouseButton.Left && e.Action == InputAction.Release)
		{
			if (_sim == null)
			{
				return;
			}
			var value = _sim.GetCell(_mousePosition);
			_sim.SetCell(_mousePosition, !value);
		}
	}

	#endregion
}