using System.ComponentModel;
using Critters.Gfx;
using Critters.Services;
using OpenTK.Mathematics;

namespace Critters.UI;

class ContentPresenter : UIElement
{
	#region Fields

	// The resource manager and event bus need to be stored here so we can load new content as needed.
	private IResourceManager? _resourceManager = null;
	private IEventBus? _eventBus = null;

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
					if (IsLoaded)
					{
						_content.Unload(_resourceManager!, _eventBus!);
					}
					_content.PropertyChanged -= OnContentPropertyChanged;
				}

				_content = value;

				if (_content != null)
				{
					if (IsLoaded)
					{
						_content.Load(_resourceManager!, _eventBus!);
					}
					_content.PropertyChanged += OnContentPropertyChanged;
					_content.Parent = this;
				}
				OnPropertyChanged();
			}
		}
	}

	#endregion

	#region Methods

	public override void Load(IResourceManager resources, IEventBus eventBus)
	{
		if (IsLoaded)
		{
			return;
		}

		base.Load(resources, eventBus);
		Content?.Load(resources, eventBus);
	}

	public override void Unload(IResourceManager resources, IEventBus eventBus)
	{
		if (!IsLoaded)
		{
			return;
		}

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