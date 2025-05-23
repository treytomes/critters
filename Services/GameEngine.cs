// Services/GameEngine.cs  
using Critters.Gfx;
using Critters.IO;
using Critters.States;
using Critters.UI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using System.ComponentModel;

namespace Critters.Services;

class GameEngine : IGameEngine, IDisposable
{
	#region Fields

	private readonly AppSettings _settings;
	private readonly IResourceManager _resourceManager;
	private readonly IEventBus _eventBus;
	private readonly ILogger<GameEngine> _logger;
	private readonly IVirtualDisplay _display;
	private readonly IRenderingContext _renderingContext;
	private GameWindow? _window;
	private MouseCursor? _mouseCursor;
	private IGameStateManager _stateManager;
	private GameTime _renderGameTime = new();
	private GameTime _updateGameTime = new();

	#endregion

	#region Constructors

	public GameEngine(
		GameWindow window,
		IOptions<AppSettings> settings,
		IResourceManager resourceManager,
		IEventBus eventBus,
		IVirtualDisplay display,
		IRenderingContext renderingContext,
		IGameStateManager stateManager,
		ILogger<GameEngine> logger)
	{
		_window = window;
		_settings = settings.Value;
		_resourceManager = resourceManager;
		_eventBus = eventBus;
		_display = display;
		_renderingContext = renderingContext;
		_stateManager = stateManager;
		_logger = logger;
	}

	#endregion

	#region Methods

	public async Task RunAsync(CancellationToken cancellationToken = default)
	{
		try
		{
			SetupWindowEvents();
			InitializeResources();

			// Register a cancellation handler  
			cancellationToken.Register(() => _window?.Close());

			_window!.Run();

			await Task.CompletedTask; // Just to make this method async  
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "An error occurred while running the game");
			throw;
		}
	}

	private void InitializeResources()
	{
		_resourceManager.Register<Image, ImageLoader>();

		_mouseCursor = new MouseCursor(_resourceManager, _renderingContext);
		_mouseCursor.Load();

		_stateManager.Load();
		_stateManager.EnterState(new MainMenuState(_resourceManager, _renderingContext));
	}

	private void SetupWindowEvents()
	{
		if (_window == null)
		{
			return;
		}

		// Occurs when the window is about to close.  
		_window.Closing += HandleWindowClosing;
		_window.FocusedChanged += HandleFocusedChanged;
		_window.KeyDown += HandleKeyDown;
		_window.KeyUp += HandleKeyUp;
		_window.MouseDown += HandleMouseDown;
		_window.MouseUp += HandleMouseUp;
		_window.MouseMove += HandleMouseMove;
		_window.MouseWheel += HandleMouseWheel;
		_window.TextInput += HandleTextInput;
		_window.Unload += HandleWindowUnload;
		_window.Load += HandleWindowLoad;
		_window.Resize += HandleWindowResize;
		_window.UpdateFrame += HandleUpdateFrame;
		_window.RenderFrame += HandleRenderFrame;
	}

	private void HandleWindowClosing(CancelEventArgs e)
	{
		_stateManager?.Unload();
		_mouseCursor?.Unload();
	}

	private void HandleFocusedChanged(FocusedChangedEventArgs e)
	{
		if (_settings.Debug)
		{
			_logger.LogInformation("Window focused: {IsFocused}", e.IsFocused);
		}
	}

	private void HandleKeyDown(KeyboardKeyEventArgs e)
	{
		_stateManager.KeyDown(e);
		_eventBus.Publish(new KeyEventArgs(e.Key, e.ScanCode, e.Modifiers, e.IsRepeat, true));
	}

	private void HandleKeyUp(KeyboardKeyEventArgs e)
	{
		_stateManager.KeyUp(e);
		_eventBus.Publish(new KeyEventArgs(e.Key, e.ScanCode, e.Modifiers, e.IsRepeat, false));
	}

	private void HandleMouseDown(MouseButtonEventArgs e)
	{
		_stateManager.MouseDown(e);
		_eventBus.Publish(e);
	}

	private void HandleMouseUp(MouseButtonEventArgs e)
	{
		_stateManager.MouseUp(e);
		_eventBus.Publish(e);
	}

	private void HandleMouseMove(MouseMoveEventArgs e)
	{
		if (_window == null || _display == null)
		{
			return;
		}

		var position = _display.ActualToVirtualPoint(e.Position);
		var delta = e.Delta / _display.Scale;

		if (position.X < 0 || position.Y < 0 || position.X > _display.Width || position.Y > _display.Height)
		{
			// The cursor has fallen off the virtual display.  
			_window.CursorState = OpenTK.Windowing.Common.CursorState.Normal;
		}
		else
		{
			_window.CursorState = OpenTK.Windowing.Common.CursorState.Hidden;
		}

		e = new MouseMoveEventArgs(position, delta);

		_stateManager.MouseMove(e);
		_mouseCursor?.MouseMove(e);
		_eventBus.Publish(e);
	}

	private void HandleMouseWheel(MouseWheelEventArgs e)
	{
		_stateManager.MouseWheel(e);
		_eventBus.Publish(e);
	}

	private void HandleTextInput(TextInputEventArgs e)
	{
		_stateManager.TextInput(e);
	}

	private void HandleWindowUnload()
	{
		if (_settings.Debug)
		{
			_logger.LogInformation("The window is about to be destroyed.");
		}
	}

	private void HandleWindowLoad()
	{
		if (_window == null || _display == null)
		{
			return;
		}

		_display.Resize(_window.ClientSize);
		if (_settings.Debug)
		{
			_logger.LogInformation("Window is being loaded.");
		}
	}

	private void HandleWindowResize(ResizeEventArgs e)
	{
		if (_window == null || _display == null)
		{
			return;
		}

		if (_settings.Debug)
		{
			_logger.LogInformation("Window resized: {ClientSize}", _window.ClientSize);
		}
		_display.Resize(_window.ClientSize);
	}

	private void HandleUpdateFrame(FrameEventArgs e)
	{
		if (_window == null)
		{
			return;
		}

		_updateGameTime = _updateGameTime.Add(e.Time);

		if (_stateManager.HasState)
		{
			_stateManager.Update(_updateGameTime);
		}
		else
		{
			_window.Close();
		}

		_mouseCursor?.Update(_updateGameTime);
	}

	private void HandleRenderFrame(FrameEventArgs e)
	{
		if (_window == null || _renderingContext == null || _display == null || _stateManager == null || _mouseCursor == null)
		{
			return;
		}

		_renderGameTime = _renderGameTime.Add(e.Time);

		if (_stateManager.HasState)
		{
			_stateManager.Render(_renderGameTime);
		}

		_mouseCursor.Render(_renderGameTime);
		_renderingContext.Present();
		_display.Render(); // Render the virtual display  
		_window.SwapBuffers();
	}

	public void Dispose()
	{
		_window?.Dispose();
		_display?.Dispose();
		GC.SuppressFinalize(this);
	}

	#endregion
}