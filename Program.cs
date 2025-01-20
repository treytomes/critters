﻿using Critter.Gfx;
using Critters.Gfx;
using Critters.IO;
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

      var resources = new ResourceManager("assets");
      resources.Register<Image, ImageLoader>();
      
      var image = resources.Load<Image>("oem437_8.png");

      var nativeWindowSettings = new NativeWindowSettings()
      {
          ClientSize = new Vector2i(settings.Window.Width, settings.Window.Height),
          Title = settings.Window.Title,
      };

      using var window = new GameWindow(GameWindowSettings.Default, nativeWindowSettings);
      using var virtualDisplay = new VirtualDisplay(settings.VirtualDisplay);

      var palette = new Palette();
      var rc = new RenderingContext(virtualDisplay);

      // // Example: Fill with a test pattern
      // for (int y = 0; y < settings.VirtualDisplay.Height; y++)
      // {
      //   for (int x = 0; x < settings.VirtualDisplay.Width; x++)
      //   {
      //     var r = (x | y) % 6;
      //     var g = (x ^ y) % 6;
      //     var b = (x & y) % 6;
      //     rc.SetPixel(x, y, palette[r, g, b]);
      //   }
      // }

      // Example: Display an image.
      rc.Fill(palette[1, 1, 0]);
      // for (int y = 0; y < image.Height; y++)
      // {
      //   for (int x = 0; x < image.Width; x++)
      //   {
      //     var pixel = image.GetPixel(x, y);
      //     if (pixel.Red == 0)
      //     {
      //       rc.SetPixel(x, y, palette[0, 0, 4]);
      //     }
      //     else
      //     {
      //       rc.SetPixel(x, y, palette[5, 5, 5]);
      //     }
      //   }
      // }

      // rc.DrawImage(image, 100, 100);

      var tiles = new TileSet<Bitmap>(new Bitmap(image), 8, 8);
      tiles[65].Draw(rc, 100, 100, palette[5, 5, 5], palette[0, 0, 4]);

      var font = new Font(tiles);
      font.WriteString(rc, "Hello world!", 150, 120, palette[5, 4, 3], palette[0, 0, 0]);

      window.RenderFrame += (FrameEventArgs e) =>
      {
        rc.Present();
        virtualDisplay.Render(window.ClientSize); // Render the virtual display
        window.SwapBuffers();
      };

      window.Resize += (ResizeEventArgs e) =>
      {
        // Update the window size for the virtual display
        virtualDisplay.Render(window.ClientSize);
      };

      window.Run();
    }
  }
}
