// States\Particles\ParticlesSim.cs

using Critters.Gfx;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System.Diagnostics;

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

class ParticlesSim : IDisposable
{
	#region Constants

	private const int WORKGROUP_SIZE_X = 128;
	private const int WORKGROUP_SIZE_Y = 1;
	private const int WORKGROUP_SIZE_Z = 1;
	private const string SHADER_FILENAME = "assets/shaders/compute/particle.glsl";

	#endregion

	#region Fields

	private int _numParticles;
	private ParticleData[] _particleData;
	private ShaderProgram _computeProgram;
	private ShaderBuffer<ParticleData>[] _particleBuffers;
	private int _activeBuffer = 0;
	private bool _disposedValue;
	private bool _needsDataReadback = true;
	private ParticleSimParams _params;

	#endregion

	#region Constructors

	public ParticlesSim(int numParticles, int width, int height)
	{
		_numParticles = numParticles;
		_params = new ParticleSimParams
		{
			Bounds = new Vector2(width, height)
		};

		_particleData = new ParticleData[_numParticles];

		// Try to load shader from file, fall back to embedded shader
		string shaderCode = string.Empty;
		try
		{
			if (File.Exists(SHADER_FILENAME))
			{
				shaderCode = File.ReadAllText(SHADER_FILENAME);
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Failed to load particle shader from file: {ex.Message}");
			throw;
		}

		_computeProgram = ShaderProgram.ForCompute(shaderCode);

		_particleBuffers = [
			new ShaderBuffer<ParticleData>(_numParticles, 0),
			new ShaderBuffer<ParticleData>(_numParticles, 1),
		];

		InitializeParticles();

		// Upload initial data.
		_particleBuffers[0].Set(_particleData);
		_particleBuffers[1].Set(_particleData);
	}

	#endregion

	#region Methods

	/// <summary>
	/// Resets a single particle with new random values
	/// </summary>
	public void ResetParticle(int i)
	{
		var rand = Random.Shared;

		// Position - start in middle with randomness
		_particleData[i].Position = new Vector2(
			(float)(_params.Bounds.X * (0.5 + 0.2 * rand.NextDouble())),
			(float)(_params.Bounds.Y * (0.5 + 0.2 * rand.NextDouble()))
		);

		// Velocity - random spread
		_particleData[i].Velocity = new Vector2(
			(float)(rand.NextDouble() * 200.0 - 100.0) * 2.0f,
			(float)-(50.0 + rand.NextDouble() * 150.0) * 2.0f
		);

		// Acceleration - initialized to zero
		_particleData[i].Acceleration = Vector2.Zero;

		// Color - initial color based on position (will be updated by shader)
		_particleData[i].Color = new Vector4(
			(float)rand.NextDouble(),
			(float)rand.NextDouble(),
			(float)rand.NextDouble(),
			1.0f
		);

		// Lifetime - more variation
		_particleData[i].Lifetime = (float)(8.0 + rand.NextDouble() * 7.0);

		// Size
		_particleData[i].Size = (float)(1.0 + rand.NextDouble() * 2.0);

		// Mark that we need to upload data
		_needsDataReadback = true;
	}

	/// <summary>
	/// Initialize all particles with random values
	/// </summary>
	public void InitializeParticles()
	{
		for (var i = 0; i < _numParticles; i++)
		{
			ResetParticle(i);
		}

		// Upload the reset particles to both buffers
		_particleBuffers[0].Set(_particleData);
		_particleBuffers[1].Set(_particleData);
		_needsDataReadback = true;
	}

	/// <summary>
	/// Update the particle simulation
	/// </summary>
	public void Update(GameTime gameTime)
	{
		_computeProgram.Use();
		float deltaTime = (float)gameTime.ElapsedTime.TotalSeconds;

		// Explicitly bind the input and output buffers to their respective binding points
		_particleBuffers[_activeBuffer].Bind(0);       // Active buffer as input (binding 0)
		_particleBuffers[1 - _activeBuffer].Bind(1);   // Inactive buffer as output (binding 1)

		// Set uniforms
		_computeProgram.GetUniform1("numParticles").Set(_numParticles);
		_computeProgram.GetUniform1("deltaTime").Set(deltaTime);
		_computeProgram.GetUniform2("gravity").Set(_params.Gravity);
		_computeProgram.GetUniform2("bounds").Set(_params.Bounds);
		_computeProgram.GetUniform2("attractor").Set(_params.Attractor);
		_computeProgram.GetUniform1("attractorStrength").Set(_params.AttractorStrength);
		_computeProgram.GetUniform1("dampingFactor").Set(_params.DampingFactor);
		_computeProgram.GetUniform1("bounceEnergyLoss").Set(_params.BounceEnergyLoss);
		_computeProgram.GetUniform1("maxAcceleration").Set(_params.MaxAcceleration);
		_computeProgram.GetUniform1("maxForce").Set(_params.MaxForce);
		_computeProgram.GetUniform1("minAttractorDistance").Set(_params.MinAttractorDistance);

		// Dispatch compute shader
		int numWorkGroups = (_numParticles + WORKGROUP_SIZE_X - 1) / WORKGROUP_SIZE_X;

		GL.DispatchCompute(numWorkGroups, 1, 1);

		// Check for OpenGL errors
		CheckGLError("Dispatch Compute");

		// Memory barrier to ensure compute shader writes are visible
		GL.MemoryBarrier(MemoryBarrierFlags.ShaderStorageBarrierBit);

		// Switch active buffer for next frame
		_activeBuffer = 1 - _activeBuffer;

		// Mark that data needs to be read back when requested
		_needsDataReadback = true;
	}

	/// <summary>
	/// Set the attractor position and strength
	/// </summary>
	public void SetAttractor(float x, float y, float strength)
	{
		_params.Attractor = new Vector2(x, y);
		_params.AttractorStrength = strength;
	}

	/// <summary>
	/// Set the gravity vector
	/// </summary>
	public void SetGravity(float x, float y)
	{
		_params.Gravity = new Vector2(x, y);
	}

	/// <summary>
	/// Get the current gravity vector
	/// </summary>
	public Vector2 GetGravity()
	{
		return _params.Gravity;
	}

	/// <summary>
	/// Get the current particle data
	/// </summary>
	public ParticleData[] GetParticleData()
	{
		// Only read back data from GPU when necessary
		if (_needsDataReadback)
		{
			_particleData = _particleBuffers[_activeBuffer].Get();
			_needsDataReadback = false;
		}
		return _particleData;
	}

	/// <summary>
	/// Helper method to check for OpenGL errors
	/// </summary>
	private void CheckGLError(string operation)
	{
		ErrorCode error;
		while ((error = GL.GetError()) != ErrorCode.NoError)
		{
			Debug.WriteLine($"OpenGL Error during {operation}: {error}");
		}
	}

	#region IDisposable Implementation

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
				// Dispose managed resources
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

	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method.
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	#endregion

	#endregion
}