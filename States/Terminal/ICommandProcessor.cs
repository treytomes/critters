namespace Critters.States.Terminal;

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
