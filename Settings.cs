using System.IO.Pipelines;
using System.Text.Json;

class Settings
{
    public required WindowSettings Window { get; set; }
    public required VirtualDisplaySettings VirtualDisplay { get; set; }

    public class WindowSettings
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public required string Title { get; set; }
    }

    public class VirtualDisplaySettings
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public required string VertexShaderPath { get; set; }
        public required string FragmentShaderPath { get; set; }
    }

    public static Settings Load(string filePath)
    {
        return JsonSerializer.Deserialize<Settings>(
            File.ReadAllText(filePath)
        ) ?? throw new NullReferenceException($"Failed to load settings from '{filePath}'.");
    }
}
