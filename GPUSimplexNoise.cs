using OpenTK.Graphics.OpenGL4;

namespace Critters;

public class GPUSimplexNoise
{
	private int _computeShader;
	private int _program;
	private int _noiseTexture;
	private int _uniformBuffer;
	
	private int _width;
	private int _height;
	
	public GPUSimplexNoise(int width, int height)
	{
		_width = width;
		_height = height;
		
		InitializeShaders();
		InitializeTexture();
		InitializeUniformBuffer();
	}
	
	private void InitializeShaders()
	{
		// Load shader code from file or resource.
		var shaderSource = LoadShaderSource(); // Implement this to load your shader code
		
		// Create compute shader.
		_computeShader = GL.CreateShader(ShaderType.ComputeShader);
		GL.ShaderSource(_computeShader, shaderSource);
		GL.CompileShader(_computeShader);
		
		// Check for compilation errors.
		GL.GetShader(_computeShader, ShaderParameter.CompileStatus, out int status);
		if (status != 1)
		{
			var info = GL.GetShaderInfoLog(_computeShader);
			throw new Exception($"Compute shader compilation failed: {info}");
		}
		
		// Create program.
		_program = GL.CreateProgram();
		GL.AttachShader(_program, _computeShader);
		GL.LinkProgram(_program);
		
		// Check for linking errors.
		GL.GetProgram(_program, GetProgramParameterName.LinkStatus, out status);
		if (status != 1)
		{
			var info = GL.GetProgramInfoLog(_program);
			throw new Exception($"Program linking failed: {info}");
		}
	}
	
	private void InitializeTexture()
	{
		// Generate texture.
		_noiseTexture = GL.GenTexture();
		GL.BindTexture(TextureTarget.Texture2D, _noiseTexture);
		
		// Set texture parameters.
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
		
		// Allocate texture storage (R32F format).
		GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R32f, _width, _height, 0, PixelFormat.Red, PixelType.Float, IntPtr.Zero);
		
		// Bind texture as image for compute shader.
		GL.BindImageTexture(0, _noiseTexture, 0, false, 0, TextureAccess.WriteOnly, SizedInternalFormat.R32f);
	}
	
	private void InitializeUniformBuffer()
	{
		// Create uniform buffer object.
		_uniformBuffer = GL.GenBuffer();
		GL.BindBuffer(BufferTarget.UniformBuffer, _uniformBuffer);
		
		// Allocate space for the buffer (6 floats: seed, scale, width, height, octaves, persistence).
		GL.BufferData(BufferTarget.UniformBuffer, 6 * sizeof(float), IntPtr.Zero, BufferUsageHint.DynamicDraw);
		
		// Bind buffer to binding point 0.
		GL.BindBufferBase(BufferRangeTarget.UniformBuffer, 0, _uniformBuffer);
	}
	
	public void GenerateNoise(float seed, float scale, int octaves, float persistence)
	{
		// Bind uniform buffer and update parameters.
		GL.BindBuffer(BufferTarget.UniformBuffer, _uniformBuffer);
		
		float[] parameters = [ 
			seed, 
			scale, 
			_width, 
			_height, 
			octaves, 
			persistence,
		];
		
		GL.BufferSubData(BufferTarget.UniformBuffer, IntPtr.Zero, parameters.Length * sizeof(float), parameters);
		
		// Use compute shader program.
		GL.UseProgram(_program);
		
		// Dispatch compute shader.
		var groupSizeX = (_width + 15) / 16;  // Ceiling division to ensure all pixels are covered.
		var groupSizeY = (_height + 15) / 16; // Our local work group size is 16x16.
		
		GL.DispatchCompute(groupSizeX, groupSizeY, 1);
		
		// Wait for compute shader to finish.
		GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);
	}
	
	public float[] GetNoiseData()
	{
		// Allocate array for storing the noise values.
		var noiseData = new float[_width * _height];
		
		// Bind texture
		GL.BindTexture(TextureTarget.Texture2D, _noiseTexture);
		
		// Get pixel data
		GL.GetTexImage(TextureTarget.Texture2D, 0, PixelFormat.Red, PixelType.Float, noiseData);
		
		return noiseData;
	}
	
	public int GetNoiseTexture()
	{
		return _noiseTexture;
	}
	
	public void Resize(int width, int height)
	{
		_width = width;
		_height = height;
		
		// Recreate texture with new dimensions
		GL.BindTexture(TextureTarget.Texture2D, _noiseTexture);
		GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R32f, _width, _height, 0, PixelFormat.Red, PixelType.Float, IntPtr.Zero);
	}
	
	private string LoadShaderSource()
	{
		// Implement this to load your shader code from a file or embedded resource
		// For now, just return the compute shader code as a string

		// return @"#version 430 core
		
		// // Add your compute shader code here
		// // ...
		
		// ";

		return File.ReadAllText("assets/shaders/simplex.glsl");
	}
	
	public void Dispose()
	{
		GL.DeleteProgram(_program);
		GL.DeleteShader(_computeShader);
		GL.DeleteTexture(_noiseTexture);
		GL.DeleteBuffer(_uniformBuffer);
	}
}