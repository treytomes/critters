using System.Text;

namespace Critters.States.Terminal;

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
	private bool _isInString = false;
	private char _stringDelimiter = '"';

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

		// Process string literals and line continuation
		bool endsWithBackslash = input.EndsWith("\\");

		// Track string state by counting unescaped quotes
		UpdateStringState(input);

		// Handle line continuation
		if (endsWithBackslash || _isInString)
		{
			if (!_isMultiLine)
			{
				_isMultiLine = true;
				_multiLineBuffer.Clear();
			}

			if (endsWithBackslash && !_isInString)
			{
				// Outside of string - remove backslash and add space
				_multiLineBuffer.Append(input.Substring(0, input.Length - 1));
				_multiLineBuffer.Append(' '); // Add space for concatenation
			}
			else if (endsWithBackslash && _isInString)
			{
				// Inside string with backslash - keep backslash if it's escaping a quote
				if (input.EndsWith("\\\\"))
				{
					// Double backslash - keep both and add newline
					_multiLineBuffer.Append(input);
					_multiLineBuffer.Append('\n');
				}
				else
				{
					// Single backslash - remove it and add newline
					_multiLineBuffer.Append(input.Substring(0, input.Length - 1));
					_multiLineBuffer.Append('\n');
				}
			}
			else
			{
				// In string without backslash - add with newline
				_multiLineBuffer.Append(input);
				_multiLineBuffer.Append('\n');
			}

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
			_isInString = false;
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
	/// Updates the string state by analyzing quotes in the input.
	/// </summary>
	/// <param name="input">The input text to analyze.</param>
	private void UpdateStringState(string input)
	{
		if (string.IsNullOrEmpty(input))
			return;

		bool escaped = false;

		for (int i = 0; i < input.Length; i++)
		{
			char c = input[i];

			if (escaped)
			{
				// This character is escaped, so it doesn't affect string state
				escaped = false;
				continue;
			}

			if (c == '\\')
			{
				// Next character will be escaped
				escaped = true;
				continue;
			}

			if (c == _stringDelimiter)
			{
				// Toggle string state
				_isInString = !_isInString;
			}
		}
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
