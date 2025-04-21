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

	public ContentPresenter(UIElement? parent, IResourceManager resources, IEventBus eventBus, IRenderingContext rc)
		: base(parent, resources, eventBus, rc)
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
						_content.Unload();
					}
					_content.PropertyChanged -= OnContentPropertyChanged;
				}

				_content = value;

				if (_content != null)
				{
					if (IsLoaded)
					{
						_content.Load();
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

	public override void Load()
	{
		if (IsLoaded)
		{
			return;
		}

		base.Load();
		Content?.Load();
	}

	public override void Unload()
	{
		if (!IsLoaded)
		{
			return;
		}

		base.Unload();
		Content?.Unload();
	}

	public sealed override void Render(GameTime gameTime)
	{
		base.Render(gameTime);
		RenderSelf(gameTime);
		Content?.Render(gameTime);
	}

	protected virtual void RenderSelf(GameTime gameTime)
	{
	}

	protected void RenderContent(GameTime gameTime)
	{
		Content?.Render(gameTime);
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