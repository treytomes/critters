using Critters.Events;
using Critters.Gfx;
using Critters.IO;
using Critters.States;
using OpenTK.Mathematics;

namespace Critters.UI;

/// <summary>
/// UI element position and size is measured in tiles.
/// The PixelBounds property will convert to virtual display pixels for things like mouse hover check.
/// </summary>
class UIElement : IGameComponent
{
	#region Constructors

	public UIElement(UIElement? parent = null)
	{
		Parent = parent;
	}

	#endregion

	#region Properties

	public UIElement? Parent { get; set; }
	public Vector2 Position { get; protected set; }
	public Vector2 Size { get; protected set; }
	public Box2 Bounds => new(Position, Position + Size);
	public Thickness Padding = new(0);
	public Thickness Margin = new(0);

	public Vector2 AbsolutePosition
	{
		get
		{
			var position = new Vector2(Margin.Left, Margin.Top) + Position;
			if (Parent != null)
			{
				return Parent.AbsolutePosition + new Vector2(Parent.Padding.Left, Parent.Padding.Top) + position;
			}
			else
			{
				return position;
			}
		}
	}

	public Box2 AbsoluteBounds => new(AbsolutePosition, AbsolutePosition + Size);

	#endregion

	#region Methods

	public virtual void Load(ResourceManager resources, EventBus eventBus)
	{
	}

	public virtual void Render(RenderingContext rc, GameTime gameTime)
	{
	}

	public virtual void Unload(ResourceManager resources, EventBus eventBus)
	{
	}

	public virtual void Update(GameTime gameTime)
	{
	}

	#endregion
}
