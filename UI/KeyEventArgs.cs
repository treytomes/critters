using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Critters.IO;

readonly struct KeyEventArgs
{
	/// <summary>
	/// Initializes a new instance of the <see cref="KeyEventArgs"/> struct.
	/// </summary>
	/// <param name="key">The key that generated this event.</param>
	/// <param name="scanCode">The scan code of the key that generated this event.</param>
	/// <param name="modifiers">The key modifiers that were active when this event was generated.</param>
	/// <param name="isRepeat">Whether this event is a repeat from the user holding the key down.</param>
	public KeyEventArgs(Keys key, int scanCode, KeyModifiers modifiers, bool isRepeat, bool isPressed)
	{
		Key = key;
		ScanCode = scanCode;
		Modifiers = modifiers;
		IsRepeat = isRepeat;
		IsPressed = isPressed;
	}

	/// <summary>
	/// Gets the key that generated this event.
	/// </summary>
	public Keys Key { get; }

	/// <summary>
	/// Gets the keyboard scan code of the key that generated this event.
	/// </summary>
	public int ScanCode { get; }

	/// <summary>
	/// Gets a bitwise combination representing the key modifiers were active when this event was generated.
	/// </summary>
	public KeyModifiers Modifiers { get; }

	/// <summary>
	/// Gets a value indicating whether
	/// this key event is a repeat.
	/// </summary>
	/// <value>
	/// true, if this event was caused by the user holding down
	/// a key; false, if this was caused by the user pressing a
	/// key for the first time.
	/// </value>
	public bool IsRepeat { get; }

	/// <summary>
	/// Gets a value indicating whether this key event was caused by the user pressing the key.
	/// </summary>
	/// <value>
	/// true, if this event was caused by the user pressing a key;
	/// false, if this was caused by the user releasing a key
	/// </value>
	public bool IsPressed { get; }

	/// <summary>
	/// Gets a value indicating whether <see cref="OpenTK.Windowing.Common.Input.KeyModifiers.Alt" /> is pressed.
	/// </summary>
	/// <value><c>true</c> if pressed; otherwise, <c>false</c>.</value>
	public bool Alt => Modifiers.HasFlag(KeyModifiers.Alt);

	/// <summary>
	/// Gets a value indicating whether <see cref="OpenTK.Windowing.Common.Input.KeyModifiers.Control" /> is pressed.
	/// </summary>
	/// <value><c>true</c> if pressed; otherwise, <c>false</c>.</value>
	public bool Control => Modifiers.HasFlag(KeyModifiers.Control);

	/// <summary>
	/// Gets a value indicating whether <see cref="OpenTK.Windowing.Common.Input.KeyModifiers.Shift" /> is pressed.
	/// </summary>
	/// <value><c>true</c> if pressed; otherwise, <c>false</c>.</value>
	public bool Shift => Modifiers.HasFlag(KeyModifiers.Shift);

	/// <summary>
	/// Gets a value indicating whether <see cref="OpenTK.Windowing.Common.Input.KeyModifiers.Super" /> is pressed.
	/// </summary>
	/// <value><c>true</c> if pressed; otherwise, <c>false</c>.</value>
	public bool Command => Modifiers.HasFlag(KeyModifiers.Super);
}