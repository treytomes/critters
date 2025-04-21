using System.ComponentModel;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using Critters.Core;
using Critters.Events;
using Critters.Gfx;
using Critters.Services;
using Critters.States;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;

namespace Critters.UI;

/// <summary>
/// UI element position and size is measured in tiles.
/// The PixelBounds property will convert to virtual display pixels for things like mouse hover check.
/// </summary>
class UIElement : Disposable, IGameComponent, INotifyPropertyChanged, IEventHandler
{
	#region Subjects

	private readonly Subject<UIElement> _childAddedSubject = new();
	private readonly Subject<UIElement> _childRemovedSubject = new();
	private readonly Subject<PropertyChangedEventArgs> _propertyChangedSubject = new();
	private readonly Subject<Vector2> _sizeChangedSubject = new();

	#endregion

	#region Events

	public event PropertyChangedEventHandler? PropertyChanged;

	#endregion

	#region Observables

	public IObservable<UIElement> ChildAdded => _childAddedSubject.AsObservable();
	public IObservable<UIElement> ChildRemoved => _childRemovedSubject.AsObservable();
	public IObservable<PropertyChangedEventArgs> PropertyChanges => _propertyChangedSubject.AsObservable();
	public IObservable<Vector2> SizeChanged => _sizeChangedSubject.AsObservable();

	#endregion

	#region Fields

	private readonly IResourceManager _resources;
	private readonly IRenderingContext _rc;
	private readonly List<UIElement> _children = new();
	private IDisposable _parentPropertyChangesSubscription;
	private bool _isLoaded = false;
	private UIElement? _parent;
	private Vector2 _position = Vector2.Zero;
	private Vector2 _contentSize = Vector2.Zero;
	private Thickness _padding = new(0);
	private Thickness _margin = new(0);
	private bool _isVisible = true;
	private int _zIndex = 0;

	#endregion

	#region Constructors

	public UIElement(IResourceManager resources, IRenderingContext rc)
	{
		_resources = resources ?? throw new ArgumentNullException(nameof(resources));
		_rc = rc ?? throw new ArgumentNullException(nameof(rc));

		// Empty subscription to initialize
		_parentPropertyChangesSubscription = Observable.Empty<PropertyChangedEventArgs>().Subscribe();

		// Subscribe to disposal events to clean up when disposed
		DisposalEvents.Subscribe(e =>
		{
			if (e.Stage == DisposalStage.Started)
			{
				// Unsubscribe from parent property changes
				_parentPropertyChangesSubscription.Dispose();

				// Complete all subjects
				_childAddedSubject.OnCompleted();
				_childRemovedSubject.OnCompleted();
				_propertyChangedSubject.OnCompleted();
				_sizeChangedSubject.OnCompleted();
			}
		});
	}

	#endregion

	#region Properties

	protected IResourceManager Resources => _resources;
	protected IRenderingContext RC => _rc;

	public bool IsLoaded
	{
		get => _isLoaded;
		private set
		{
			if (_isLoaded != value)
			{
				_isLoaded = value;
				OnPropertyChanged();
			}
		}
	}

	public UIElement? Parent
	{
		get => _parent;
		private set
		{
			ThrowIfDisposed();

			if (_parent != value)
			{
				// Dispose old subscription
				_parentPropertyChangesSubscription.Dispose();

				_parent = value;

				// Create new subscription if parent exists
				if (_parent != null)
				{
					_parentPropertyChangesSubscription = _parent.PropertyChanges
						.Subscribe(OnParentPropertyChanged);
				}
				else
				{
					// Empty subscription as placeholder
					_parentPropertyChangesSubscription = Observable.Empty<PropertyChangedEventArgs>().Subscribe();
				}

				OnPropertyChanged();
			}
		}
	}

	public IReadOnlyList<UIElement> Children => _children.AsReadOnly();

	public Vector2 Position
	{
		get => _position;
		set
		{
			ThrowIfDisposed();

			if (_position != value)
			{
				_position = value;
				OnPropertyChanged();
			}
		}
	}

	public Vector2 ContentSize
	{
		get => _contentSize;
		set
		{
			ThrowIfDisposed();

			if (_contentSize != value)
			{
				_contentSize = value;
				OnPropertyChanged();

				// Size depends on ContentSize, so it changes too
				var newSize = Size;
				OnPropertyChanged(nameof(Size));
				OnPropertyChanged(nameof(Bounds));

				// Emit the size change event
				_sizeChangedSubject.OnNext(newSize);
			}
		}
	}

	public Vector2 Size
	{
		get => new(
			_contentSize.X + Padding.Left + Padding.Right,
			_contentSize.Y + Padding.Top + Padding.Bottom);
	}

	public Box2 Bounds
	{
		get => new(Position, Position + Size);
	}

	public Thickness Padding
	{
		get => _padding;
		set
		{
			ThrowIfDisposed();

			if (_padding != value)
			{
				var oldSize = Size;
				_padding = value;
				OnPropertyChanged();

				var newSize = Size;
				if (oldSize != newSize)
				{
					OnPropertyChanged(nameof(Size));
					OnPropertyChanged(nameof(Bounds));

					// Emit the size change event
					_sizeChangedSubject.OnNext(newSize);
				}
			}
		}
	}

	public Thickness Margin
	{
		get => _margin;
		set
		{
			ThrowIfDisposed();

			if (_margin != value)
			{
				_margin = value;
				OnPropertyChanged();
			}
		}
	}

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

	public Box2 ContentBounds => new(
		AbsolutePosition + new Vector2(Padding.Left, Padding.Top),
		AbsolutePosition + Size - new Vector2(Padding.Right, Padding.Bottom)
	);

	public bool IsVisible
	{
		get => _isVisible;
		set
		{
			ThrowIfDisposed();

			if (_isVisible != value)
			{
				_isVisible = value;
				OnPropertyChanged();
			}
		}
	}

	public int ZIndex
	{
		get => _zIndex;
		set
		{
			ThrowIfDisposed();

			if (_zIndex != value)
			{
				_zIndex = value;
				OnPropertyChanged();
			}
		}
	}

	#endregion

	#region Child Management

	public void AddChild(UIElement child)
	{
		ThrowIfDisposed();

		if (child == null) throw new ArgumentNullException(nameof(child));
		if (child == this) throw new InvalidOperationException("Cannot add element as its own child");
		if (child._parent != null) throw new InvalidOperationException("Element already has a parent");
		if (child.IsDisposed) throw new InvalidOperationException("Cannot add a disposed element as a child");

		_children.Add(child);
		child.Parent = this;

		if (IsLoaded && !child.IsLoaded)
		{
			child.Load();
		}

		_childAddedSubject.OnNext(child);
	}

	public bool RemoveChild(UIElement child)
	{
		ThrowIfDisposed();

		if (child == null) throw new ArgumentNullException(nameof(child));

		if (_children.Remove(child))
		{
			child.Parent = null;

			if (child.IsLoaded)
			{
				child.Unload();
			}

			_childRemovedSubject.OnNext(child);
			return true;
		}

		return false;
	}

	public void ClearChildren()
	{
		ThrowIfDisposed();

		while (_children.Count > 0)
		{
			RemoveChild(_children[0]);
		}
	}

	#endregion

	#region IGameComponent Implementation

	public virtual void Load()
	{
		ThrowIfDisposed();

		if (IsLoaded)
		{
			return;
		}

		foreach (var child in _children)
		{
			child.Load();
		}

		IsLoaded = true;
	}

	public virtual void Unload()
	{
		ThrowIfDisposed();

		if (!IsLoaded)
		{
			return;
		}

		foreach (var child in _children)
		{
			child.Unload();
		}

		IsLoaded = false;
	}

	public virtual void Render(GameTime gameTime)
	{
		ThrowIfDisposed();

		if (!IsVisible)
		{
			return;
		}

		// Sort children by Z-index before rendering
		var sortedChildren = new List<UIElement>(_children);
		sortedChildren.Sort((a, b) => a.ZIndex.CompareTo(b.ZIndex));

		foreach (var child in sortedChildren)
		{
			child.Render(gameTime);
		}
	}

	public virtual void Update(GameTime gameTime)
	{
		ThrowIfDisposed();

		foreach (var child in _children)
		{
			child.Update(gameTime);
		}
	}

	#endregion

	#region Event Handling

	public virtual bool KeyDown(KeyboardKeyEventArgs e)
	{
		ThrowIfDisposed();

		// Process from front to back (reverse Z-order)
		var sortedChildren = new List<UIElement>(_children);
		sortedChildren.Sort((a, b) => b.ZIndex.CompareTo(a.ZIndex));

		foreach (var child in sortedChildren)
		{
			if (child.IsVisible && !child.IsDisposed && child.KeyDown(e))
			{
				return true;
			}
		}

		return false;
	}

	public virtual bool KeyUp(KeyboardKeyEventArgs e)
	{
		ThrowIfDisposed();

		var sortedChildren = new List<UIElement>(_children);
		sortedChildren.Sort((a, b) => b.ZIndex.CompareTo(a.ZIndex));

		foreach (var child in sortedChildren)
		{
			if (child.IsVisible && !child.IsDisposed && child.KeyUp(e))
			{
				return true;
			}
		}

		return false;
	}

	public virtual bool MouseDown(MouseButtonEventArgs e)
	{
		ThrowIfDisposed();

		var sortedChildren = new List<UIElement>(_children);
		sortedChildren.Sort((a, b) => b.ZIndex.CompareTo(a.ZIndex));

		foreach (var child in sortedChildren)
		{
			if (child.IsVisible && !child.IsDisposed && child.MouseDown(e))
			{
				return true;
			}
		}

		return false;
	}

	public virtual bool MouseUp(MouseButtonEventArgs e)
	{
		ThrowIfDisposed();

		var sortedChildren = new List<UIElement>(_children);
		sortedChildren.Sort((a, b) => b.ZIndex.CompareTo(a.ZIndex));

		foreach (var child in sortedChildren)
		{
			if (child.IsVisible && !child.IsDisposed && child.MouseUp(e))
			{
				return true;
			}
		}

		return false;
	}

	public virtual bool MouseMove(MouseMoveEventArgs e)
	{
		ThrowIfDisposed();

		var sortedChildren = new List<UIElement>(_children);
		sortedChildren.Sort((a, b) => b.ZIndex.CompareTo(a.ZIndex));

		foreach (var child in sortedChildren)
		{
			if (child.IsVisible && !child.IsDisposed && child.MouseMove(e))
			{
				return true;
			}
		}

		return false;
	}

	public virtual bool MouseWheel(MouseWheelEventArgs e)
	{
		ThrowIfDisposed();

		var sortedChildren = new List<UIElement>(_children);
		sortedChildren.Sort((a, b) => b.ZIndex.CompareTo(a.ZIndex));

		foreach (var child in sortedChildren)
		{
			if (child.IsVisible && !child.IsDisposed && child.MouseWheel(e))
			{
				return true;
			}
		}

		return false;
	}

	public virtual bool TextInput(TextInputEventArgs e)
	{
		ThrowIfDisposed();

		var sortedChildren = new List<UIElement>(_children);
		sortedChildren.Sort((a, b) => b.ZIndex.CompareTo(a.ZIndex));

		foreach (var child in sortedChildren)
		{
			if (child.IsVisible && !child.IsDisposed && child.TextInput(e))
			{
				return true;
			}
		}

		return false;
	}

	#endregion

	#region Property Changed Handling

	protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
	{
		var args = new PropertyChangedEventArgs(propertyName);
		PropertyChanged?.Invoke(this, args);
		_propertyChangedSubject.OnNext(args);

		if (propertyName == nameof(Margin) || propertyName == nameof(Position) || propertyName == nameof(Parent))
		{
			OnPropertyChanged(nameof(AbsolutePosition));
		}

		if (propertyName == nameof(AbsolutePosition) || propertyName == nameof(Size))
		{
			OnPropertyChanged(nameof(AbsoluteBounds));
			OnPropertyChanged(nameof(ContentBounds));
		}
	}

	protected virtual void OnParentPropertyChanged(PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(AbsolutePosition) || e.PropertyName == nameof(Padding))
		{
			OnPropertyChanged(nameof(AbsolutePosition));
		}
	}

	#endregion

	#region Observable Utility Methods

	/// <summary>
	/// Gets an observable that emits when the size of this element changes.
	/// </summary>
	/// <returns>An observable sequence of size values.</returns>
	public IObservable<Vector2> WhenSizeChanges()
	{
		ThrowIfDisposed();
		return SizeChanged;
	}

	/// <summary>
	/// Gets an observable that emits when a specific property changes.
	/// </summary>
	/// <typeparam name="T">The type of the property value.</typeparam>
	/// <param name="propertyName">The name of the property to observe.</param>
	/// <param name="selector">A function to extract the property value.</param>
	/// <returns>An observable sequence of property values.</returns>
	public IObservable<T> WhenPropertyChanges<T>(string propertyName, Func<UIElement, T> selector)
	{
		ThrowIfDisposed();
		return PropertyChanges
			.Where(e => e.PropertyName == propertyName)
			.Select(_ => selector(this));
	}

	/// <summary>
	/// Gets an observable that emits when the content size of this element changes.
	/// </summary>
	/// <returns>An observable sequence of content size values.</returns>
	public IObservable<Vector2> WhenContentSizeChanges()
	{
		ThrowIfDisposed();
		return WhenPropertyChanges(nameof(ContentSize), e => e.ContentSize);
	}

	/// <summary>
	/// Gets an observable that emits when the position of this element changes.
	/// </summary>
	/// <returns>An observable sequence of position values.</returns>
	public IObservable<Vector2> WhenPositionChanges()
	{
		ThrowIfDisposed();
		return WhenPropertyChanges(nameof(Position), e => e.Position);
	}

	/// <summary>
	/// Gets an observable that emits when the absolute position of this element changes.
	/// </summary>
	/// <returns>An observable sequence of absolute position values.</returns>
	public IObservable<Vector2> WhenAbsolutePositionChanges()
	{
		ThrowIfDisposed();
		return WhenPropertyChanges(nameof(AbsolutePosition), e => e.AbsolutePosition);
	}

	/// <summary>
	/// Gets an observable that emits when the visibility of this element changes.
	/// </summary>
	/// <returns>An observable sequence of visibility values.</returns>
	public IObservable<bool> WhenVisibilityChanges()
	{
		ThrowIfDisposed();
		return WhenPropertyChanges(nameof(IsVisible), e => e.IsVisible);
	}

	#endregion

	#region Utility Methods

	/// <summary>
	/// Determines if a point is inside the bounds of this element.
	/// </summary>
	/// <param name="point">The point to test.</param>
	/// <returns>True if the point is inside the element bounds, false otherwise.</returns>
	public bool IsPointInside(Vector2 point)
	{
		ThrowIfDisposed();
		return IsVisible && AbsoluteBounds.ContainsInclusive(point);
	}

	/// <summary>
	/// Determines if a point is inside the content area of this element.
	/// </summary>
	/// <param name="point">The point to test.</param>
	/// <returns>True if the point is inside the content area, false otherwise.</returns>
	public bool IsPointInsideContent(Vector2 point)
	{
		ThrowIfDisposed();
		return IsVisible && ContentBounds.ContainsInclusive(point);
	}

	/// <summary>
	/// Finds the child element at the specified point.
	/// </summary>
	/// <param name="point">The point to test.</param>
	/// <returns>The topmost child at the point, or null if no child contains the point.</returns>
	public UIElement? FindChildAt(Vector2 point)
	{
		ThrowIfDisposed();

		if (!IsVisible || !IsPointInside(point))
			return null;

		var sortedChildren = new List<UIElement>(_children);
		sortedChildren.Sort((a, b) => b.ZIndex.CompareTo(a.ZIndex));

		foreach (var child in sortedChildren)
		{
			if (child.IsVisible && !child.IsDisposed && child.IsPointInside(point))
			{
				return child;
			}
		}

		return null;
	}

	/// <summary>
	/// Finds all child elements at the specified point, ordered by z-index.
	/// </summary>
	/// <param name="point">The point to test.</param>
	/// <returns>A list of all children at the point, ordered by z-index (front to back).</returns>
	public IEnumerable<UIElement> FindChildrenAt(Vector2 point)
	{
		ThrowIfDisposed();

		if (!IsVisible || !IsPointInside(point))
			yield break;

		var sortedChildren = new List<UIElement>(_children);
		sortedChildren.Sort((a, b) => b.ZIndex.CompareTo(a.ZIndex));

		foreach (var child in sortedChildren)
		{
			if (child.IsVisible && !child.IsDisposed && child.IsPointInside(point))
			{
				yield return child;
			}
		}
	}

	/// <summary>
	/// Sets the position of this element to center it within its parent.
	/// </summary>
	public void CenterInParent()
	{
		ThrowIfDisposed();

		if (Parent == null)
			return;

		Position = new Vector2(
			(Parent.ContentSize.X - Size.X) / 2,
			(Parent.ContentSize.Y - Size.Y) / 2
		);
	}

	/// <summary>
	/// Sets the position of this element to align it horizontally within its parent.
	/// </summary>
	/// <param name="alignment">The horizontal alignment (0 = left, 0.5 = center, 1 = right).</param>
	public void AlignHorizontally(float alignment = 0.5f)
	{
		ThrowIfDisposed();

		if (Parent == null)
			return;

		float x = (Parent.ContentSize.X - Size.X) * Math.Clamp(alignment, 0, 1);
		Position = new Vector2(x, Position.Y);
	}

	/// <summary>
	/// Sets the position of this element to align it vertically within its parent.
	/// </summary>
	/// <param name="alignment">The vertical alignment (0 = top, 0.5 = middle, 1 = bottom).</param>
	public void AlignVertically(float alignment = 0.5f)
	{
		ThrowIfDisposed();

		if (Parent == null)
			return;

		float y = (Parent.ContentSize.Y - Size.Y) * Math.Clamp(alignment, 0, 1);
		Position = new Vector2(Position.X, y);
	}

	#endregion

	#region Disposable Implementation

	protected override void DisposeManagedResources()
	{
		// Unload resources if loaded
		if (IsLoaded)
		{
			Unload();
		}

		// Remove from parent if has one
		if (Parent != null && !Parent.IsDisposed)
		{
			Parent.RemoveChild(this);
		}

		// Clear and dispose all children
		foreach (var child in _children.ToArray())
		{
			child.Dispose();
		}
		_children.Clear();

		// Dispose subjects
		_childAddedSubject.Dispose();
		_childRemovedSubject.Dispose();
		_propertyChangedSubject.Dispose();
		_sizeChangedSubject.Dispose();

		// Dispose parent property changes subscription
		_parentPropertyChangesSubscription.Dispose();

		base.DisposeManagedResources();
	}

	#endregion
}