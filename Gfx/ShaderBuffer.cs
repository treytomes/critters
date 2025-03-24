using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;

namespace Critters.Gfx;

class ShaderBuffer<T> : IDisposable
	where T : struct
{
	#region Constants

	private const BufferTarget TARGET_TYPE = BufferTarget.ShaderStorageBuffer;
	private const BufferRangeTarget RANGE_TARGET_TYPE = BufferRangeTarget.ShaderStorageBuffer;

	#endregion

	#region Fields

	public readonly int Handle;
	private bool _disposedValue;

	/// <summary>
	/// The size of the buffer in bytes.
	/// </summary>
	private readonly int _size;

	#endregion

	#region Constructors

	public ShaderBuffer(int count, int baseIndex)
	{
		Count = count;
		_size = count * Marshal.SizeOf<T>();
		GL.GenBuffers(1, out Handle);
		
		Bind();
		GL.BufferData(TARGET_TYPE, _size, IntPtr.Zero, BufferUsageHint.DynamicCopy);
		BaseIndex = baseIndex;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Bind the buffer object to an indexed buffer target.
	/// </summary>
	public int BaseIndex
	{
		set
		{
			GL.BindBufferBase(RANGE_TARGET_TYPE, value, Handle);
		}
	}

	/// <summary>
	/// The number of data elements.
	/// </summary>
	public int Count { get; }

	#endregion

	#region Methods

	public void Bind()
	{
		GL.BindBuffer(TARGET_TYPE, Handle);
	}

	public void Set(T[] data) 
	{
		if (data.Length != Count)
		{
			throw new ArgumentException("Data length does not match buffer size", nameof(data));
		}

		Bind();
		GL.BufferSubData(TARGET_TYPE, IntPtr.Zero, _size, data);
	}

	public T[] Get()
	{
		Bind();
		var data = new T[Count];
		GL.GetBufferSubData(TARGET_TYPE, IntPtr.Zero, _size, data);
		return data;
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
			}

			GL.DeleteBuffer(Handle);
			_disposedValue = true;
		}
	}

	~ShaderBuffer()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: false);
	}

	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	#endregion
}