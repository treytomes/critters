using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

class Program
{
  private const string PATH_SETTINGS = "settings.json";
  
  static void Main(string[] args)
  {
    var settings = Settings.Load(PATH_SETTINGS);

    var nativeWindowSettings = new NativeWindowSettings()
    {
      ClientSize = new OpenTK.Mathematics.Vector2i(settings.Window.Width, settings.Window.Height),
      Title = settings.Window.Title,
    };

    using var window = new GameWindow(GameWindowSettings.Default, nativeWindowSettings);

    window.RenderFrame += (FrameEventArgs args) =>
    {
      // Rendering logic (e.g., drawing tiles)
      window.SwapBuffers();
    };

    window.UpdateFrame += (FrameEventArgs args) =>
    {
      // Game update logic (e.g., processing input, updating game state)
    };

    window.Run();
  }
}