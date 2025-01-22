namespace Critters.UI;

public readonly struct ButtonPressedEventArgs
{
	public readonly int ButtonId;

	public ButtonPressedEventArgs(int buttonId)
	{
		ButtonId = buttonId;
	}
}