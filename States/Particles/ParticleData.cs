using System.Runtime.InteropServices;
using OpenTK.Mathematics;

namespace Critters.States.Particles;

[StructLayout(LayoutKind.Sequential)]
struct ParticleData
{
		/// <remarks>
		/// +8 bytes = 8 bytes
		/// </remarks>
    public Vector2 Position;

		/// <remarks>
		/// +8 bytes = 16 bytes
		/// </remarks>
		public Vector2 Velocity;

		/// <remarks>
		/// +8 bytes = 24 bytes
		/// </remarks>
    public Vector2 Acceleration;
    
		/// <summary>
		/// 2 floats of padding to align the color vec4 to a 16-byte boundary.
		/// </summary>
		/// <remarks>
		/// +4 bytes = 28 bytes
		/// </remarks>
		float padding0;

		/// <remarks>
		/// +4 bytes = 32 bytes
		/// </remarks>
    float padding1;
		
		/// <remarks>
		/// +16 bytes = 48 bytes
		/// </remarks>
    public Vector4 Color;

		/// <remarks>
		/// +4 bytes = 52 bytes
		/// </remarks>
    public float Lifetime;

		/// <remarks>
		/// +4 bytes = 56 bytes
		/// </remarks>
    public float Size;

		/// <summary>
		/// Add explicit padding if needed to align struct size to a multiple of 16.
		/// </summary>
		/// <remarks>
		/// +4 bytes = 60 bytes
		/// </remarks>
    float padding2;

		/// <remarks>
		/// +4 bytes = 64 bytes
		/// </remarks>
    float padding3;
}
