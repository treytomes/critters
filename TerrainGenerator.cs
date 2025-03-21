using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using System;

namespace Critters;

public class TerrainGenerator
{
    private GPUSimplexNoise _noiseGenerator;
    private int _terrainWidth;
    private int _terrainHeight;
    
    // Terrain parameters
    private float _seed = 42.0f;
    private float _scale = 25.0f;
    private int _octaves = 6;
    private float _persistence = 0.5f;
    
    // Terrain thresholds for different biomes/tiles
    private const float WATER_THRESHOLD = 0.4f;
    private const float SAND_THRESHOLD = 0.45f;
    private const float GRASS_THRESHOLD = 0.75f;
    private const float MOUNTAIN_THRESHOLD = 0.85f;
    
    public TerrainGenerator(int width, int height)
    {
			_terrainWidth = width;
			_terrainHeight = height;
			_noiseGenerator = new GPUSimplexNoise(width, height);
    }
    
    public void GenerateTerrain()
    {
        // Generate noise on the GPU
        _noiseGenerator.GenerateNoise(_seed, _scale, _octaves, _persistence);
        
        // Get the noise data
        float[] noiseData = _noiseGenerator.GetNoiseData();
        
        // Convert noise to terrain tiles
        for (int y = 0; y < _terrainHeight; y++)
        {
            for (int x = 0; x < _terrainWidth; x++)
            {
                int index = y * _terrainWidth + x;
                float noiseValue = noiseData[index];
                
                // Determine terrain type based on noise value
                TerrainType terrainType = GetTerrainType(noiseValue);
                
                // Set tile in your world (implement this according to your game structure)
                SetTile(x, y, terrainType);
            }
        }
    }
    
    private TerrainType GetTerrainType(float noiseValue)
    {
        if (noiseValue < WATER_THRESHOLD)
            return TerrainType.Water;
        if (noiseValue < SAND_THRESHOLD)
            return TerrainType.Sand;
        if (noiseValue < GRASS_THRESHOLD)
            return TerrainType.Grass;
        if (noiseValue < MOUNTAIN_THRESHOLD)
            return TerrainType.Hill;
        return TerrainType.Mountain;
    }
    
    private void SetTile(int x, int y, TerrainType terrainType)
    {
        // Implementation depends on your game's tile system
        // For example:
        // world.SetTile(x, y, terrainType);
    }
    
    public void UpdateTerrainParameters(float seed, float scale, int octaves, float persistence)
    {
        _seed = seed;
        _scale = scale;
        _octaves = octaves;
        _persistence = persistence;
    }
    
    public void Resize(int width, int height)
    {
        _terrainWidth = width;
        _terrainHeight = height;
        _noiseGenerator.Resize(width, height);
    }
}

public enum TerrainType
{
    Water,
    Sand,
    Grass,
    Hill,
    Mountain
}