// CommandLineOptions.cs

namespace Critters;  
  
public class CommandLineOptions  
{  
    public string? ConfigFile { get; set; }  
    public bool Debug { get; set; }  
    public bool Fullscreen { get; set; }  
    public int? WindowWidth { get; set; }  
    public int? WindowHeight { get; set; }  
}