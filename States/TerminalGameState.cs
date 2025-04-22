using Critters.Gfx;
using Critters.Services;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Text;

namespace Critters.States;

/// <summary>
/// A game state that provides terminal functionality for command input and output.
/// </summary>
class TerminalGameState : GameState
{
	private Font _font;
	private readonly TerminalController _terminal;
	private readonly TerminalSettings _settings;
	private float _cursorBlinkTime;
	private bool _cursorVisible = true;

	/// <summary>
	/// Initializes a new instance of the <see cref="TerminalGameState"/> class.
	/// </summary>
	/// <param name="resources">Resource manager for loading assets.</param>
	/// <param name="rc">Rendering context for drawing.</param>
	/// <param name="font">Font to use for rendering text.</param>
	/// <param name="settings">Terminal settings (optional).</param>
	public TerminalGameState(
		IResourceManager resources,
		IRenderingContext rc,
		TerminalSettings? settings = null)
		: base(resources, rc)
	{
		_settings = settings ?? new TerminalSettings();
		_terminal = new TerminalController(_settings);

		var image = Resources.Load<Gfx.Image>("oem437_8.png");
		var bmp = new Bitmap(image);
		var tiles = new GlyphSet<Bitmap>(bmp, 8, 8);
		_font = new Font(tiles);
	}

	/// <summary>
	/// Called when this state becomes the active state.
	/// </summary>
	public override void AcquireFocus()
	{
		base.AcquireFocus();
		_terminal.DisplayPrompt();
	}

	/// <summary>
	/// Updates the terminal state.
	/// </summary>
	/// <param name="gameTime">Timing values for the current frame.</param>
	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);

		// Update cursor blink
		_cursorBlinkTime += (float)gameTime.ElapsedTime.TotalSeconds;
		if (_cursorBlinkTime >= _settings.CursorBlinkRate)
		{
			_cursorBlinkTime = 0;
			_cursorVisible = !_cursorVisible;
		}
	}

	/// <summary>
	/// Renders the terminal.
	/// </summary>
	/// <param name="gameTime">Timing values for the current frame.</param>
	public override void Render(GameTime gameTime)
	{
		base.Render(gameTime);

		// Clear the background
		RC.RenderFilledRect(RC.Bounds, _settings.BackgroundColor);

		// Calculate visible area
		int visibleLines = (int)(RC.Height / _settings.LineHeight) - 1;
		int startLine = Math.Max(0, _terminal.Buffer.LineCount - visibleLines);

		// Render terminal lines
		for (int i = 0; i < visibleLines && (i + startLine) < _terminal.Buffer.LineCount; i++)
		{
			TerminalLine line = _terminal.Buffer.GetLine(i + startLine);
			Vector2 position = new Vector2(_settings.MarginLeft, _settings.MarginTop + i * _settings.LineHeight);

			_font.WriteString(RC, line.Text, position, line.ForegroundColor, line.BackgroundColor);
		}

		// Render cursor if visible and at input line
		if (_cursorVisible && _terminal.IsInputActive)
		{
			int cursorLine = _terminal.Buffer.LineCount - 1;
			int cursorPos = _terminal.TotalCursorPosition; // Use the total position that includes prompt length

			if (cursorLine >= startLine)
			{
				int lineY = (cursorLine - startLine);
				Vector2 cursorPosition = new Vector2(
					_settings.MarginLeft + cursorPos * 8, // Assuming 8px char width
					_settings.MarginTop + lineY * _settings.LineHeight);

				RC.RenderFilledRect(
					new Box2(cursorPosition, cursorPosition + new Vector2(8, _settings.LineHeight)),
					_settings.CursorColor);
			}
		}
	}

	/// <summary>
	/// Handles keyboard input for the terminal.
	/// </summary>
	/// <param name="e">Keyboard event arguments.</param>
	/// <returns>True if the input was handled.</returns>
	public override bool KeyDown(KeyboardKeyEventArgs e)
	{
		if (base.KeyDown(e))
			return true;

		switch (e.Key)
		{
			case Keys.Enter:
				_terminal.ExecuteCommand();
				return true;

			case Keys.Backspace:
				_terminal.Backspace();
				return true;

			case Keys.Delete:
				_terminal.Delete();
				return true;

			case Keys.Left:
				_terminal.MoveCursorLeft();
				return true;

			case Keys.Right:
				_terminal.MoveCursorRight();
				return true;

			case Keys.Up:
				_terminal.NavigateHistoryUp();
				return true;

			case Keys.Down:
				_terminal.NavigateHistoryDown();
				return true;

			case Keys.Home:
				_terminal.MoveCursorToStart();
				return true;

			case Keys.End:
				_terminal.MoveCursorToEnd();
				return true;
		}

		return false;
	}

	/// <summary>
	/// Handles text input for the terminal.
	/// </summary>
	/// <param name="e">Text input event arguments.</param>
	/// <returns>True if the input was handled.</returns>
	public override bool TextInput(TextInputEventArgs e)
	{
		if (base.TextInput(e))
			return true;

		_terminal.InsertText(e.AsString);
		return true;
	}
}

/// <summary>
/// Settings for the terminal appearance and behavior.
/// </summary>
struct TerminalSettings
{
	/// <summary>
	/// The prompt string displayed before user input.
	/// </summary>
	public string Prompt { get; set; }

	/// <summary>
	/// The color of the prompt text.
	/// </summary>
	public RadialColor PromptColor { get; set; }

	/// <summary>
	/// The color of the user input text.
	/// </summary>
	public RadialColor InputColor { get; set; }

	/// <summary>
	/// The color of the output text.
	/// </summary>
	public RadialColor OutputColor { get; set; }

	/// <summary>
	/// The color of the error text.
	/// </summary>
	public RadialColor ErrorColor { get; set; }

	/// <summary>
	/// The background color of the terminal.
	/// </summary>
	public RadialColor BackgroundColor { get; set; }

	/// <summary>
	/// The color of the cursor.
	/// </summary>
	public RadialColor CursorColor { get; set; }

	/// <summary>
	/// The height of each line in pixels.
	/// </summary>
	public float LineHeight { get; set; }

	/// <summary>
	/// The left margin in pixels.
	/// </summary>
	public float MarginLeft { get; set; }

	/// <summary>
	/// The top margin in pixels.
	/// </summary>
	public float MarginTop { get; set; }

	/// <summary>
	/// The cursor blink rate in seconds.
	/// </summary>
	public float CursorBlinkRate { get; set; }

	/// <summary>
	/// The maximum number of commands to store in history.
	/// </summary>
	public int MaxHistorySize { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="TerminalSettings"/> struct with default values.
	/// </summary>
	public TerminalSettings()
	{
		Prompt = "> ";
		PromptColor = RadialColor.Green;
		InputColor = RadialColor.White;
		OutputColor = new RadialColor(4, 4, 4); // Light gray
		ErrorColor = RadialColor.Red;
		BackgroundColor = RadialColor.Black;
		CursorColor = new RadialColor(3, 3, 5); // Light blue
		LineHeight = 10;
		MarginLeft = 10;
		MarginTop = 10;
		CursorBlinkRate = 0.5f;
		MaxHistorySize = 100;
	}
}

/// <summary>
/// Represents a line of text in the terminal with color information.
/// </summary>
struct TerminalLine
{
	/// <summary>
	/// The text content of the line.
	/// </summary>
	public string Text { get; set; }

	/// <summary>
	/// The foreground color of the text.
	/// </summary>
	public RadialColor ForegroundColor { get; set; }

	/// <summary>
	/// The background color of the text (or null for default).
	/// </summary>
	public RadialColor? BackgroundColor { get; set; }

	/// <summary>
	/// The type of line (prompt, input, output, error).
	/// </summary>
	public TerminalLineType LineType { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="TerminalLine"/> struct.
	/// </summary>
	/// <param name="text">The text content.</param>
	/// <param name="foregroundColor">The foreground color.</param>
	/// <param name="backgroundColor">The background color (optional).</param>
	/// <param name="lineType">The type of line.</param>
	public TerminalLine(string text, RadialColor foregroundColor, RadialColor? backgroundColor = null, TerminalLineType lineType = TerminalLineType.Output)
	{
		Text = text;
		ForegroundColor = foregroundColor;
		BackgroundColor = backgroundColor;
		LineType = lineType;
	}
}

/// <summary>
/// Defines the type of a terminal line.
/// </summary>
enum TerminalLineType
{
	/// <summary>
	/// A prompt line.
	/// </summary>
	Prompt,

	/// <summary>
	/// A user input line.
	/// </summary>
	Input,

	/// <summary>
	/// An output line.
	/// </summary>
	Output,

	/// <summary>
	/// An error line.
	/// </summary>
	Error
}

/// <summary>
/// Manages the terminal buffer and lines.
/// </summary>
class TerminalBuffer
{
	private readonly List<TerminalLine> _lines = new();

	/// <summary>
	/// Gets the number of lines in the buffer.
	/// </summary>
	public int LineCount => _lines.Count;

	/// <summary>
	/// Adds a line to the buffer.
	/// </summary>
	/// <param name="line">The line to add.</param>
	public void AddLine(TerminalLine line)
	{
		_lines.Add(line);
	}

	/// <summary>
	/// Gets a line from the buffer.
	/// </summary>
	/// <param name="index">The index of the line to get.</param>
	/// <returns>The line at the specified index.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when index is out of range.</exception>
	public TerminalLine GetLine(int index)
	{
		if (index < 0 || index >= _lines.Count)
			throw new ArgumentOutOfRangeException(nameof(index));

		return _lines[index];
	}

	/// <summary>
	/// Updates the last line in the buffer.
	/// </summary>
	/// <param name="line">The new line content.</param>
	public void UpdateLastLine(TerminalLine line)
	{
		if (_lines.Count > 0)
			_lines[_lines.Count - 1] = line;
		else
			AddLine(line);
	}

	/// <summary>
	/// Clears all lines from the buffer.
	/// </summary>
	public void Clear()
	{
		_lines.Clear();
	}
}

/// <summary>
/// Manages command history and navigation.
/// </summary>
class CommandHistory
{
	private readonly List<string> _history = new();
	private readonly int _maxSize;
	private int _currentIndex = -1;
	private string _savedInput = string.Empty;

	/// <summary>
	/// Initializes a new instance of the <see cref="CommandHistory"/> class.
	/// </summary>
	/// <param name="maxSize">The maximum number of commands to store.</param>
	public CommandHistory(int maxSize)
	{
		_maxSize = Math.Max(1, maxSize);
	}

	/// <summary>
	/// Adds a command to the history.
	/// </summary>
	/// <param name="command">The command to add.</param>
	public void Add(string command)
	{
		// Don't add empty commands or duplicates of the most recent command
		if (string.IsNullOrWhiteSpace(command) ||
			(_history.Count > 0 && _history[0] == command))
			return;

		// Store the command exactly as it was entered
		_history.Insert(0, command);
		if (_history.Count > _maxSize)
			_history.RemoveAt(_history.Count - 1);

		_currentIndex = -1;
	}

	/// <summary>
	/// Navigates to the previous command in history.
	/// </summary>
	/// <returns>The previous command, or null if at the beginning of history.</returns>
	public string? NavigateUp(string currentInput)
	{
		if (_history.Count == 0)
			return null;

		// Save the current input when first navigating up
		if (_currentIndex == -1)
		{
			_savedInput = currentInput;
			_currentIndex = 0;
		}
		else if (_currentIndex < _history.Count - 1)
		{
			_currentIndex++;
		}

		return _history[_currentIndex];
	}

	/// <summary>
	/// Navigates to the next command in history.
	/// </summary>
	/// <returns>The next command, or empty string if at the end of history.</returns>
	public string? NavigateDown()
	{
		if (_currentIndex <= 0)
		{
			_currentIndex = -1;
			return _savedInput;
		}

		_currentIndex--;
		return _history[_currentIndex];
	}

	/// <summary>
	/// Resets the history navigation.
	/// </summary>
	public void ResetNavigation()
	{
		_currentIndex = -1;
		_savedInput = string.Empty;
	}
}

/// <summary>
/// Interface for command processing.
/// </summary>
interface ICommandProcessor
{
	/// <summary>
	/// Processes a command and returns the result.
	/// </summary>
	/// <param name="command">The command to process.</param>
	/// <returns>The result of the command.</returns>
	CommandResult ProcessCommand(string command);
}

/// <summary>
/// Represents the result of a command execution.
/// </summary>
class CommandResult
{
	/// <summary>
	/// Gets the output text of the command.
	/// </summary>
	public string Output { get; }

	/// <summary>
	/// Gets a value indicating whether the command was successful.
	/// </summary>
	public bool IsSuccessful { get; }

	/// <summary>
	/// Gets the color to use for the output.
	/// </summary>
	public RadialColor Color { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="CommandResult"/> class.
	/// </summary>
	/// <param name="output">The output text.</param>
	/// <param name="success">Whether the command was successful.</param>
	/// <param name="color">The color for the output (optional).</param>
	public CommandResult(string output, bool success, RadialColor? color = null)
	{
		Output = output;
		IsSuccessful = success;
		Color = color ?? (success ? new RadialColor(4, 4, 4) : RadialColor.Red);
	}

	/// <summary>
	/// Creates a successful command result.
	/// </summary>
	/// <param name="output">The output text.</param>
	/// <param name="color">The color for the output (optional).</param>
	/// <returns>A successful command result.</returns>
	public static CommandResult Success(string output, RadialColor? color = null)
	{
		return new CommandResult(output, true, color);
	}

	/// <summary>
	/// Creates an error command result.
	/// </summary>
	/// <param name="errorMessage">The error message.</param>
	/// <param name="color">The color for the output (defaults to red).</param>
	/// <returns>An error command result.</returns>
	public static CommandResult Error(string errorMessage, RadialColor? color = null)
	{
		return new CommandResult(errorMessage, false, color ?? RadialColor.Red);
	}
}

/// <summary>
/// Default implementation of ICommandProcessor with basic commands.
/// </summary>
class DefaultCommandProcessor : ICommandProcessor
{
	private readonly Dictionary<string, Func<string[], CommandResult>> _commands = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="DefaultCommandProcessor"/> class.
	/// </summary>
	public DefaultCommandProcessor()
	{
		RegisterDefaultCommands();
	}

	/// <summary>
	/// Registers the default set of commands.
	/// </summary>
	private void RegisterDefaultCommands()
	{
		// Help command
		_commands["help"] = _ => CommandResult.Success(
			"Available commands:\n" +
			"  help     - Display this help text\n" +
			"  clear    - Clear the terminal\n" +
			"  echo     - Echo the provided text\n" +
			"  color    - Change text color (e.g. color 5 3 0)\n" +
			"  exit     - Exit the terminal"
		);

		// Clear command
		_commands["clear"] = _ => CommandResult.Success("", RadialColor.White);

		// Echo command
		_commands["echo"] = args => CommandResult.Success(string.Join(" ", args));

		// Color command
		_commands["color"] = args =>
		{
			if (args.Length != 3 ||
				!byte.TryParse(args[0], out byte r) ||
				!byte.TryParse(args[1], out byte g) ||
				!byte.TryParse(args[2], out byte b))
			{
				return CommandResult.Error("Usage: color R G B (values 0-5)");
			}

			try
			{
				var color = new RadialColor(r, g, b);
				return CommandResult.Success($"Color set to {color}", color);
			}
			catch (ArgumentException)
			{
				return CommandResult.Error("Color values must be between 0 and 5");
			}
		};

		// Exit command
		_commands["exit"] = _ => CommandResult.Success("Goodbye!");
	}

	/// <summary>
	/// Processes a command and returns the result.
	/// </summary>
	/// <param name="command">The command to process.</param>
	/// <returns>The result of the command.</returns>
	public CommandResult ProcessCommand(string command)
	{
		if (string.IsNullOrWhiteSpace(command))
			return CommandResult.Success("");

		string[] parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length == 0)
			return CommandResult.Success("");

		string cmdName = parts[0].ToLower();
		string[] args = parts.Length > 1 ? parts[1..] : Array.Empty<string>();

		if (_commands.TryGetValue(cmdName, out var handler))
		{
			return handler(args);
		}

		return CommandResult.Error($"Unknown command: {cmdName}");
	}
}

/// <summary>
/// Controls the terminal behavior and state.
/// </summary>
class TerminalController
{
	private readonly TerminalSettings _settings;
	private readonly TerminalBuffer _buffer;
	private readonly CommandHistory _history;
	private readonly ICommandProcessor _commandProcessor;
	private readonly StringBuilder _currentInput = new();
	private readonly StringBuilder _multiLineBuffer = new();
	private int _cursorPosition = 0;
	private bool _isMultiLine = false;

	// Add a continuation prompt property
	private string CurrentPrompt => _isMultiLine ? "... " : _settings.Prompt;

	/// <summary>
	/// Gets the terminal buffer.
	/// </summary>
	public TerminalBuffer Buffer => _buffer;

	/// <summary>
	/// Gets the cursor position within the current input.
	/// </summary>
	public int CursorPosition => _cursorPosition;

	/// <summary>
	/// Gets the total cursor position including the prompt length.
	/// </summary>
	public int TotalCursorPosition => CurrentPrompt.Length + _cursorPosition;

	/// <summary>
	/// Gets a value indicating whether input is currently active.
	/// </summary>
	public bool IsInputActive => true;

	/// <summary>
	/// Initializes a new instance of the <see cref="TerminalController"/> class.
	/// </summary>
	/// <param name="settings">The terminal settings.</param>
	/// <param name="commandProcessor">The command processor to use (optional).</param>
	public TerminalController(TerminalSettings settings, ICommandProcessor? commandProcessor = null)
	{
		_settings = settings;
		_buffer = new TerminalBuffer();
		_history = new CommandHistory(settings.MaxHistorySize);
		_commandProcessor = commandProcessor ?? new DefaultCommandProcessor();
	}

	/// <summary>
	/// Displays the prompt for user input.
	/// </summary>
	public void DisplayPrompt()
	{
		_buffer.AddLine(new TerminalLine(
			CurrentPrompt + _currentInput.ToString(),
			_settings.PromptColor,
			null,
			TerminalLineType.Prompt));
		_cursorPosition = 0;
		_currentInput.Clear();
	}

	/// <summary>
	/// Inserts text at the current cursor position.
	/// </summary>
	/// <param name="text">The text to insert.</param>
	public void InsertText(string text)
	{
		if (string.IsNullOrEmpty(text))
			return;

		_currentInput.Insert(_cursorPosition, text);
		_cursorPosition += text.Length;
		UpdateInputLine();
	}

	/// <summary>
	/// Removes the character before the cursor.
	/// </summary>
	public void Backspace()
	{
		if (_cursorPosition > 0)
		{
			_currentInput.Remove(_cursorPosition - 1, 1);
			_cursorPosition--;
			UpdateInputLine();
		}
	}

	/// <summary>
	/// Removes the character at the cursor position.
	/// </summary>
	public void Delete()
	{
		if (_cursorPosition < _currentInput.Length)
		{
			_currentInput.Remove(_cursorPosition, 1);
			UpdateInputLine();
		}
	}

	/// <summary>
	/// Moves the cursor left.
	/// </summary>
	public void MoveCursorLeft()
	{
		if (_cursorPosition > 0)
		{
			_cursorPosition--;
			UpdateInputLine();
		}
	}

	/// <summary>
	/// Moves the cursor right.
	/// </summary>
	public void MoveCursorRight()
	{
		if (_cursorPosition < _currentInput.Length)
		{
			_cursorPosition++;
			UpdateInputLine();
		}
	}

	/// <summary>
	/// Moves the cursor to the start of the input.
	/// </summary>
	public void MoveCursorToStart()
	{
		_cursorPosition = 0;
		UpdateInputLine();
	}

	/// <summary>
	/// Moves the cursor to the end of the input.
	/// </summary>
	public void MoveCursorToEnd()
	{
		_cursorPosition = _currentInput.Length;
		UpdateInputLine();
	}

	/// <summary>
	/// Navigates up through command history.
	/// </summary>
	public void NavigateHistoryUp()
	{
		string? previous = _history.NavigateUp(_currentInput.ToString());
		if (previous != null)
		{
			_currentInput.Clear();
			_currentInput.Append(previous);
			_cursorPosition = _currentInput.Length;
			UpdateInputLine();
		}
	}

	/// <summary>
	/// Navigates down through command history.
	/// </summary>
	public void NavigateHistoryDown()
	{
		string? next = _history.NavigateDown();
		if (next != null)
		{
			_currentInput.Clear();
			_currentInput.Append(next);
			_cursorPosition = _currentInput.Length;
			UpdateInputLine();
		}
	}

	/// <summary>
	/// Executes the current command.
	/// </summary>
	public void ExecuteCommand()
	{
		string input = _currentInput.ToString();

		// Update the display to show the entered command
		_buffer.UpdateLastLine(new TerminalLine(
			CurrentPrompt + input,
			_settings.InputColor,
			null,
			TerminalLineType.Input));

		// Handle line continuation
		if (input.EndsWith("\\"))
		{
			if (!_isMultiLine)
			{
				_isMultiLine = true;
				_multiLineBuffer.Clear();
			}

			// Store without the backslash but don't add newline character
			_multiLineBuffer.Append(input.Substring(0, input.Length - 1));
			_multiLineBuffer.Append(' '); // Add space instead of newline for proper concatenation
			_currentInput.Clear();
			_cursorPosition = 0;

			// Display a continuation prompt
			DisplayPrompt();
			return;
		}

		// Process the complete command
		string commandToExecute;
		if (_isMultiLine)
		{
			_multiLineBuffer.Append(input);
			commandToExecute = _multiLineBuffer.ToString();
			_isMultiLine = false;
			_multiLineBuffer.Clear();
		}
		else
		{
			commandToExecute = input;
		}

		// Add to history if not empty
		if (!string.IsNullOrWhiteSpace(commandToExecute))
		{
			_history.Add(commandToExecute);
		}

		// Process the command
		CommandResult result = _commandProcessor.ProcessCommand(commandToExecute);

		// Handle special case for clear command
		if (commandToExecute.Trim().ToLower() == "clear")
		{
			_buffer.Clear();
		}
		else if (!string.IsNullOrEmpty(result.Output))
		{
			// Display the result - properly split by newlines
			string[] lines = result.Output.Split('\n');
			foreach (string line in lines)
			{
				// Remove any CR characters that might be in the output
				string cleanLine = line.Replace("\r", "");

				_buffer.AddLine(new TerminalLine(
					cleanLine,
					result.Color,
					null,
					result.IsSuccessful ? TerminalLineType.Output : TerminalLineType.Error));
			}
		}

		// Reset and display new prompt
		_currentInput.Clear();
		_cursorPosition = 0;
		DisplayPrompt();
	}

	/// <summary>
	/// Updates the input line display.
	/// </summary>
	private void UpdateInputLine()
	{
		_buffer.UpdateLastLine(new TerminalLine(
			CurrentPrompt + _currentInput.ToString(),
			_settings.InputColor,
			null,
			TerminalLineType.Input));
	}
}

/// <summary>
/// Tracks cursor state for the terminal.
/// </summary>
struct CursorState
{
	/// <summary>
	/// Gets or sets the position of the cursor within the current line.
	/// </summary>
	public int Position { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the cursor is visible.
	/// </summary>
	public bool IsVisible { get; set; }

	/// <summary>
	/// Gets or sets the elapsed time since the last cursor blink.
	/// </summary>
	public float BlinkTime { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="CursorState"/> struct.
	/// </summary>
	/// <param name="position">The initial cursor position.</param>
	public CursorState(int position = 0)
	{
		Position = position;
		IsVisible = true;
		BlinkTime = 0;
	}

	/// <summary>
	/// Updates the cursor blink state.
	/// </summary>
	/// <param name="deltaTime">The elapsed time since the last update.</param>
	/// <param name="blinkRate">The cursor blink rate in seconds.</param>
	public void Update(float deltaTime, float blinkRate)
	{
		BlinkTime += deltaTime;
		if (BlinkTime >= blinkRate)
		{
			BlinkTime -= blinkRate;
			IsVisible = !IsVisible;
		}
	}
}