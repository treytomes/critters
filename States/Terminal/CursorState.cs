namespace Critters.States.Terminal;

/// <summary>
/// Tracks cursor state for the terminal.
/// </summary>
struct CursorState
{
	/// <summary>
	/// Gets or sets the position of the cursor within the current line.
	/// </summary>
	public int Position { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the cursor is visible.
	/// </summary>
	public bool IsVisible { get; set; }

	/// <summary>
	/// Gets or sets the elapsed time since the last cursor blink.
	/// </summary>
	public float BlinkTime { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="CursorState"/> struct.
	/// </summary>
	/// <param name="position">The initial cursor position.</param>
	public CursorState(int position = 0)
	{
		Position = position;
		IsVisible = true;
		BlinkTime = 0;
	}

	/// <summary>
	/// Updates the cursor blink state.
	/// </summary>
	/// <param name="deltaTime">The elapsed time since the last update.</param>
	/// <param name="blinkRate">The cursor blink rate in seconds.</param>
	public void Update(float deltaTime, float blinkRate)
	{
		BlinkTime += deltaTime;
		if (BlinkTime >= blinkRate)
		{
			BlinkTime -= blinkRate;
			IsVisible = !IsVisible;
		}
	}
}