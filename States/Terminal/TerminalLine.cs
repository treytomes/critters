using Critters.Gfx;

namespace Critters.States.Terminal;

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
