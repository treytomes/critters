using Critters.Gfx;

namespace Critters.States.Terminal;

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
