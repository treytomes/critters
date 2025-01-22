using System.ComponentModel;
using Critters.Events;
using Critters.Gfx;
using Critters.IO;
using OpenTK.Mathematics;

namespace Critters.UI;

class ContentPresenter : UIElement
{
	#region Fields

	// The resource manager and event bus need to be stored here so we can load new content as needed.
	private ResourceManager? _resourceManager = null;
	private EventBus? _eventBus = null;

	private UIElement? _content = null;

	#endregion

	#region Constructors

	public ContentPresenter()
		: this(null)
	{
	}

	public ContentPresenter(UIElement? parent)
		: base(parent)
	{
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
			if (_content != value)
			{
				if (_content != null)
				{
					_content.PropertyChanged -= OnContentPropertyChanged;
				}

				_content = value;

				if (_content != null)
				{
					_content.PropertyChanged += OnContentPropertyChanged;
					_content.Parent = this;
				}
				OnPropertyChanged();
			}
		}
	}

    #endregion

    #region Methods

	public override void Load(ResourceManager resources, EventBus eventBus)
	{
		base.Load(resources, eventBus);
		Content?.Load(resources, eventBus);
		if (Content != null)
		{
			// TODO: Updating Padding should automatically update Size.  I think.
			Size = Content.Size + new Vector2(Padding.Left + Padding.Right, Padding.Top + Padding.Bottom);
		}
	}

	public override void Unload(ResourceManager resources, EventBus eventBus)
	{
		base.Unload(resources, eventBus);
		Content?.Unload(resources, eventBus);
	}

	public sealed override void Render(RenderingContext rc, GameTime gameTime)
	{
		base.Render(rc, gameTime);
		RenderSelf(rc, gameTime);
		Content?.Render(rc, gameTime);
	}

	protected virtual void RenderSelf(RenderingContext rc, GameTime gameTime)
	{
	}

	protected void RenderContent(RenderingContext rc, GameTime gameTime)
	{
		Content?.Render(rc, gameTime);
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);
		Content?.Update(gameTime);
	}

	protected virtual void OnContentPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(Size))
		{
			Size = Content!.Size + new Vector2(Padding.Left + Padding.Right, Padding.Top + Padding.Bottom);
		}
	}

	#endregion
}