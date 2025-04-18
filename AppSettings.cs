// AppSettings.cs  
namespace Critters;  
  
public class AppSettings  
{  
    public string AssetRoot { get; set; } = string.Empty;  
    public bool Debug { get; set; }  
    public WindowSettings Window { get; set; } = new();  
    public VirtualDisplaySettings VirtualDisplay { get; set; } = new();  
}  
  
public class WindowSettings  
{  
    public int Width { get; set; }  
    public int Height { get; set; }  
    public string Title { get; set; } = string.Empty;  
    public bool Fullscreen { get; set; }  
}  
  
public class VirtualDisplaySettings  
{  
    public int Width { get; set; }  
    public int Height { get; set; }  
    public string VertexShaderPath { get; set; } = string.Empty;  
    public string FragmentShaderPath { get; set; } = string.Empty;  
}  