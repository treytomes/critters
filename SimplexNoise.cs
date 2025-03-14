public class SimplexNoise
{
    private const int PERM_SIZE = 512;
    private readonly int[] perm = new int[PERM_SIZE];

    private const float SQRT3 = 1.7320508075688772f;
    private const float F2 = 0.5f * (SQRT3 - 1.0f);
    private const float G2 = (3.0f - SQRT3) / 6.0f;

    // Gradients for 2D. They approximate the directions to the
    // vertices of a hexagon from the center.
    private static readonly int[][] grad2 = new int[][] {
        new int[] { 1, 0 }, new int[] { -1, 0 }, new int[] { 0, 1 }, new int[] { 0, -1 },
        new int[] { 1, 1 }, new int[] { -1, 1 }, new int[] { 1, -1 }, new int[] { -1, -1 }
    };

    public SimplexNoise(int seed = 0)
    {
        InitializePerm(seed);
    }

    private void InitializePerm(int seed)
    {
        Random rnd = new Random(seed);
        
        // Initialize with values 0...255
        for (int i = 0; i < 256; i++)
        {
            perm[i] = i;
        }

        // Shuffle
        for (int i = 0; i < 255; i++)
        {
            int r = rnd.Next(i, 256);
            int temp = perm[i];
            perm[i] = perm[r];
            perm[r] = temp;
        }

        // Duplicate to avoid buffer overflow
        for (int i = 0; i < 256; i++)
        {
            perm[i + 256] = perm[i];
        }
    }

    private float Dot(int[] g, float x, float y)
    {
        return g[0] * x + g[1] * y;
    }

    // 2D Simplex noise
    public float Noise(float x, float y)
    {
        // Noise contributions from the three corners
        float n0, n1, n2;

        // Skew the input space to determine which simplex cell we're in
        float s = (x + y) * F2; // Hairy factor for 2D
        int i = FastFloor(x + s);
        int j = FastFloor(y + s);

        float t = (i + j) * G2;
        // Unskew the cell origin back to (x,y) space
        float X0 = i - t;
        float Y0 = j - t;
        // The x,y distances from the cell origin
        float x0 = x - X0;
        float y0 = y - Y0;

        // For the 2D case, the simplex shape is an equilateral triangle.
        // Determine which simplex we are in.
        int i1, j1; // Offsets for second (middle) corner of simplex in (i,j) coords
        if (x0 > y0) { i1 = 1; j1 = 0; } // lower triangle, XY order: (0,0)->(1,0)->(1,1)
        else { i1 = 0; j1 = 1; }      // upper triangle, YX order: (0,0)->(0,1)->(1,1)

        // A step of (1,0) in (i,j) means a step of (1-c,-c) in (x,y), and
        // a step of (0,1) in (i,j) means a step of (-c,1-c) in (x,y), where
        // c = (3-sqrt(3))/6
        float x1 = x0 - i1 + G2; // Offsets for middle corner in (x,y) unskewed coords
        float y1 = y0 - j1 + G2;
        float x2 = x0 - 1.0f + 2.0f * G2; // Offsets for last corner in (x,y) unskewed coords
        float y2 = y0 - 1.0f + 2.0f * G2;

        // Work out the hashed gradient indices of the three simplex corners
        int ii = i & 255;
        int jj = j & 255;
        int gi0 = perm[ii + perm[jj]] % 8;
        int gi1 = perm[ii + i1 + perm[jj + j1]] % 8;
        int gi2 = perm[ii + 1 + perm[jj + 1]] % 8;

        // Calculate the contribution from the three corners
        float t0 = 0.5f - x0 * x0 - y0 * y0;
        if (t0 < 0) n0 = 0.0f;
        else
        {
            t0 *= t0;
            n0 = t0 * t0 * Dot(grad2[gi0], x0, y0); // (x,y) of grad3 used for 2D gradient
        }

        float t1 = 0.5f - x1 * x1 - y1 * y1;
        if (t1 < 0) n1 = 0.0f;
        else
        {
            t1 *= t1;
            n1 = t1 * t1 * Dot(grad2[gi1], x1, y1);
        }

        float t2 = 0.5f - x2 * x2 - y2 * y2;
        if (t2 < 0) n2 = 0.0f;
        else
        {
            t2 *= t2;
            n2 = t2 * t2 * Dot(grad2[gi2], x2, y2);
        }

        // Add contributions from each corner to get the final noise value.
        // The result is scaled to return values in the interval [-1,1].
        return 70.0f * (n0 + n1 + n2);
    }

    // Multi-octave version for richer noise patterns
    public float FractalNoise(float x, float y, int octaves, float persistence)
    {
        float total = 0;
        float frequency = 1;
        float amplitude = 1;
        float maxValue = 0;
        
        for (int i = 0; i < octaves; i++)
        {
            total += Noise(x * frequency, y * frequency) * amplitude;
            maxValue += amplitude;
            amplitude *= persistence;
            frequency *= 2;
        }
        
        return total / maxValue;
    }

    private int FastFloor(float x)
    {
        return x > 0 ? (int)x : (int)x - 1;
    }
}