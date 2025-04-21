namespace Critters.Events;

/// <summary>
/// Provides data for the <see cref="IButton.Clicked"/> event.
/// </summary>
public class ButtonClickedEventArgs : EventArgs
{
	public ButtonClickedEventArgs(object? metadata = null)
	{
		Metadata = metadata;
		Handled = false;
	}

	/// <summary>
	/// Gets or sets custom data associated with the button click.
	/// </summary>
	/// <remarks>
	/// This property contains the value of the <see cref="IButton.Metadata"/> property
	/// at the time the button was clicked.
	/// </remarks>
	public object? Metadata { get; set; }

	/// <summary>
	/// Gets or sets whether the click event has been handled.
	/// </summary>
	/// <remarks>
	/// Setting this property to true indicates that the event has been handled and should
	/// not be processed by other handlers.
	/// </remarks>
	public bool Handled { get; set; }
}
