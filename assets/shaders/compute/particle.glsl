#version 430 core

struct Particle {
    vec2 position;
    vec2 velocity;
    vec2 acceleration;

    // 2 floats of padding to align color to a 16-byte boundary.
    float padding0;
    float padding1;

    vec4 color;
    float lifetime;
    float size;

    // 2 floats of padding to align the struct to a multiple of 16-bytes.
    float padding2;
    float padding3;
};

// Define work group size - best to use multiples of 32 or 64 for particles
layout(local_size_x = 128, local_size_y = 1, local_size_z = 1) in;

// Input buffer containing current particle states
layout(std430, binding = 0) buffer ParticleBuffer {
    Particle particles[];
};

// Output buffer for next particle states
layout(std430, binding = 1) buffer NextParticleBuffer {
    Particle nextParticles[];
};

// Simulation parameters
uniform int numParticles;
uniform float deltaTime;
uniform vec2 gravity;
uniform vec2 bounds;
uniform vec2 attractor;
uniform float attractorStrength;
uniform float dampingFactor;
uniform float bounceEnergyLoss;
uniform float maxAcceleration;
uniform float maxForce;
uniform float minAttractorDistance;

// Simple hash function for pseudo-randomness
float hash(float n) {
    return fract(sin(n) * 43758.5453123); 
}

// Generate a random float between min and max using the seed
float randomFloat(float seed, float min, float max) {
    return min + hash(seed) * (max - min);
}

// Reset a particle with new random values
void resetParticle(uint index) {
    float seed = float(index) + deltaTime * 1000.0;
    
    // Position - start in middle with randomness
    vec2 position = vec2(
        bounds.x * (0.5 + 0.2 * hash(seed + 1.0)),
        bounds.y * (0.5 + 0.2 * hash(seed + 2.0))
    );
    
    // Velocity - random spread
    vec2 velocity = vec2(
        (hash(seed + 3.0) * 2.0 - 1.0) * 200.0,
        -(50.0 + hash(seed + 4.0) * 150.0) * 2.0
    );
    
    // Color - initial random color
    vec4 color = vec4(
        hash(seed + 5.0),
        hash(seed + 6.0),
        hash(seed + 7.0),
        1.0
    );
    
    // Lifetime and size
    float lifetime = 8.0 + hash(seed + 8.0) * 7.0;
    float size = 1.0 + hash(seed + 9.0) * 2.0;
    
    // Update the particle
    nextParticles[index].position = position;
    nextParticles[index].velocity = velocity;
    nextParticles[index].acceleration = vec2(0.0);
    nextParticles[index].color = color;
    nextParticles[index].lifetime = lifetime;
    nextParticles[index].size = size;
}

void main() {
    uint index = gl_GlobalInvocationID.x;
    
    // Skip if beyond the particle count.
    if (index >= numParticles) {
        return;
    }
    
    // Read current particle
    Particle particle = particles[index];

    // Reset dead particles directly in the shader
    if (particle.lifetime <= 0.0) {
        resetParticle(index);
        return;
    }
    
    // Calculate new acceleration
    vec2 acceleration = gravity;
    
    // Add attractor force (without branching)
    vec2 direction = attractor - particle.position;
    float distance = max(length(direction), minAttractorDistance);
    direction = normalize(direction);
    
    // Force proportional to 1/distance^2 (multiplied by attractorStrength which can be 0)
    float clampedDistance = max(minAttractorDistance, distance);
    vec2 attractorForce = direction * clamp(attractorStrength / (clampedDistance * clampedDistance), -maxForce, maxForce);
    acceleration += attractorForce;
    
    // Clamp maximum acceleration to prevent instability
    float accelMagnitude = length(acceleration);
    if (accelMagnitude > maxAcceleration) {
        acceleration = normalize(acceleration) * maxAcceleration;
    }
    
    // Update velocity with acceleration
    particle.velocity += acceleration * deltaTime;
    
    // Add damping to prevent infinite acceleration
    particle.velocity *= dampingFactor;
    
    // Update position with velocity
    particle.position += particle.velocity * deltaTime;
    
    // Boundary collision - bounce with energy loss
    if (particle.position.x < 0.0) {
        particle.position.x = 0.0;
        particle.velocity.x = -particle.velocity.x * bounceEnergyLoss;
    } else if (particle.position.x > bounds.x) {
        particle.position.x = bounds.x;
        particle.velocity.x = -particle.velocity.x * bounceEnergyLoss;
    }
    
    if (particle.position.y < 0.0) {
        particle.position.y = 0.0;
        particle.velocity.y = -particle.velocity.y * bounceEnergyLoss;
    } else if (particle.position.y > bounds.y) {
        particle.position.y = bounds.y;
        particle.velocity.y = -particle.velocity.y * bounceEnergyLoss;
    }
    
    // Update lifetime
    particle.lifetime = max(0.0, particle.lifetime - deltaTime);

    // Color shift based on speed (blue->green->red as speed increases)
    float speed = length(particle.velocity);
    particle.color = vec4(
        min(1.0, speed / 300.0),                  // Red increases with speed
        min(1.0, 150.0 / (100.0 + speed)),        // Green highest at medium speed
        min(1.0, 100.0 / (50.0 + speed)),         // Blue highest at low speed
        min(1.0, particle.lifetime / 5.0)         // Alpha fades out as lifetime decreases
    );

    // Write to output buffer
    nextParticles[index] = particle;
}