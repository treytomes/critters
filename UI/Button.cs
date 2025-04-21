using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Disposables;
using Critters.Core;
using Critters.Events;
using Critters.Gfx;
using Critters.Services;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Critters.UI;

class Button : UIElement, IButton
{
	#region Constants

	private const int SHADOW_OFFSET = 2;

	#endregion

	#region Fields

	/// <summary>
	/// Sets the visual style of the button.
	/// </summary>
	/// <remarks>
	/// Different button styles have different visual appearances and behaviors:
	/// - Flat: A simple rectangular button without shadows
	/// - Raised: A button with a drop shadow that appears to be raised from the surface
	/// </remarks>
	private readonly ButtonStyle _style;

	private bool _hasMouseHover = false;
	private bool _hasMouseFocus = false;
	private UIElement? _content;
	private readonly Subject<ButtonClickedEventArgs> _clickedSubject = new();
	private readonly CompositeDisposable _contentSubscriptions = new();

	#endregion

	#region Observables

	public IObservable<ButtonClickedEventArgs> ClickEvents => _clickedSubject.AsObservable();

	#endregion

	#region Constructors

	public Button(IResourceManager resources, IRenderingContext rc, ButtonStyle style = ButtonStyle.Raised)
		: base(resources, rc)
	{
		_style = style;

		if (_style == ButtonStyle.Flat)
		{
			Padding = new(3, 1, 0, 0);
		}
		else
		{
			// Padding = new(8, 10, 8, 10);
			Padding = new(3, 2, 3, 1);
		}

		// Clean up subscriptions and subjects when disposed
		DisposalEvents.Subscribe(e =>
		{
			if (e.Stage == DisposalStage.Started)
			{
				_clickedSubject.OnCompleted();
				_contentSubscriptions.Dispose();
			}
		});
	}

	#endregion

	#region Properties

	public object? Metadata { get; set; } = null;

	public UIElement? Content
	{
		get => _content;
		set
		{
			ThrowIfDisposed();

			if (_content == value)
				return;

			// Clear existing subscriptions
			_contentSubscriptions.Clear();

			// Remove old content
			if (_content != null)
			{
				RemoveChild(_content);
			}

			_content = value;

			// Add new content
			if (_content != null)
			{
				AddChild(_content);

				// Subscribe to content size changes
				_content.SizeChanged
					.Subscribe(newSize => UpdateContentSize())
					.AddTo(_contentSubscriptions);

				// Also subscribe to content's position changes as they might affect layout
				_content.WhenPropertyChanges(nameof(Position), c => c.Position)
					.Subscribe(_ => UpdateContentSize())
					.AddTo(_contentSubscriptions);

				// Subscribe to content's padding changes
				_content.WhenPropertyChanges(nameof(Padding), c => c.Padding)
					.Subscribe(_ => UpdateContentSize())
					.AddTo(_contentSubscriptions);

				// Initial size update
				UpdateContentSize();
			}
			else
			{
				ContentSize = Vector2.Zero;
			}

			OnPropertyChanged();
		}
	}

	#endregion

	#region Methods

	/// <summary>
	/// Updates the button's content size based on its content element
	/// </summary>
	private void UpdateContentSize()
	{
		if (_content != null && !IsDisposed && !_content.IsDisposed)
		{
			ContentSize = _content.Size;
		}
	}

	public override void Render(GameTime gameTime)
	{
		ThrowIfDisposed();

		if (!IsVisible)
			return;

		if (_style == ButtonStyle.Flat)
		{
			RenderFlat(gameTime);
		}
		else if (_style == ButtonStyle.Raised)
		{
			RenderRaised(gameTime);
		}

		// Render children (including content)
		base.Render(gameTime);
	}

	private void RenderFlat(GameTime gameTime)
	{
		var color = RC.Palette[2, 2, 2];
		if (_hasMouseFocus)
		{
			color = RC.Palette[4, 4, 4];
		}
		else if (_hasMouseHover)
		{
			color = RC.Palette[3, 3, 3];
		}
		RC.RenderFilledRect(AbsoluteBounds, color);
	}

	private void RenderRaised(GameTime gameTime)
	{
		if (!_hasMouseFocus)
		{
			// Render the drop-shadow.
			RC.RenderFilledRect(
				AbsoluteBounds.Min + new Vector2(SHADOW_OFFSET, SHADOW_OFFSET),
				AbsoluteBounds.Max + new Vector2(SHADOW_OFFSET, SHADOW_OFFSET),
				RC.Palette[0, 0, 0]
			);
		}

		var color = RC.Palette[2, 2, 2];
		if (_hasMouseFocus)
		{
			color = RC.Palette[4, 4, 4];
		}
		else if (_hasMouseHover)
		{
			color = RC.Palette[3, 3, 3];
		}
		RC.RenderFilledRect(AbsoluteBounds, color);
	}

	public override bool MouseMove(MouseMoveEventArgs e)
	{
		ThrowIfDisposed();

		bool wasHovering = _hasMouseHover;
		_hasMouseHover = AbsoluteBounds.ContainsInclusive(e.Position);

		// If hover state changed, we need to handle it
		bool hoverChanged = wasHovering != _hasMouseHover;

		// Process child events first
		bool childHandled = base.MouseMove(e);

		// Return true if either a child handled it or the hover state changed
		return childHandled || hoverChanged || _hasMouseHover;
	}

	public override bool MouseDown(MouseButtonEventArgs e)
	{
		ThrowIfDisposed();

		if (e.Button == MouseButton.Left && _hasMouseHover)
		{
			if (_style == ButtonStyle.Raised)
			{
				Margin = new(SHADOW_OFFSET, SHADOW_OFFSET, 0, 0);
			}
			_hasMouseFocus = true;
			return true;
		}

		return base.MouseDown(e);
	}

	public override bool MouseUp(MouseButtonEventArgs e)
	{
		ThrowIfDisposed();

		if (e.Button == MouseButton.Left)
		{
			bool wasClicked = _hasMouseFocus && _hasMouseHover;

			if (_style == ButtonStyle.Raised)
			{
				Margin = new(0);
			}

			_hasMouseFocus = false;

			if (wasClicked)
			{
				var args = new ButtonClickedEventArgs(Metadata);
				_clickedSubject.OnNext(args);
				return true;
			}
		}

		return base.MouseUp(e);
	}

	protected override void DisposeManagedResources()
	{
		_clickedSubject.Dispose();
		_contentSubscriptions.Dispose();
		base.DisposeManagedResources();
	}

	#endregion
}
