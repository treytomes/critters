namespace Critters.UI;

/// <summary>
/// Defines the visual styles available for buttons.
/// </summary>
public enum ButtonStyle
{
	/// <summary>
	/// A simple flat button with no shadow effects.
	/// </summary>
	/// <remarks>
	/// Flat buttons are visually simpler and take up less space. They're suitable for
	/// toolbars, compact UIs, or when buttons should be less prominent.
	/// </remarks>
	Flat,

	/// <summary>
	/// A button with a drop shadow that appears raised from the surface.
	/// </summary>
	/// <remarks>
	/// Raised buttons have a more prominent appearance and provide better visual feedback
	/// when pressed (the shadow disappears). They're suitable for primary actions.
	/// </remarks>
	Raised
}