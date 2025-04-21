#version 430 core

// Define work group size.
layout(local_size_x = 16, local_size_y = 16, local_size_z = 1) in;

// Input buffer containing current cell states (1.0 = alive, 0.0 = dead).
layout(std430, binding = 0) buffer CellBuffer {
    vec4 cells[];
};

// Output buffer for next generation.
layout(std430, binding = 1) buffer NextGenBuffer {
    vec4 nextGen[];
};

// Grid dimensions as uniform variables.
uniform int width;
uniform int height;
uniform bool wrapEdges;

// Helper function to get cell state (1.0 = alive, 0.0 = dead).
float getCellState(int x, int y) {
    // Handle edges according to configuration
    if (wrapEdges) {
        // Wrap around edges (toroidal grid)
        x = (x + width) % width;
        y = (y + height) % height;
    } else {
        // Check bounds
        if (x < 0 || x >= width || y < 0 || y >= height) {
            return 0.0; // Out of bounds cells are dead
        }
    }
    
    // Calculate buffer index.
    uint index = uint(x + y * width);
    
    // The cell state is stored in the x component of the vec4.
    return cells[index].x;
}

void main() {
    // Get global position.
    uint x = gl_GlobalInvocationID.x;
    uint y = gl_GlobalInvocationID.y;
    
    // Skip computation if outside grid bounds.
    if (x >= width || y >= height) {
        return;
    }
    
    // Calculate index in the buffer.
    uint index = x + y * width;
    
    // Get current cell state.
    float currentState = getCellState(int(x), int(y));
    
    // Count live neighbors.
    float liveNeighbors = 0.0;
    for (int dy = -1; dy <= 1; dy++) {
        for (int dx = -1; dx <= 1; dx++) {
            // Skip the cell itself.
            if (dx == 0 && dy == 0) continue;
            
            // Add the state of this neighbor.
            liveNeighbors += getCellState(int(x) + dx, int(y) + dy);
        }
    }
    
    // Apply Conway's Game of Life rules.
    float newState = 0.0;
    
    if (currentState > 0.5) {
        // Cell is currently alive.
        if (liveNeighbors < 2.0 || liveNeighbors > 3.0) {
            // Dies from underpopulation or overpopulation.
            newState = 0.0;
        } else {
            // Stays alive.
            newState = 1.0;
        }
    } else {
        // Cell is currently dead.
        if (liveNeighbors == 3.0) {
            // Becomes alive through reproduction.
            newState = 1.0;
        } else {
            // Stays dead.
            newState = 0.0;
        }
    }
    
    // Update the output buffer.
    nextGen[index] = vec4(newState, 0.0, 0.0, 1.0);
}
