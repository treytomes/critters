namespace Critters.States.Terminal;

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
