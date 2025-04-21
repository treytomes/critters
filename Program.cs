// Program.cs  
using System.CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Critters.Services;
using Critters.Events;
using Critters.IO;
using Critters.Gfx;
using OpenTK.Mathematics;
using Microsoft.Extensions.Options;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;

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
		rootCommand.SetHandler(async (configFile, debug, fullscreen, width, height) =>
			{
				await RunGameAsync(configFile, debug, fullscreen, width, height);
			},
			configFileOption, debugOption, fullscreenOption, widthOption, heightOption);

		// Parse the command line  
		return await rootCommand.InvokeAsync(args);
	}

	static async Task RunGameAsync(string configFile, bool debug, bool fullscreen, int? width, int? height)
	{
		try
		{
			// Build host with DI container  
			using var host = CreateHostBuilder(configFile, debug, fullscreen, width, height).Build();

			// Start the game  
			await host.Services.GetRequiredService<IGameEngine>().RunAsync();
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"Error starting the game: {ex.Message}");
			Environment.Exit(1);
		}
	}

	static IHostBuilder CreateHostBuilder(string configFile, bool debug, bool fullscreen, int? width, int? height)
	{
		return Host.CreateDefaultBuilder()
			.ConfigureAppConfiguration((hostContext, config) =>
			{
				config.Sources.Clear();
				config.SetBasePath(Directory.GetCurrentDirectory());
				config.AddJsonFile(configFile, optional: false, reloadOnChange: true);

				// Add command line overrides  
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

				config.AddInMemoryCollection(commandLineConfig);
			})
			.ConfigureLogging((hostContext, logging) =>
			{
				logging.ClearProviders();
				logging.AddConsole();

				// Set minimum log level based on debug setting  
				var debugEnabled = hostContext.Configuration.GetValue<bool>("Debug");
				if (debugEnabled)
				{
					logging.SetMinimumLevel(LogLevel.Debug);
				}
				else
				{
					logging.SetMinimumLevel(LogLevel.Information);
				}
			})
			.ConfigureServices((hostContext, services) =>
			{
				// Register configuration  
				services.Configure<AppSettings>(hostContext.Configuration);

				// Register services  
				services.AddSingleton<IEventBus, EventBus>();
				services.AddSingleton<IResourceManager, ResourceManager>();
				services.AddSingleton<IGameEngine, GameEngine>();

				// Register GameWindow as a factory.
				services.AddSingleton<GameWindow>(serviceProvider =>
				{
					// Get window settings from configuration.
					var appSettings = serviceProvider.GetRequiredService<IOptions<AppSettings>>().Value;

					var nativeWindowSettings = new NativeWindowSettings()
					{
						ClientSize = new Vector2i(appSettings.Window.Width, appSettings.Window.Height),
						Title = appSettings.Window.Title,
						Profile = ContextProfile.Core,
						APIVersion = new Version(4, 5),
						WindowState = appSettings.Window.Fullscreen ? WindowState.Fullscreen : WindowState.Normal
					};

					return new GameWindow(GameWindowSettings.Default, nativeWindowSettings);
				});

				// Register IVirtualDisplay as a factory  
				services.AddSingleton<IVirtualDisplay>(serviceProvider =>
				{
					// Get window settings from configuration  
					var appSettings = serviceProvider.GetRequiredService<IOptions<AppSettings>>().Value;

					// Get or create window  
					var windowSize = new Vector2i(
						appSettings.Window.Width,
						appSettings.Window.Height
					);

					// Create virtual display settings  
					var virtualDisplaySettings = new VirtualDisplaySettings
					{
						Width = appSettings.VirtualDisplay.Width,
						Height = appSettings.VirtualDisplay.Height,
						VertexShaderPath = appSettings.VirtualDisplay.VertexShaderPath,
						FragmentShaderPath = appSettings.VirtualDisplay.FragmentShaderPath
					};

					// Create and return the virtual display  
					return new VirtualDisplay(windowSize, virtualDisplaySettings);
				});

				services.AddSingleton<IRenderingContext, RenderingContext>();

				services.AddTransient<IResourceLoader<Image>, ImageLoader>();
				services.AddTransient<IGameStateManager, GameStateManager>();

				// Register game states if needed  
				// services.AddTransient<MainMenuState>();  
				// etc.  
			});
	}
}