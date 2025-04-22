using Critters.Gfx;
using Critters.Services;
using Critters.States.Terminal;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

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
			case Keys.Escape:
				Leave();
				return true;

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
