namespace Critters.States.Terminal;

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
