using Critters.Gfx;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Critters.States.FloatingConwayLife;

class ConwayLifeSimComputeShader : IDisposable
{
	#region Constants

	private const int WORKGROUP_SIZE_X = 16;
	private const int WORKGROUP_SIZE_Y = 16;
	private const int WORKGROUP_SIZE_Z = 1;

	private const string NAME_WIDTH = "width";
	private const string NAME_HEIGHT = "height";

	private static readonly string SHADER_SOURCE = @$"
#version 430 core

// Define work group size.
layout(local_size_x = {WORKGROUP_SIZE_X}, local_size_y = {WORKGROUP_SIZE_Y}, local_size_z = {WORKGROUP_SIZE_Z}) in;

// Input buffer containing current cell states (1.0 = alive, 0.0 = dead).
layout(std430, binding = 0) buffer CellBuffer {{
	vec4 cells[];
}};

// Output buffer for next generation.
layout(std430, binding = 1) buffer NextGenBuffer {{
	vec4 nextGen[];
}};

// Grid dimensions as uniform variables.
uniform int {NAME_WIDTH};
uniform int {NAME_HEIGHT};

// Helper function to get cell state (1.0 = alive, 0.0 = dead).
vec4 getCellState(uint x, uint y) {{
	// Handle wrap-around edges (toroidal grid).
	x = (x + {NAME_WIDTH}) % {NAME_WIDTH};
	y = (y + {NAME_HEIGHT}) % {NAME_HEIGHT};
	
	// Calculate buffer index.
	uint index = uint(x + y * {NAME_WIDTH});
	
	// The cell state is stored in the x component of the vec4.
	return cells[index];
}}

// Count live neighbors.
vec4 countLiveNeighbors(uint x, uint y) {{
	vec4 liveNeighbors = vec4(0.0);
	for (int dy = -1; dy <= 1; dy++) {{
		for (int dx = -1; dx <= 1; dx++) {{
			// Skip the cell itself.
			if (dx == 0 && dy == 0) continue;
			
			// Add the state of this neighbor.
			liveNeighbors += getCellState(x + dx, y + dy);
		}}
	}}
	return liveNeighbors;
}}

// Apply Conway's Game of Life rules.
float applyRules(float currentState, float liveNeighbors) {{
	float newState = 0.0;
	
	if (currentState > 0.5) {{
		// Cell is currently alive.
		if (liveNeighbors < 2.0 || liveNeighbors > 3.0) {{
			// Dies from underpopulation or overpopulation.
			newState = 0;
		}} else {{
			// Stays alive.
			newState = 1;
		}}
	}} else {{
		// Cell is currently dead.
		if (liveNeighbors == 3.0) {{
			// Becomes alive through reproduction.
			newState = 1.0;
		}} else {{
			// Stays dead.
			newState = 0.0;
		}}
	}}

	return newState;
}}

vec4 applyRules(vec4 currentState, vec4 liveNeighbors) {{
	vec4 newState = vec4(
		applyRules(currentState[0], liveNeighbors[0]),
		applyRules(currentState[1], liveNeighbors[1]),
		applyRules(currentState[2], liveNeighbors[2]),
		0.0
	);

	return newState;
}}

void main() {{
	// Get global position.
	uint x = gl_GlobalInvocationID.x;
	uint y = gl_GlobalInvocationID.y;
	
	// Skip computation if outside grid bounds.
	if (x >= {NAME_WIDTH} || y >= {NAME_HEIGHT}) {{
		return;
	}}
	
	// Calculate index in the buffer.
	uint index = x + y * {NAME_WIDTH};
	
	// Get current cell state.
	vec4 currentState = getCellState(x, y);

	// Count live neighbors.
	vec4 liveNeighbors = countLiveNeighbors(x, y);
	
	// Apply Conway's Game of Life rules.
	vec4 newState = applyRules(currentState, liveNeighbors);
	
	// Update the output buffer.
	nextGen[index] = newState;
}}
";

	#endregion

	#region Fields

	private Vector4[] _cellData;
	private ShaderProgram _computeProgram;
	private ShaderBuffer<Vector4> _currentStateSSBO;
	private ShaderBuffer<Vector4> _nextStateSSBO;
	private bool _disposedValue;

	#endregion

	#region Constructors

	public ConwayLifeSimComputeShader(int width, int height)
	{
		Width = width;
		Height = height;
		_cellData = new Vector4[Width * Height];

		_computeProgram = ShaderProgram.ForCompute(SHADER_SOURCE);

		// Create and compile the compute shader
		using var computeShader = new Shader(ShaderType.ComputeShader, SHADER_SOURCE);

		// Create and bind two SSBOs
		_currentStateSSBO = new ShaderBuffer<Vector4>(Width * Height, 0);
		_nextStateSSBO = new ShaderBuffer<Vector4>(Width * Height, 1);

		// Set uniform variables.
		// Width and height are readonly, so we should only need to do this once.
		_computeProgram.GetUniform1(NAME_WIDTH).Set(width);
		_computeProgram.GetUniform1(NAME_HEIGHT).Set(height);
	}

	#endregion

	#region Properties

	public int Width { get; }
	public int Height { get; }

	public Vector4 this[int y, int x]
	{
		get
		{
			if (x < 0 || x >= Width || y < 0 || y >= Height)
			{
				return Vector4.Zero;
			}

			var index = x + y * Width;
			return _cellData[index];
		}
		set
		{
			if (x < 0 || x >= Width || y < 0 || y >= Height)
			{
				return;
			}

			var index = x + y * Width;
			_cellData[index] = value;
		}
	}

	#endregion

	#region Methods

	public void Randomize()
	{
		for (var y = 0; y < Height; y++)
		{
			for (var x = 0; x < Width; x++)
			{
				this[y, x] = new Vector4(
					Random.Shared.NextDouble() < 0.5 ? 0 : 1,
					0.0f, // Random.Shared.NextDouble() < 0.5 ? 0 : 1,
					0.0f, // Random.Shared.NextDouble() < 0.5 ? 0 : 1,
					1.0f
				);
			}
		}

		_currentStateSSBO.Set(_cellData);
		_nextStateSSBO.Set(_cellData);
	}

	public void Step()
	{
    // Use the compute shader program.
		_computeProgram.Use();

    // Make sure uniforms are set (might be necessary to set every frame).
		_computeProgram.GetUniform1(NAME_WIDTH).Set(Width);
		_computeProgram.GetUniform1(NAME_HEIGHT).Set(Height);

    // Run the compute shader.
    GL.DispatchCompute(Width / WORKGROUP_SIZE_X, Height / WORKGROUP_SIZE_Y, WORKGROUP_SIZE_Z);
    GL.MemoryBarrier(MemoryBarrierFlags.ShaderStorageBarrierBit);

    // Read back data from the OUTPUT buffer (before swapping).
		_cellData = _nextStateSSBO.Get();

    // Swap buffers for next iteration.
    var temp = _currentStateSSBO;
    _currentStateSSBO = _nextStateSSBO;
    _nextStateSSBO = temp;

    // Update binding points after swapping.
		_currentStateSSBO.BaseIndex = 0;
		_nextStateSSBO.BaseIndex = 1;
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
				// Dispose managed state (managed objects).
				_currentStateSSBO.Dispose();
				_nextStateSSBO.Dispose();
				_computeProgram.Dispose();
			}

			_disposedValue = true;
		}
	}

	~ConwayLifeSimComputeShader()
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

	#endregion
}
