using Critters.Gfx;
using OpenTK.Graphics.OpenGL4;

namespace Critters.States.ConwayLife;

class ConwayLifeSimComputeShader : ICellularAutomata, IDisposable
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
float getCellState(int x, int y) {{
	// Handle wrap-around edges (toroidal grid).
	x = (x + {NAME_WIDTH}) % {NAME_WIDTH};
	y = (y + {NAME_HEIGHT}) % {NAME_HEIGHT};
	
	// Calculate buffer index.
	uint index = uint(x + y * {NAME_WIDTH});
	
	// The cell state is stored in the x component of the vec4.
	return cells[index].x;
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
	float currentState = getCellState(int(x), int(y));
	
	// Count live neighbors.
	float liveNeighbors = 0.0;
	for (int dy = -1; dy <= 1; dy++) {{
		for (int dx = -1; dx <= 1; dx++) {{
			// Skip the cell itself.
			if (dx == 0 && dy == 0) continue;
			
			// Add the state of this neighbor.
			liveNeighbors += getCellState(int(x) + dx, int(y) + dy);
		}}
	}}
	
	// Apply Conway's Game of Life rules.
	float newState = 0.0;
	
	if (currentState > 0.5) {{
		// Cell is currently alive.
		if (liveNeighbors < 2.0 || liveNeighbors > 3.0) {{
			// Dies from underpopulation or overpopulation.
			newState = 0.0;
		}} else {{
			// Stays alive.
			newState = 1.0;
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
	
	// Update the output buffer.
	nextGen[index] = vec4(newState, 0.0, 0.0, 1.0);
}}
";

	#endregion

	#region Fields

	private float[] _cellData;
	private ShaderProgram _computeProgram;
	private ShaderBuffer<float> _currentStateSSBO;
	private ShaderBuffer<float> _nextStateSSBO;
	private bool _disposedValue;

	#endregion

	#region Constructors

	public ConwayLifeSimComputeShader(int width, int height)
	{
		Width = width;
		Height = height;
		_cellData = new float[4 * Width * Height];

		_computeProgram = ShaderProgram.ForCompute(SHADER_SOURCE);

		// Create and compile the compute shader
		using var computeShader = new Shader(ShaderType.ComputeShader, SHADER_SOURCE);

		// Create and bind two SSBOs
		_currentStateSSBO = new ShaderBuffer<float>(4 * Width * Height, 0);
		_nextStateSSBO = new ShaderBuffer<float>(4 * Width * Height, 1);

		// Set uniform variables.
		// Width and height are readonly, so we should only need to do this once.
		_computeProgram.GetUniform1(NAME_WIDTH).Set(width);
		_computeProgram.GetUniform1(NAME_HEIGHT).Set(height);
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
    // Use the compute shader program.
		_computeProgram.Use();

		_currentStateSSBO.Set(_cellData);

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
