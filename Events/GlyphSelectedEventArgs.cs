namespace Critters.Events;

/// <summary>
/// Event args for glyph selection events
/// </summary>
public class GlyphSelectedEventArgs : EventArgs
{
	public byte GlyphIndex { get; }

	public GlyphSelectedEventArgs(byte glyphIndex)
	{
		GlyphIndex = glyphIndex;
	}
}