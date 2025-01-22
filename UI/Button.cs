using Critters.Events;
using Critters.Gfx;
using Critters.IO;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Critters.UI;

// TODO: Button styles for 3D or Flat.

class Button : UIElement
{
	#region Constants

	private const int PADDING_H = 8;
	private const int PADDING_V = 0;
	private const int SHADOW_OFFSET = 4;

	#endregion

	#region Fields

	private static int _nextId = 0;
	public readonly int Id;
	private UIElement? _content;
	private bool _hasMouseHover = false;
	private bool _hasMouseFocus = false;
	private EventBus? _eventBus = null;

	#endregion

	#region Constructors

	public Button(Vector2 position)
		: this(null, position)
	{
	}
	
	public Button(UIElement? parent, Vector2 position)
		: base(parent)
	{
		Id = _nextId++;
		Position = position;
		Padding = new(PADDING_H, PADDING_V);
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
		eventBus.Subscribe<MouseButtonEventArgs>(OnMouseClick);
		_eventBus = eventBus;
	}

	public override void Unload(ResourceManager resources, EventBus eventBus)
	{
		Content?.Unload(resources, eventBus);
		eventBus.Unsubscribe<MouseMoveEventArgs>(OnMouseMove);
	}

	public override void Render(RenderingContext rc, GameTime gameTime)
	{
		if (!_hasMouseFocus)
		{
			// Render the drop-shadow.
			rc.RenderFilledRect(AbsoluteBounds.Min + new Vector2(SHADOW_OFFSET, SHADOW_OFFSET), AbsoluteBounds.Max + new Vector2(SHADOW_OFFSET, SHADOW_OFFSET), 0);
		}
		
		var color = rc.Palette[2, 2, 2];
		if (_hasMouseFocus)
		{
			color = rc.Palette[4, 4, 4];
		}
		else if (_hasMouseHover)
		{
			color = rc.Palette[3, 3, 3];
		}
		rc.RenderFilledRect(AbsoluteBounds, color);
		Content?.Render(rc, gameTime);
	}

	private void OnMouseMove(MouseMoveEventArgs e)
	{
		_hasMouseHover = AbsoluteBounds.ContainsInclusive(e.Position);
	}

	private void OnMouseClick(MouseButtonEventArgs e)
	{
		if (e.Button == MouseButton.Left)
		{
			if (e.Action == InputAction.Press)
			{
				if (_hasMouseHover)
				{
					Margin = new(SHADOW_OFFSET, SHADOW_OFFSET, 0, 0);
					_hasMouseFocus = true;
				}
			}
			else if (e.Action == InputAction.Release)
			{
				if (_hasMouseFocus && _hasMouseHover)
				{
					_eventBus?.Publish(new ButtonPressedEventArgs(Id));
				}
				Margin = new(0);
				_hasMouseFocus = false;
			}
		}
	}

	#endregion
}