#version 430 core

// Define work group size
layout(local_size_x = 16, local_size_y = 16) in;

// Input parameters
layout(std140, binding = 0) uniform NoiseParams {
    float seed;       // Noise seed
    float scale;      // Scale factor for noise
    int width;        // Output texture width
    int height;       // Output texture height
    int octaves;      // Number of octaves for fractal noise
    float persistence; // Persistence value for fractal noise
};

// Output texture
layout(binding = 0, r32f) uniform image2D noiseTexture;

// Constants from your C# implementation
const float SQRT3 = 1.7320508075688772;
const float F2 = 0.5 * (SQRT3 - 1.0);
const float G2 = (3.0 - SQRT3) / 6.0;

// 2D gradient directions
const vec2 grad2[8] = vec2[8](
    vec2(1.0, 0.0), vec2(-1.0, 0.0), vec2(0.0, 1.0), vec2(0.0, -1.0),
    vec2(1.0, 1.0), vec2(-1.0, 1.0), vec2(1.0, -1.0), vec2(-1.0, -1.0)
);

// Hash function to replace the permutation table
int hash(int x, int seedValue) {
    x += seedValue;
    x = ((x >> 16) ^ x) * 0x45d9f3b;
    x = ((x >> 16) ^ x) * 0x45d9f3b;
    x = (x >> 16) ^ x;
    return x & 255;
}

// Fast floor function
int fastFloor(float x) {
    return x > 0.0 ? int(x) : int(x) - 1;
}

// Dot product of a gradient and vector
float dotGrad(vec2 g, float x, float y) {
    return g.x * x + g.y * y;
}

// 2D Simplex noise function
float noise(float x, float y) {
    // Use the seed to slightly offset coordinates for variation
    int seedValue = int(seed * 32767.0);
    
    // Noise contributions from the three corners
    float n0, n1, n2;

    // Skew the input space to determine which simplex cell we're in
    float s = (x + y) * F2;
    int i = fastFloor(x + s);
    int j = fastFloor(y + s);

    float t = (i + j) * G2;
    // Unskew the cell origin back to (x,y) space
    float X0 = float(i) - t;
    float Y0 = float(j) - t;
    // The x,y distances from the cell origin
    float x0 = x - X0;
    float y0 = y - Y0;

    // For the 2D case, the simplex shape is an equilateral triangle.
    // Determine which simplex we are in.
    int i1, j1; // Offsets for second (middle) corner of simplex in (i,j) coords
    if (x0 > y0) { 
        i1 = 1; 
        j1 = 0; 
    } else { 
        i1 = 0; 
        j1 = 1; 
    }

    // Offsets for corners in (x,y) unskewed coords
    float x1 = x0 - float(i1) + G2;
    float y1 = y0 - float(j1) + G2;
    float x2 = x0 - 1.0 + 2.0 * G2;
    float y2 = y0 - 1.0 + 2.0 * G2;

    // Work out the hashed gradient indices of the three simplex corners
    int ii = i & 255;
    int jj = j & 255;
    
    // Using hash function instead of perm table
    int gi0 = hash(ii + hash(jj, seedValue), seedValue) % 8;
    int gi1 = hash(ii + i1 + hash(jj + j1, seedValue), seedValue) % 8;
    int gi2 = hash(ii + 1 + hash(jj + 1, seedValue), seedValue) % 8;

    // Calculate the contribution from the three corners
    float t0 = 0.5 - x0*x0 - y0*y0;
    if (t0 < 0.0) {
        n0 = 0.0;
    } else {
        t0 *= t0;
        n0 = t0 * t0 * dotGrad(grad2[gi0], x0, y0);
    }

    float t1 = 0.5 - x1*x1 - y1*y1;
    if (t1 < 0.0) {
        n1 = 0.0;
    } else {
        t1 *= t1;
        n1 = t1 * t1 * dotGrad(grad2[gi1], x1, y1);
    }

    float t2 = 0.5 - x2*x2 - y2*y2;
    if (t2 < 0.0) {
        n2 = 0.0;
    } else {
        t2 *= t2;
        n2 = t2 * t2 * dotGrad(grad2[gi2], x2, y2);
    }

    // Add contributions from each corner to get the final noise value.
    // The result is scaled to return values in the interval [-1,1].
    return 70.0 * (n0 + n1 + n2);
}

// Multi-octave version for richer noise patterns
float fractalNoise(float x, float y, int octaves, float persistence) {
    float total = 0.0;
    float frequency = 1.0;
    float amplitude = 1.0;
    float maxValue = 0.0;
    
    for (int i = 0; i < octaves; i++) {
        total += noise(x * frequency, y * frequency) * amplitude;
        maxValue += amplitude;
        amplitude *= persistence;
        frequency *= 2.0;
    }
    
    return total / maxValue;
}

void main() {
    // Get the current pixel's position from global work group
    ivec2 pixelCoords = ivec2(gl_GlobalInvocationID.xy);
    
    // Check if the pixel is within the texture bounds
    if (pixelCoords.x >= width || pixelCoords.y >= height) {
        return;
    }

    // Convert to normalized coordinates [0, 1]
    float nx = float(pixelCoords.x) / float(width);
    float ny = float(pixelCoords.y) / float(height);
    
    // Apply scaling and generate noise
    float noiseValue = fractalNoise(nx * scale, ny * scale, octaves, persistence);
    
    // Normalize from [-1, 1] to [0, 1] for storage
    noiseValue = noiseValue * 0.5 + 0.5;
    
    // Write the noise value to the output texture
    imageStore(noiseTexture, pixelCoords, vec4(noiseValue, 0.0, 0.0, 0.0));
}