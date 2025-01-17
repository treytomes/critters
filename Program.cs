using Critters.Gfx;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace Critters
{
  class Program
  {
    static void Main(string[] args)
    {
      // Load settings
      var settings = Settings.Load("assets/settings.json");

      var nativeWindowSettings = new NativeWindowSettings()
      {
          ClientSize = new Vector2i(settings.Window.Width, settings.Window.Height),
          Title = settings.Window.Title,
      };

      using var window = new GameWindow(GameWindowSettings.Default, nativeWindowSettings);
      var vertexShaderSource = File.ReadAllText("assets/shaders/vertex.glsl");
      var fragmentShaderSource = File.ReadAllText("assets/shaders/fragment.glsl");
      var virtualDisplay = new VirtualDisplay(settings.VirtualDisplay.Width, settings.VirtualDisplay.Height, vertexShaderSource, fragmentShaderSource);

      // Example: Fill with a test pattern
      var palette = new Palette();
      var rc = new RenderingContext(virtualDisplay);
      for (int y = 0; y < settings.VirtualDisplay.Height; y++)
      {
        for (int x = 0; x < settings.VirtualDisplay.Width; x++)
        {
          var r = (x | y) % 6;
          var g = (x ^ y) % 6;
          var b = (x & y) % 6;
          rc.SetPixel(x, y, palette[r, g, b]);
        }
      }

      window.RenderFrame += (FrameEventArgs e) =>
      {
        rc.Present();
        virtualDisplay.Render(window.Size); // Render the virtual display
        window.SwapBuffers();
      };

      window.Resize += (ResizeEventArgs e) =>
      {
        // Update the window size for the virtual display
        virtualDisplay.Render(new Vector2i(e.Width, e.Height));
      };

      window.Run();
    }
  }
}
