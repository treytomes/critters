using Critters.Gfx;

namespace Critters.States.Terminal;

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
