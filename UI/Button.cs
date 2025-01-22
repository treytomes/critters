using Critters.Events;
using Critters.Gfx;
using Critters.IO;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;

namespace Critters.UI;

class Button : UIElement
{
	#region Fields

	private UIElement? _content;
	private bool _hasMouseHover = false;

	#endregion

	#region Constructors

	public Button(Vector2 position)
		: this(null, position)
	{
	}
	
	public Button(UIElement? parent, Vector2 position)
		: base(parent)
	{
		Position = position;
		Padding = new(8, 0);
	}

	#endregion

	#region Properties

	public UIElement? Content
	{
		get
		{
			return _content;
		}
		set
		{
			_content = value;
			if (_content != null)
			{
				_content.Parent = this;
			}
		}
	}

	#endregion

	#region Methods

	public override void Load(ResourceManager resources, EventBus eventBus)
	{
		if (Content != null)
		{
			Content.Load(resources, eventBus);

			// TODO: Updating Padding should automatically update Size.  I think.
			Size = Content.Size + new Vector2(Padding.Left + Padding.Right, Padding.Top + Padding.Bottom);
		}
		eventBus.Subscribe<MouseMoveEventArgs>(OnMouseMove);
	}

	public override void Unload(ResourceManager resources, EventBus eventBus)
	{
		Content?.Unload(resources, eventBus);
		eventBus.Unsubscribe<MouseMoveEventArgs>(OnMouseMove);
	}

	public override void Render(RenderingContext rc, GameTime gameTime)
	{
		if (_hasMouseHover)
		{
			rc.RenderFilledRect(AbsoluteBounds.Min + new Vector2(4, 4), AbsoluteBounds.Max + new Vector2(4, 4), 0);
			rc.RenderFilledRect(AbsoluteBounds, rc.Palette[3, 3, 3]);
		}
		else
		{
			rc.RenderFilledRect(AbsoluteBounds.Min + new Vector2(4, 4), AbsoluteBounds.Max + new Vector2(4, 4), 0);
			rc.RenderFilledRect(AbsoluteBounds, rc.Palette[2, 2, 2]);
		}
		Content?.Render(rc, gameTime);
	}

	private void OnMouseMove(MouseMoveEventArgs e)
	{
		_hasMouseHover = AbsoluteBounds.ContainsInclusive(e.Position);
	}

	#endregion
}