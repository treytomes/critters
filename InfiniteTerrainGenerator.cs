using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;

namespace Critters;

public class InfiniteTerrainGenerator
{
    // private GPUSimplexNoise _noiseGenerator;
    // private SimplexNoise _noiseGenerator;
    private Dictionary<Vector2i, TerrainChunk> _loadedChunks;
    
    // Chunk parameters
    private int _chunkSize = 32; // Size of each chunk in tiles
    private int _viewDistance = 5; // Number of chunks to load in each direction
    
    // Terrain parameters
    private float _seed = 42.0f;
    private float _scale = 64.0f;
    private int _octaves = 6;
    private float _persistence = 0.1f;
    
    // Terrain thresholds for different biomes/tiles
    private const float WATER_THRESHOLD = 0.19f;
    private const float SAND_THRESHOLD = 0.24f;
    private const float GRASS_THRESHOLD = 0.8f;
    private const float HILL_THRESHOLD = 0.82f;
    
    public InfiniteTerrainGenerator()
    {
        _loadedChunks = new Dictionary<Vector2i, TerrainChunk>();
        
        // // We'll create the noise generator with a fixed size of one chunk
        // _noiseGenerator = new GPUSimplexNoise(_chunkSize, _chunkSize);
				// _noiseGenerator = new SimplexNoise(42);
    }
    
    public void UpdateViewPosition(Vector2 playerPosition)
    {
        // Convert player position to chunk coordinates
        Vector2i playerChunk = new Vector2i(
            (int)Math.Floor(playerPosition.X / _chunkSize), 
            (int)Math.Floor(playerPosition.Y / _chunkSize)
        );
        
        // Determine which chunks should be loaded
        HashSet<Vector2i> chunksToKeep = new HashSet<Vector2i>();
        for (int x = -_viewDistance; x <= _viewDistance; x++)
        {
            for (int y = -_viewDistance; y <= _viewDistance; y++)
            {
                Vector2i chunkPos = new Vector2i(playerChunk.X + x, playerChunk.Y + y);
                chunksToKeep.Add(chunkPos);
                
                // Generate new chunk if it doesn't exist
                if (!_loadedChunks.ContainsKey(chunkPos))
                {
                    GenerateChunk(chunkPos);
                }
            }
        }
        
        // Unload chunks that are too far away
        List<Vector2i> chunksToRemove = new List<Vector2i>();
        foreach (var pos in _loadedChunks.Keys)
        {
            if (!chunksToKeep.Contains(pos))
            {
                chunksToRemove.Add(pos);
            }
        }
        
        foreach (var pos in chunksToRemove)
        {
            _loadedChunks.Remove(pos);
            // If your game uses a separate system for storing/rendering chunks,
            // you would unload them here
        }
    }
    
    private void GenerateChunk(Vector2i chunkPos)
    {
        // Use position-dependent seed for consistent results
        float chunkSeed = _seed + chunkPos.X * 10000.0f + chunkPos.Y * 1000.0f;
        
        // Generate noise for this chunk
        // _noiseGenerator.GenerateNoise(chunkSeed, _scale, _octaves, _persistence);
        
        // Get the noise data
        // float[] noiseData = _noiseGenerator.GetNoiseData();
        
        // Create and store the terrain chunk
        TerrainChunk chunk = new TerrainChunk(chunkPos, _chunkSize);
        
        // Convert noise to terrain tiles
        for (int localY = 0; localY < _chunkSize; localY++)
        {
            for (int localX = 0; localX < _chunkSize; localX++)
            {
								var x = chunkPos.X * _chunkSize + localX;
								var y = chunkPos.Y * _chunkSize + localY;
								// var noiseValue = _noiseGenerator.FractalNoise(chunkPos.X * _chunkSize + localX, chunkPos.Y * _chunkSize + localY, _scale, _octaves, _persistence);
                // int index = localY * _chunkSize + localX;
                // float noiseValue = noiseData[index];
								// var noiseValue = Noise.CalcPixel3D(chunkPos.X * _chunkSize + localX, chunkPos.Y * _chunkSize + localY, 0, 1.0f);

								var v1 = Noise.CalcPixel2D((int)x, (int)y, 0.001f);
								var v2 = Noise.CalcPixel2D((int)(x * 2), (int)(y * 4), 0.03f);
								var value = (v1 * 0.9f) + (v2 * 0.1f);
								value = value / 255.0f;

                // Offset the noise to avoid repeating patterns at chunk boundaries
                // We can do this by slightly varying the position input to the noise function
                // This is already handled in our compute shader by adding the chunk position to the seed
                
                // Determine terrain type based on noise value
                TerrainType terrainType = GetTerrainType(value);
                
                // Set tile in the chunk
                chunk.SetTile(localX, localY, terrainType);
            }
        }
        
        _loadedChunks[chunkPos] = chunk;
    }
    
    private TerrainType GetTerrainType(float noiseValue)
    {
        if (noiseValue < WATER_THRESHOLD)
            return TerrainType.Water;
        if (noiseValue < SAND_THRESHOLD)
            return TerrainType.Sand;
        if (noiseValue < GRASS_THRESHOLD)
            return TerrainType.Grass;
        if (noiseValue < HILL_THRESHOLD)
            return TerrainType.Hill;
        return TerrainType.Mountain;
    }
    
    public TerrainType GetTileAt(int worldX, int worldY)
    {
        // Convert world position to chunk coordinates
        Vector2i chunkPos = new Vector2i(
            (int)Math.Floor((float)worldX / _chunkSize), 
            (int)Math.Floor((float)worldY / _chunkSize)
        );
        
        // Convert world position to local coordinates within chunk
        int localX = worldX - (chunkPos.X * _chunkSize);
        int localY = worldY - (chunkPos.Y * _chunkSize);
        
        // Check if the chunk is loaded
        if (_loadedChunks.TryGetValue(chunkPos, out TerrainChunk chunk))
        {
            return chunk.GetTile(localX, localY);
        }
        
        // If chunk isn't loaded, generate it
        GenerateChunk(chunkPos);
        return _loadedChunks[chunkPos].GetTile(localX, localY);
    }
    
    public void SetChunkSize(int size)
    {
        _chunkSize = size;
        // _noiseGenerator.Resize(size, size);
        _loadedChunks.Clear();
    }
    
    public void SetViewDistance(int distance)
    {
        _viewDistance = distance;
    }
    
    public void UpdateTerrainParameters(float seed, float scale, int octaves, float persistence)
    {
        _seed = seed;
        _scale = scale;
        _octaves = octaves;
        _persistence = persistence;
        
        // Clear all chunks to regenerate with new parameters
        _loadedChunks.Clear();
    }
}

public class TerrainChunk
{
    private Vector2i _position;
    private int _size;
    private TerrainType[,] _tiles;
    
    public Vector2i Position => _position;
    
    public TerrainChunk(Vector2i position, int size)
    {
        _position = position;
        _size = size;
        _tiles = new TerrainType[size, size];
    }
    
    public void SetTile(int x, int y, TerrainType type)
    {
        if (x >= 0 && x < _size && y >= 0 && y < _size)
        {
            _tiles[x, y] = type;
        }
    }
    
    public TerrainType GetTile(int x, int y)
    {
        if (x >= 0 && x < _size && y >= 0 && y < _size)
        {
            return _tiles[x, y];
        }
        return TerrainType.Water; // Default for out of bounds
    }
}