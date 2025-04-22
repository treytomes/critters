using Critters.Gfx;

namespace Critters.States.Terminal;

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
