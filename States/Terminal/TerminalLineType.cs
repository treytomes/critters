namespace Critters.States.Terminal;

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
