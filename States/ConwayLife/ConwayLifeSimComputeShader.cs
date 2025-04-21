using Critters.Gfx;
using OpenTK.Graphics.OpenGL4;
using System;

namespace Critters.States.ConwayLife;

class ConwayLifeSimComputeShader : ICellularAutomata, IDisposable
{
	#region Constants

	// Make workgroup size adaptive based on grid dimensions
	private const int WORKGROUP_SIZE_X = 16;
	private const int WORKGROUP_SIZE_Y = 16;
	private const int WORKGROUP_SIZE_Z = 1;

	private const string NAME_WIDTH = "width";
	private const string NAME_HEIGHT = "height";
	private const string NAME_WRAP_EDGES = "wrapEdges";

	#endregion

	#region Fields

	private float[] _cellData;
	private ShaderProgram _computeProgram;
	private ShaderBuffer<float> _currentStateSSBO;
	private ShaderBuffer<float> _nextStateSSBO;
	private bool _disposedValue;
	private bool _wrapEdges = true;

	#endregion

	#region Constructors

	public ConwayLifeSimComputeShader(int width, int height)
	{
		try
		{
			Width = width;
			Height = height;
			_cellData = new float[4 * Width * Height];

			// Check if compute shaders are supported
			int majorVersion = GL.GetInteger(GetPName.MajorVersion);
			int minorVersion = GL.GetInteger(GetPName.MinorVersion);

			if (majorVersion < 4 || (majorVersion == 4 && minorVersion < 3))
			{
				throw new NotSupportedException("Compute shaders require OpenGL 4.3 or higher.");
			}

			// Create compute shader program
			var shaderSource = File.ReadAllText("assets/shaders/compute/conway.glsl");
			_computeProgram = ShaderProgram.ForCompute(shaderSource);

			// Create and bind two SSBOs
			_currentStateSSBO = new ShaderBuffer<float>(4 * Width * Height, 0);
			_nextStateSSBO = new ShaderBuffer<float>(4 * Width * Height, 1);

			// Set uniform variables
			_computeProgram.Use();
			_computeProgram.GetUniform1(NAME_WIDTH).Set(width);
			_computeProgram.GetUniform1(NAME_HEIGHT).Set(height);
			_computeProgram.GetUniform1(NAME_WRAP_EDGES).Set(_wrapEdges);
		}
		catch (Exception ex)
		{
			// Clean up any resources that were created before the exception
			Dispose(true);
			throw new Exception($"Failed to initialize compute shader: {ex.Message}", ex);
		}
	}

	#endregion

	#region Properties

	public int Width { get; }
	public int Height { get; }

	public bool this[int y, int x]
	{
		get
		{
			if (x < 0 || x >= Width || y < 0 || y >= Height)
			{
				return false;
			}

			var index = 4 * (x + y * Width);
			return _cellData[index] > 0.5f;
		}

		set
		{
			if (x < 0 || x >= Width || y < 0 || y >= Height)
			{
				return;
			}

			var index = 4 * (x + y * Width);
			_cellData[index] = value ? 1.0f : 0.0f;
		}
	}

	#endregion

	#region Methods

	public void Step()
	{
		try
		{
			// Use the compute shader program
			_computeProgram.Use();

			// Update the current state buffer
			_currentStateSSBO.Set(_cellData);

			// Set uniform for wrap edges (might change during runtime)
			_computeProgram.GetUniform1(NAME_WRAP_EDGES).Set(_wrapEdges);

			// Calculate dispatch dimensions - ensure we cover the entire grid
			int dispatchX = (Width + WORKGROUP_SIZE_X - 1) / WORKGROUP_SIZE_X;
			int dispatchY = (Height + WORKGROUP_SIZE_Y - 1) / WORKGROUP_SIZE_Y;

			// Run the compute shader
			GL.DispatchCompute(dispatchX, dispatchY, WORKGROUP_SIZE_Z);
			GL.MemoryBarrier(MemoryBarrierFlags.ShaderStorageBarrierBit);

			// Read back data from the output buffer
			_cellData = _nextStateSSBO.Get();

			// Swap buffers for next iteration
			var temp = _currentStateSSBO;
			_currentStateSSBO = _nextStateSSBO;
			_nextStateSSBO = temp;

			// Update binding points after swapping
			_currentStateSSBO.BaseIndex = 0;
			_nextStateSSBO.BaseIndex = 1;
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error in compute shader execution: {ex.Message}");
			// Continue with previous state
		}
	}

	public void Configure(SimulationConfig config)
	{
		_wrapEdges = config.WrapEdges;
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
				// Dispose managed state (managed objects)
				_currentStateSSBO?.Dispose();
				_nextStateSSBO?.Dispose();
				_computeProgram?.Dispose();
			}

			_disposedValue = true;
		}
	}

	~ConwayLifeSimComputeShader()
	{
		Dispose(disposing: false);
	}

	void IDisposable.Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	#endregion
}