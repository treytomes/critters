using Critters.Events;
using Critters.Gfx;
using Critters.IO;
using Critters.UI;
using OpenTK.Mathematics;

namespace Critters.States;

/// <summary>
/// Load up an existing glyph, or draw a new one.  Select the colors, save to disk.
/// </summary>
class GlyphEditState : GameState
{
	#region Fields

	private List<UIElement> _ui = new List<UIElement>();
	
	#endregion

	#region Constructors

	public GlyphEditState()
	{
		_ui.Add(new Label("Foreground", new Vector2(0, 0), Palette.GetIndex(5, 5, 5), Palette.GetIndex(0, 0, 0)));
		_ui.Add(new ColorPicker(new Vector2(0, 8)));

		_ui.Add(new Label("Background", new Vector2(100, 0), Palette.GetIndex(5, 5, 5), Palette.GetIndex(0, 0, 0)));
		_ui.Add(new ColorPicker(new Vector2(100, 8)));

	}

	#endregion

	#region Properties

	#endregion

	#region Methods

	public override void Load(ResourceManager resources, EventBus eventBus)
	{
		base.Load(resources, eventBus);

		foreach (var ui in _ui)
		{
			ui.Load(resources, eventBus);
		}
	}

	public override void Unload(ResourceManager resources, EventBus eventBus)
	{
		base.Unload(resources, eventBus);

		foreach (var ui in _ui)
		{
			ui.Unload(resources, eventBus);
		}
	}


	public override void Render(RenderingContext rc, GameTime gameTime)
	{
		base.Render(rc, gameTime);

		rc.Fill(0);

		foreach (var ui in _ui)
		{
			ui.Render(rc, gameTime);
		}
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);

		foreach (var ui in _ui)
		{
			ui.Update(gameTime);
		}
	}

	#endregion
}
