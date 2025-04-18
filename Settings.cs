// // Settings.cs

// using Newtonsoft.Json;

// namespace Critters;

// class Settings
// {
// 	public required string AssetRoot { get; set; }
// 	public required WindowSettings Window { get; set; }
// 	public required VirtualDisplaySettings VirtualDisplay { get; set; }

// 	public class WindowSettings
// 	{
// 		public int Width { get; set; }
// 		public int Height { get; set; }
// 		public required string Title { get; set; }
// 	}

// 	public class VirtualDisplaySettings
// 	{
// 		public int Width { get; set; }
// 		public int Height { get; set; }
// 		public required string VertexShaderPath { get; set; }
// 		public required string FragmentShaderPath { get; set; }
// 	}

// 	public static Settings Load(string filePath)
// 	{
// 		return JsonConvert.DeserializeObject<Settings>(
// 			File.ReadAllText(filePath)
// 		) ?? throw new NullReferenceException($"Failed to load settings from '{filePath}'.");
// 	}
// }
