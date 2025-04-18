// Program.cs  

using System.CommandLine;
using System.ComponentModel;
using Critters.Events;
using Critters.Gfx;
using Critters.IO;
using Critters.States;
using Critters.UI;
using Microsoft.Extensions.Configuration;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace Critters;

class Program
{
	static async Task<int> Main(string[] args)
	{
		// Define command-line options  
		var configFileOption = new Option<string>(
			name: "--config",
			description: "Path to the configuration file",
			getDefaultValue: () => "appsettings.json");

		var debugOption = new Option<bool>(
			name: "--debug",
			description: "Enable debug mode");

		var fullscreenOption = new Option<bool>(
			name: "--fullscreen",
			description: "Start in fullscreen mode");

		var widthOption = new Option<int?>(
			name: "--width",
			description: "Window width in pixels");

		var heightOption = new Option<int?>(
			name: "--height",
			description: "Window height in pixels");

		// Create root command  
		var rootCommand = new RootCommand("Critters Game");
		rootCommand.AddOption(configFileOption);
		rootCommand.AddOption(debugOption);
		rootCommand.AddOption(fullscreenOption);
		rootCommand.AddOption(widthOption);
		rootCommand.AddOption(heightOption);

		// Set handler for processing the command  
		rootCommand.SetHandler((configFile, debug, fullscreen, width, height) =>
			{
				RunGame(configFile, debug, fullscreen, width, height);
			},
			configFileOption, debugOption, fullscreenOption, widthOption, heightOption);

		// Parse the command line  
		return await rootCommand.InvokeAsync(args);
	}

	static void RunGame(string configFile, bool debug, bool fullscreen, int? width, int? height)
	{
		try
		{
			// Set up configuration with command-line overrides  
			var configBuilder = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile(configFile, optional: false, reloadOnChange: true);

			// Create command-line based configuration  
			var commandLineConfig = new Dictionary<string, string?>();

			if (debug)
			{
				commandLineConfig["Debug"] = "true";
			}
			if (fullscreen)
			{
				commandLineConfig["Window:Fullscreen"] = "true";
			}
			if (width.HasValue)
			{
				commandLineConfig["Window:Width"] = width.ToString();
			}
			if (height.HasValue)
			{
				commandLineConfig["Window:Height"] = height.ToString();
			}
			// Add command line values as configuration source  
			configBuilder.AddInMemoryCollection(commandLineConfig);

			IConfiguration configuration = configBuilder.Build();

			// Bind configuration to settings object  
			var settings = configuration.Get<AppSettings>()
				?? throw new InvalidOperationException("Failed to load application settings.");

			// Log configuration information if in debug mode  
			if (debug)
			{
				Console.WriteLine($"Running with settings from {configFile}");
				Console.WriteLine($"Debug mode: {settings.Debug}");
				Console.WriteLine($"Window: {settings.Window.Width}x{settings.Window.Height} ({(settings.Window.Fullscreen ? "Fullscreen" : "Windowed")})");
				Console.WriteLine($"Virtual display: {settings.VirtualDisplay.Width}x{settings.VirtualDisplay.Height}");
				Console.WriteLine($"Asset root: {settings.AssetRoot}");
			}

			// Initialize the game  
			InitializeGame(settings);
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"Error starting the game: {ex.Message}");
			Environment.Exit(1);
		}
	}

	static void InitializeGame(AppSettings settings)
	{
		var nativeWindowSettings = new NativeWindowSettings()
		{
			ClientSize = new Vector2i(settings.Window.Width, settings.Window.Height),
			Title = settings.Window.Title,
			Profile = ContextProfile.Core,
			APIVersion = new Version(4, 5),
			WindowState = settings.Window.Fullscreen ? WindowState.Fullscreen : WindowState.Normal
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
		states.EnterState(new MainMenuState());

		// Occurs when the window is about to close.  
		window.Closing += (CancelEventArgs e) =>
		{
			states.Unload(resources, eventBus);
			mouseCursor.Unload(resources, eventBus);
		};

		// Occurs when the NativeWindow.IsFocused property of the window changes.  
		window.FocusedChanged += (FocusedChangedEventArgs e) =>
		{
			if (settings.Debug)
			{
				Console.WriteLine("Window focused: {0}", e.IsFocused);
			}
		};

		// Occurs whenever a keyboard key is pressed.  
		window.KeyDown += (KeyboardKeyEventArgs e) =>
		{
			eventBus.Publish(new KeyEventArgs(e.Key, e.ScanCode, e.Modifiers, e.IsRepeat, true));
		};

		// Occurs whenever a keyboard key is released.  
		window.KeyUp += (KeyboardKeyEventArgs e) =>
		{
			eventBus.Publish(new KeyEventArgs(e.Key, e.ScanCode, e.Modifiers, e.IsRepeat, false));
		};

		// Occurs whenever a OpenTK.Windowing.GraphicsLibraryFramework.MouseButton is clicked.  
		window.MouseDown += (MouseButtonEventArgs e) =>
		{
			eventBus.Publish(e);
		};

		// Occurs whenever a OpenTK.Windowing.GraphicsLibraryFramework.MouseButton is released.  
		window.MouseUp += (MouseButtonEventArgs e) =>
		{
			eventBus.Publish(e);
		};

		// Occurs whenever the mouse cursor is moved.  
		window.MouseMove += (MouseMoveEventArgs e) =>
		{
			var position = display.ActualToVirtualPoint(e.Position);
			var delta = e.Delta / display.Scale;

			if (position.X < 0 || position.Y < 0 || position.X > display.Width || position.Y > display.Height)
			{
				// The cursor has fallen off the virtual display.  
				window.CursorState = CursorState.Normal;
			}
			else
			{
				window.CursorState = CursorState.Hidden;
			}

			eventBus.Publish(new MouseMoveEventArgs(position, delta));
		};

		// Occurs whenever a mouse wheel is moved.  
		window.MouseWheel += (MouseWheelEventArgs e) =>
		{
			eventBus.Publish(e);
		};

		// Occurs whenever a Unicode code point is typed.  
		window.TextInput += (TextInputEventArgs e) =>
		{
			// Implement if text input handling is needed  
		};

		// Occurs before the window is destroyed.  
		window.Unload += () =>
		{
			if (settings.Debug)
			{
				Console.WriteLine("The window is about to be destroyed.");
			}
		};

		// Occurs before the window is displayed for the first time.  
		window.Load += () =>
		{
			display.Resize(window.ClientSize);
			if (settings.Debug)
			{
				Console.WriteLine("Window is being loaded.");
			}
		};

		// Occurs whenever the window is resized.  
		window.Resize += (ResizeEventArgs e) =>
		{
			if (settings.Debug)
			{
				Console.WriteLine("Window resized: {0}", window.ClientSize);
			}
			display.Resize(window.ClientSize);
		};

		var updateGameTime = new GameTime();
		var renderGameTime = new GameTime();

		// Occurs when it is time to update a frame. This is invoked before GameWindow.RenderFrame.  
		window.UpdateFrame += (FrameEventArgs e) =>
		{
			if (!states.HasState)
			{
				window.Close();
			}

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