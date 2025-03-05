using OpenTK.Mathematics;

namespace Critters;

static class VectorExtensions
{
}

class SimplexNoise
{
	Vector4 mod289(Vector4 x)
	{
		return x - (x * (1.0f / 289.0f)).Floor() * 289.0f;
	}

	Vector3 mod289(Vector3 x)
	{
		return x - (x * (1.0f / 289.0f)).Floor() * 289.0f;
	}

	float mod289(float x)
	{
		return x - (float)Math.Floor(x * (1.0f / 289.0f)) * 289.0f;
	}

	Vector4 permute_v(Vector4 x)
	{
		return mod289(((x * 34.0f) + Vector4.One) * x);
	}

	float permute(float x)
	{
		return mod289(((x * 34.0f) + 1.0f) * x);
	}

	Vector4 taylorInvSqrt(Vector4 r)
	{
		return 1.79284291400159f * Vector4.One - 0.85373472095314f * r;
	}

	float taylorInvSqrt(float r)
	{
		return 1.79284291400159f - 0.85373472095314f * r;
	}

	Vector4 grad4(float j, Vector4 ip)
	{
		var ones = new Vector4(1.0f, 1.0f, 1.0f, -1.0f);
		var p = Vector4.Zero;
		var s = Vector4.Zero;

		p.Xyz = (j * ip.Xyz).Fract().Floor() * 7.0f * ip.Z - Vector3.One;
		p.W = 1.5f - Vector3.Dot(p.Xyz.Abs(), ones.Xyz);
		s = new Vector4(lessThan(p, Vector4.Zero));
		p.Xyz = p.Xyz + (s.Xyz * 2.0f - Vector3.One) * s.Www();

		return p;
	}

	// (sqrt(5) - 1)/4 = F4, used once below
	const float F4 = 0.309016994374947451f;

	float snoise(Vector4 v) {
		Vector4 C = new Vector4( 0.138196601125011f,  // (5 - sqrt(5))/20  G4
														 0.276393202250021f,  // 2 * G4
														 0.414589803375032f,  // 3 * G4
														-0.447213595499958f); // -1 + 4 * G4

	// First corner
		Vector4 i  = floor(v + dot(v, Vector4(F4, F4, F4, F4)));
		Vector4 x0 = v -   i + dot(i, C.xxxx);

	// Other corners

	// Rank sorting originally contributed by Bill Licea-Kane, AMD (formerly ATI)
		Vector4 i0;
		Vector3 isX = step( x0.yzw, x0.xxx );
		Vector3 isYZ = step( x0.zww, x0.yyz );
	//  i0.x = dot( isX, Vector3( 1.0 ) );
		i0.x = isX.x + isX.y + isX.z;
		i0.yzw = 1.0 - isX;
	//  i0.y += dot( isYZ.xy, Vector2( 1.0 ) );
		i0.y += isYZ.x + isYZ.y;
		i0.zw += 1.0 - isYZ.xy;
		i0.z += isYZ.z;
		i0.w += 1.0 - isYZ.z;

		// i0 now contains the unique values 0,1,2,3 in each channel taylorInvSqrt
		Vector4 i3 = clamp(i0, 0.0, 1.0);
		Vector4 i2 = clamp(i0 - 1.0, 0.0, 1.0);
		Vector4 i1 = clamp(i0 - 2.0, 0.0, 1.0);

		//  x0 = x0 - 0.0 + 0.0 * C.xxxx
		//  x1 = x0 - i1  + 1.0 * C.xxxx
		//  x2 = x0 - i2  + 2.0 * C.xxxx
		//  x3 = x0 - i3  + 3.0 * C.xxxx
		//  x4 = x0 - 1.0 + 4.0 * C.xxxx
		Vector4 x1 = x0 - i1 + C.xxxx;
		Vector4 x2 = x0 - i2 + C.yyyy;
		Vector4 x3 = x0 - i3 + C.zzzz;
		Vector4 x4 = x0 + C.wwww;

	// Permutations
		i = mod289(i);
		float j0 = permute( permute( permute( permute(i.w) + i.z) + i.y) + i.x);
		Vector4 j1 = permute_v(permute_v(permute_v(permute_v(
							i.w + Vector4(i1.w, i2.w, i3.w, 1.0 ))
						+ i.z + Vector4(i1.z, i2.z, i3.z, 1.0 ))
						+ i.y + Vector4(i1.y, i2.y, i3.y, 1.0 ))
						+ i.x + Vector4(i1.x, i2.x, i3.x, 1.0 ));

	// Gradients: 7x7x6 points over a cube, mapped onto a 4-cross polytope
	// 7*7*6 = 294, which is close to the ring size 17*17 = 289.
		Vector4 ip = Vector4(1.0/294.0, 1.0/49.0, 1.0/7.0, 0.0) ;

		Vector4 p0 = grad4(j0,   ip);
		Vector4 p1 = grad4(j1.x, ip);
		Vector4 p2 = grad4(j1.y, ip);
		Vector4 p3 = grad4(j1.z, ip);
		Vector4 p4 = grad4(j1.w, ip);

	// Normalise gradients
		Vector4 norm = taylorInvSqrt(Vector4(dot(p0,p0), dot(p1,p1), dot(p2, p2), dot(p3,p3)));
		p0 *= norm.x;
		p1 *= norm.y;
		p2 *= norm.z;
		p3 *= norm.w;
		p4 *= taylorInvSqrt(dot(p4,p4));

	// Mix contributions from the five corners
		Vector3 m0 = max(0.6 - Vector3(dot(x0,x0), dot(x1,x1), dot(x2,x2)), 0.0);
		Vector2 m1 = max(0.6 - Vector2(dot(x3,x3), dot(x4,x4)            ), 0.0);
		m0 = m0 * m0;
		m1 = m1 * m1;
		return 42.0 * ( dot(m0*m0, Vector3( dot( p0, x0 ), dot( p1, x1 ), dot( p2, x2 )))
								+ dot(m1*m1, Vector2( dot( p3, x3 ), dot( p4, x4 ) ) ) ) ;
	}

	Vector4 snoise_grad(Vector3 v)
	{
			const Vector2 C = Vector2(1.0 / 6.0, 1.0 / 3.0);

			// First corner
			Vector3 i  = floor(v + dot(v, C.yyy));
			Vector3 x0 = v   - i + dot(i, C.xxx);

			// Other corners
			Vector3 g = step(x0.yzx, x0.xyz);
			Vector3 l = 1.0 - g;
			Vector3 i1 = min(g.xyz, l.zxy);
			Vector3 i2 = max(g.xyz, l.zxy);

			// x1 = x0 - i1  + 1.0 * C.xxx;
			// x2 = x0 - i2  + 2.0 * C.xxx;
			// x3 = x0 - 1.0 + 3.0 * C.xxx;
			Vector3 x1 = x0 - i1 + C.xxx;
			Vector3 x2 = x0 - i2 + C.yyy;
			Vector3 x3 = x0 - 0.5;

			// Permutations
			i = mod289(i); // Avoid truncation effects in permutation
			Vector4 p =
				permute_v(permute_v(permute_v(i.z + Vector4(0.0, i1.z, i2.z, 1.0))
															+ i.y + Vector4(0.0, i1.y, i2.y, 1.0))
															+ i.x + Vector4(0.0, i1.x, i2.x, 1.0));

			// Gradients: 7x7 points over a square, mapped onto an octahedron.
			// The ring size 17*17 = 289 is close to a multiple of 49 (49*6 = 294)
			Vector4 j = p - 49.0 * floor(p / 49.0);  // mod(p,7*7)

			Vector4 x_ = floor(j / 7.0);
			Vector4 y_ = floor(j - 7.0 * x_);  // mod(j,N)

			Vector4 x = (x_ * 2.0 + 0.5) / 7.0 - 1.0;
			Vector4 y = (y_ * 2.0 + 0.5) / 7.0 - 1.0;

			Vector4 h = 1.0 - abs(x) - abs(y);

			Vector4 b0 = Vector4(x.xy, y.xy);
			Vector4 b1 = Vector4(x.zw, y.zw);

			//Vector4 s0 = Vector4(lessThan(b0, 0.0)) * 2.0 - 1.0;
			//Vector4 s1 = Vector4(lessThan(b1, 0.0)) * 2.0 - 1.0;
			Vector4 s0 = floor(b0) * 2.0 + 1.0;
			Vector4 s1 = floor(b1) * 2.0 + 1.0;
			Vector4 sh = -step(h, Vector4(0.0));

			Vector4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;
			Vector4 a1 = b1.xzyw + s1.xzyw * sh.zzww;

			Vector3 g0 = Vector3(a0.xy, h.x);
			Vector3 g1 = Vector3(a0.zw, h.y);
			Vector3 g2 = Vector3(a1.xy, h.z);
			Vector3 g3 = Vector3(a1.zw, h.w);

			// Normalise gradients
			Vector4 norm = taylorInvSqrt(Vector4(dot(g0, g0), dot(g1, g1), dot(g2, g2), dot(g3, g3)));
			g0 *= norm.x;
			g1 *= norm.y;
			g2 *= norm.z;
			g3 *= norm.w;

			// Compute noise and gradient at P
			Vector4 m = max(0.6 - Vector4(dot(x0, x0), dot(x1, x1), dot(x2, x2), dot(x3, x3)), 0.0);
			Vector4 m2 = m * m;
			Vector4 m3 = m2 * m;
			Vector4 m4 = m2 * m2;
			Vector3 grad =
					-6.0 * m3.x * x0 * dot(x0, g0) + m4.x * g0 +
					-6.0 * m3.y * x1 * dot(x1, g1) + m4.y * g1 +
					-6.0 * m3.z * x2 * dot(x2, g2) + m4.z * g2 +
					-6.0 * m3.w * x3 * dot(x3, g3) + m4.w * g3;
			Vector4 px = Vector4(dot(x0, g0), dot(x1, g1), dot(x2, g2), dot(x3, g3));
			return 42.0 * Vector4(grad, dot(m4, px));
	}
}