using Critters.Events;
using Critters.Gfx;
using Critters.IO;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Critters.States;

class ConwayLifeSimComputeShader : IDisposable
{
	#region Constants

	private const string SHADER_SOURCE = @"
#version 430 core

// Define work group size
layout(local_size_x = 16, local_size_y = 16, local_size_z = 1) in;

// Input buffer containing current cell states (1.0 = alive, 0.0 = dead)
layout(std430, binding = 0) buffer CellBuffer {
	vec4 cells[];
};

// Output buffer for next generation
layout(std430, binding = 1) buffer NextGenBuffer {
	vec4 nextGen[];
};

// Grid dimensions as uniform variables
uniform int width;
uniform int height;

// Helper function to get cell state (1.0 = alive, 0.0 = dead)
float getCellState(int x, int y) {
	// Handle wrap-around edges (toroidal grid)
	x = (x + width) % width;
	y = (y + height) % height;
	
	// Calculate buffer index
	uint index = uint(x + y * width);
	
	// The cell state is stored in the x component of the vec4
	return cells[index].x;
}

void main() {
	// Get global position
	uint x = gl_GlobalInvocationID.x;
	uint y = gl_GlobalInvocationID.y;
	
	// Skip computation if outside grid bounds
	if (x >= width || y >= height) {
		return;
	}
	
	// Calculate index in the buffer
	uint index = x + y * width;
	
	// Get current cell state
	float currentState = getCellState(int(x), int(y));
	
	// Count live neighbors
	float liveNeighbors = 0.0;
	for (int dy = -1; dy <= 1; dy++) {
		for (int dx = -1; dx <= 1; dx++) {
			// Skip the cell itself
			if (dx == 0 && dy == 0) continue;
			
			// Add the state of this neighbor
			liveNeighbors += getCellState(int(x) + dx, int(y) + dy);
		}
	}
	
	// Apply Conway's Game of Life rules
	float newState = 0.0;
	
	if (currentState > 0.5) {
		// Cell is currently alive
		if (liveNeighbors < 2.0 || liveNeighbors > 3.0) {
			// Dies from underpopulation or overpopulation
			newState = 0.0;
		} else {
			// Stays alive
			newState = 1.0;
		}
	} else {
		// Cell is currently dead
		if (liveNeighbors == 3.0) {
			// Becomes alive through reproduction
			newState = 1.0;
		} else {
			// Stays dead
			newState = 0.0;
			}
	}
	
	// Update the output buffer
	nextGen[index] = vec4(newState, 0.0, 0.0, 1.0);
}
";

	#endregion

	#region Fields

	private float[] _cellData;
	private int _computeShader;
	private int _computeProgram;
	private int _currentStateSSBO;
	private int _nextStateSSBO;
	private bool _disposedValue;

	#endregion

	#region Constructors

	public ConwayLifeSimComputeShader(int width, int height)
	{
		Width = width;
		Height = height;
		_cellData = new float[4 * Width * Height];

		// Create and compile the compute shader
		_computeShader = GL.CreateShader(ShaderType.ComputeShader);
		GL.ShaderSource(_computeShader, SHADER_SOURCE);
		GL.CompileShader(_computeShader);

		// Check compilation status
		GL.GetShader(_computeShader, ShaderParameter.CompileStatus, out int success);
		if (success == 0)
		{
			var infoLog = GL.GetShaderInfoLog(_computeShader);
			Console.WriteLine($"ERROR::COMPUTE_SHADER::COMPILATION_FAILED\n{infoLog}");
		}

		// Create shader program
		_computeProgram = GL.CreateProgram();
		GL.AttachShader(_computeProgram, _computeShader);
		GL.LinkProgram(_computeProgram);

		// Check linking status
		GL.GetProgram(_computeProgram, GetProgramParameterName.LinkStatus, out success);
		if (success == 0)
		{
			string infoLog = GL.GetProgramInfoLog(_computeProgram);
			Console.WriteLine($"ERROR::PROGRAM::LINKING_FAILED\n{infoLog}");
		}

		// Create and bind two SSBOs
		GL.GenBuffers(1, out _currentStateSSBO);
		GL.GenBuffers(1, out _nextStateSSBO);

		// Initialize the buffers
		GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _currentStateSSBO);
		GL.BufferData(BufferTarget.ShaderStorageBuffer, sizeof(float) * 4 * Width * Height, IntPtr.Zero, BufferUsageHint.DynamicCopy);
		GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, _currentStateSSBO);

		GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _nextStateSSBO);
		GL.BufferData(BufferTarget.ShaderStorageBuffer, sizeof(float) * 4 * Width * Height, IntPtr.Zero, BufferUsageHint.DynamicCopy);
		GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, _nextStateSSBO);

		// Set uniform variables.
		// Width and height are readonly, so we should only need to do this once.
		var widthLocation = GL.GetUniformLocation(_computeProgram, "width");
		var heightLocation = GL.GetUniformLocation(_computeProgram, "height");
		GL.Uniform1(widthLocation, Width);
		GL.Uniform1(heightLocation, Height);
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
    // Write current state to the input buffer
    GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _currentStateSSBO);
    GL.BufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, sizeof(float) * _cellData.Length, _cellData);

    // Use the compute shader program
    GL.UseProgram(_computeProgram);
    
    // Make sure uniforms are set (might be necessary to set every frame)
    var widthLocation = GL.GetUniformLocation(_computeProgram, "width");
    var heightLocation = GL.GetUniformLocation(_computeProgram, "height");
    GL.Uniform1(widthLocation, Width);
    GL.Uniform1(heightLocation, Height);

    // Run the compute shader
    GL.DispatchCompute(Width / 16, Height / 16, 1);
    GL.MemoryBarrier(MemoryBarrierFlags.ShaderStorageBarrierBit);

    // Read back data from the OUTPUT buffer (before swapping)
    GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _nextStateSSBO);
    GL.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, sizeof(float) * _cellData.Length, _cellData);

    // Swap buffers for next iteration
    var temp = _currentStateSSBO;
    _currentStateSSBO = _nextStateSSBO;
    _nextStateSSBO = temp;

    // Update binding points after swapping
    GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, _currentStateSSBO);
    GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, _nextStateSSBO);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
				// Dispose managed state (managed objects)
			}

			// Delete shader as it's linked to the program and no longer needed
			GL.DeleteBuffer(_currentStateSSBO);
			GL.DeleteBuffer(_nextStateSSBO);
			GL.DeleteProgram(_computeProgram);
			GL.DeleteShader(_computeShader);

			_disposedValue = true;
		}
	}

	~ConwayLifeSimComputeShader()
	{
	    // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
	    Dispose(disposing: false);
	}

	void IDisposable.Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	#endregion
}

class ConwayLifeSim
{
	#region Constants

	private static readonly RadialColor ON_COLOR = new RadialColor(5, 5, 0);
	private static readonly RadialColor OFF_COLOR = new RadialColor(0, 0, 3);

	#endregion

	#region Fields

	private bool[,] _cells;
	private ConwayLifeSimComputeShader _shader;

	#endregion

	#region Constructors

	public ConwayLifeSim(int width, int height, bool wrap)
	{
		Width = width;
		Height = height;
		Wrap = wrap;
		_cells = new bool[height, width];
		_shader = new ConwayLifeSimComputeShader(width, height);
	}

	#endregion

	#region Properties

	public int Width { get; }
	public int Height { get; }

	/// <summary>
	/// Should the edges of the simulation wrap around to the other side? 
	/// </summary>
	public bool Wrap { get; }

	#endregion

	#region Methods

	public void Render(RenderingContext rc)
	{
		rc.Clear();

		for (var y = 0; y < Height; y++)
		{
			for (var x = 0; x < Width; x++)
			{
				if (_shader[y, x])
				{
					rc.SetPixel(new Vector2(x, y), ON_COLOR);
				}
				else
				{
					rc.SetPixel(new Vector2(x, y), OFF_COLOR);
				}
			}
		}
	}

	public void Update(GameTime gameTime)
	{
		// var newCells = new bool[Height, Width];

		// for (var y = 0; y < Height; y++)
		// {
		// 	for (var x = 0; x < Width; x++)
		// 	{
		// 		var neighbors = 0;
		// 		for (var dy = -1; dy <= 1; dy++)
		// 		{
		// 			for (var dx = -1; dx <= 1; dx++)
		// 			{
		// 				if (dx == 0 && dy == 0)
		// 				{
		// 					continue;
		// 				}

		// 				var ny = y + dy;
		// 				var nx = x + dx;

		// 				if (Wrap)
		// 				{
		// 					ny = (ny + Height) % Height;
		// 					nx = (nx + Width) % Width;
		// 				}

		// 				if (ny < 0 || ny >= Height || nx < 0 || nx >= Width)
		// 				{
		// 					continue;
		// 				}

		// 				if (_cells[ny, nx])
		// 				{
		// 					neighbors++;
		// 				}
		// 			}
		// 		}

		// 		if (_cells[y, x])
		// 		{
		// 			if (neighbors == 2 || neighbors == 3)
		// 			{
		// 				newCells[y, x] = true;
		// 			}
		// 		}
		// 		else
		// 		{
		// 			if (neighbors == 3)
		// 			{
		// 				newCells[y, x] = true;
		// 			}
		// 		}
		// 	}
		// }

		// _cells = newCells;

		_shader.Step();
		// for (var y = 0; y < Height; y++)
		// {
		// 	for (var x = 0; x < Width; x++)
		// 	{
		// 		_cells[y, x] = _shader[y, x];
		// 	}
		// }
	}

	public void Randomize()
	{
		var random = new Random();

		for (var y = 0; y < Height; y++)
		{
			for (var x = 0; x < Width; x++)
			{
				// _cells[y, x] = random.Next(2) == 0;
				_shader[y, x] = random.Next(2) == 0;
			}
		}
	}

	public void SetCell(Vector2 position, bool value)
	{
		var x = (int)position.X;
		var y = (int)position.Y;

		if (x < 0 || x >= Width || y < 0 || y >= Height)
		{
			return;
		}

		_shader[y, x] = value;
	}

	public bool GetCell(Vector2 position)
	{
		var x = (int)position.X;
		var y = (int)position.Y;

		if (x < 0 || x >= Width || y < 0 || y >= Height)
		{
			return false;
		}

		return _shader[y, x];
	}

	#endregion
}

class ConwayLifeState : GameState
{
	#region Fields

	private Vector2 _mousePosition = Vector2.Zero;
	private ConwayLifeSim? _sim = null;

	#endregion

	#region Constructors

	public ConwayLifeState()
		: base()
	{
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
		base.Render(rc, gameTime);

		if (_sim == null)
		{
			_sim = new ConwayLifeSim(rc.Width, rc.Height, true);
			_sim.Randomize();
		}

		_sim.Render(rc);
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