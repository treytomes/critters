#version 330 core

#define PALETTE_SIZE 256.0

in vec2 vTexCoord;
out vec4 FragColor;

uniform sampler2D uTexture; // Holds palette indices
uniform sampler2D uPalette; // Holds the RGB palette (256 colors)

void main()
{
    // Fetch the index from the red channel of uTexture
    float index = texture(uTexture, vTexCoord).r * (PALETTE_SIZE - 1.0);

    // Map the index to the palette's texture coordinates
    vec2 paletteCoord = vec2((index + 0.5) / PALETTE_SIZE, 0.5);

    // Fetch the actual color from the palette
    FragColor = texture(uPalette, paletteCoord);
}