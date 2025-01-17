using System.IO.Pipelines;
using System.Text.Json;

class Settings
{
    public required WindowSettings Window { get; set; }

    public class WindowSettings
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public required string Title { get; set; }
    }

    public static Settings Load(string filePath)
    {
        return JsonSerializer.Deserialize<Settings>(
					File.ReadAllText(filePath),
					new JsonSerializerOptions() {
						PropertyNameCaseInsensitive = true,
					}
				) ?? throw new NullReferenceException($"Failed to load settings from '{filePath}'.");
    }
}
