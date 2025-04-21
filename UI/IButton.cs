using Critters.Events;

namespace Critters.UI;

/// <summary>
/// Represents a clickable button UI element that can contain content.
/// </summary>
interface IButton
{
	/// <summary>
	/// Gets an observable sequence of button click events.
	/// </summary>
	/// <remarks>
	/// This observable can be used with System.Reactive to subscribe to button clicks
	/// in a more flexible way than traditional events.
	/// </remarks>
	IObservable<ButtonClickedEventArgs> ClickEvents { get; }

	/// <summary>
	/// Gets or sets custom data associated with this button.
	/// </summary>
	/// <remarks>
	/// This property can be used to store any application-specific data that should be
	/// associated with the button. The metadata will be included in click events.
	/// </remarks>
	object? Metadata { get; set; }

	/// <summary>
	/// Gets or sets the content element displayed within the button.
	/// </summary>
	/// <remarks>
	/// The content can be any UIElement, such as text, an image, or a more complex
	/// composite element. The button will automatically adjust its size based on the
	/// content's size plus padding.
	/// </remarks>
	UIElement? Content { get; set; }
}
