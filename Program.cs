using System.ComponentModel;
using Critters.Events;
using Critters.Gfx;
using Critters.IO;
using Critters.States;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace Critters;

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
    using var display = new VirtualDisplay(window.ClientSize, settings.VirtualDisplay);
    var rc = new RenderingContext(display);

    var resources = new ResourceManager(settings.AssetRoot);
    resources.Register<Image, ImageLoader>();

    var eventBus = new EventBus();

    var mouseCursor = new MouseCursor();
    mouseCursor.Load(resources, eventBus);

    var states = new GameStateManager();
    states.Load(resources, eventBus);
    // states.EnterState(new ImageTestState());
    // states.EnterState(new FontTestState());
    // states.EnterState(new PatternTestState());
    // states.EnterState(new RasterGraphicsTestState());
    states.EnterState(new TileMapTestState());
    // states.EnterState(new MainMenuState());

    // Occurs when the window is about to close.
    window.Closing += (CancelEventArgs e) => {
      states.Unload(resources, eventBus);
      mouseCursor.Unload(resources, eventBus);
    };

    // Occurs whenever one or more files are dropped on the window.
    // window.FileDrop += (FileDropEventArgs e) => {};

    // Occurs when the NativeWindow.IsFocused property of the window changes.
    window.FocusedChanged += (FocusedChangedEventArgs e) =>
    {
      Console.WriteLine("Window focused: {0}", e.IsFocused);
    };

    // Occurs when the window is minimized.
    // Focus is lost when minimized.  That's all that really matters.
    // window.Minimized += (MinimizedEventArgs e) => {
    //   Console.WriteLine("Window minimized: {0}", e.IsMinimized);
    // };

    // Occurs when the window is maximized.
    // window.Maximized += (MaximizedEventArgs e) => {};

    // Occurs when a joystick is connected or disconnected.
    // window.JoystickConnected += (JoystickEventArgs e) => {};

    // Occurs whenever a keyboard key is pressed.
    window.KeyDown += (KeyboardKeyEventArgs e) => {};

    // Occurs whenever a keyboard key is released.
    window.KeyUp += (KeyboardKeyEventArgs e) => {};

    // Occurs whenever a OpenTK.Windowing.GraphicsLibraryFramework.MouseButton is clicked.
    window.MouseDown += (MouseButtonEventArgs e) => {};

    // Occurs whenever a OpenTK.Windowing.GraphicsLibraryFramework.MouseButton is released.
    window.MouseUp += (MouseButtonEventArgs e) => {};

    // Occurs whenever the mouse cursor enters the window NativeWindow.Bounds.
    // window.MouseEnter += () => {};

    // Occurs whenever the mouse cursor leaves the window NativeWindow.Bounds.
    // window.MouseLeave += () => {};

    // Occurs whenever the mouse cursor is moved.
    window.MouseMove += (MouseMoveEventArgs e) =>
    {
      var position = display.ActualToVirtualPoint(e.Position);
      var delta = e.Delta / display.Scale;

      if (position.X < 0 || position.Y < 0 || position.X > display.Width || position.Y > display.Height)
      {
        // The cursor has fallen off the virtual display.
        window.CursorState = CursorState.Normal;
      } else {
        window.CursorState = CursorState.Hidden;
      }

      eventBus.Publish(new MouseMoveEventArgs(position, delta));
    };

    // Occurs whenever a mouse wheel is moved.
    window.MouseWheel += (MouseWheelEventArgs e) => {};

    // Occurs whenever the window is moved.
    // window.Move += (WindowPositionEventArgs e) => {};

    // Occurs whenever the window is refreshed.
    // This is happening on resize.
    // window.Refresh += () => {};

    // Occurs whenever a Unicode code point is typed.
    window.TextInput += (TextInputEventArgs e) => {};

    // Occurs before the window is destroyed.
    window.Unload += () =>
    {
      Console.WriteLine("The window is about to be destroyed.");
    };

    // Occurs before the window is displayed for the first time.
    window.Load += () =>
    {
      display.Resize(window.ClientSize);
      Console.WriteLine("Window is being loaded.");
    };

    // Occurs whenever the framebuffer is resized.
    // This occurs along with a window resize.
    // window.FramebufferResize += (FramebufferResizeEventArgs e) => {};

    // Occurs whenever the window is resized.
    window.Resize += (ResizeEventArgs e) =>
    {
      Console.WriteLine("Window resized: {0}", window.ClientSize);
      display.Resize(window.ClientSize);
    };

    var updateGameTime = new GameTime();
    var renderGameTime = new GameTime();

    // Occurs when it is time to update a frame. This is invoked before GameWindow.RenderFrame.
    window.UpdateFrame += (FrameEventArgs e) =>
    {
      updateGameTime = updateGameTime.Add(e.Time);
      states.Update(updateGameTime);
      mouseCursor.Update(updateGameTime);
    };
    
    // Occurs when it is time to render a frame. This is invoked after GameWindow.UpdateFrequency.
    window.RenderFrame += (FrameEventArgs e) =>
    {
      renderGameTime = renderGameTime.Add(e.Time);

      if (states.HasState)
      {
        states.Render(rc, renderGameTime);
      }

      mouseCursor.Render(rc, renderGameTime);

      rc.Present();
      display.Render(); // Render the virtual display
      window.SwapBuffers();
    };

    window.Run();
  }
}
