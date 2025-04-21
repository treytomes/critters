using System.Reactive.Linq;
using System.Reactive.Subjects;
using Critters.Core;
using Critters.Events;
using Critters.Gfx;
using Critters.Services;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;

namespace Critters.UI;

class ColorPicker : UIElement
{
	#region Constants

	private const int BUTTON_SIZE = 8;
	private const int BUTTON_PADDING = 1;
	private const int GRID_SIZE = 6;

	#endregion

	#region Subjects

	private readonly Subject<RadialColor> _colorSelectedSubject = new();

	#endregion

	#region Events (Legacy)

	public event EventHandler<ColorSelectedEventArgs>? ColorSelected;

	#endregion

	#region Observables

	public IObservable<RadialColor> ColorSelections => _colorSelectedSubject.AsObservable();

	#endregion

	#region Fields

	private readonly List<SelectableColor> _baseColors = new();
	private readonly List<SelectableColor> _derivedColors = new();
	private SelectableColor? _selectedBaseColor;
	private SelectableColor? _selectedDerivedColor;
	private Rectangle? _selectedColorPreview;
	private Label? _colorLabel;
	private RadialColor _selectedColor;

	#endregion

	#region Constructors

	public ColorPicker(IResourceManager resources, IRenderingContext rc, Vector2 position, RadialColor? initialColor = null)
		: base(resources, rc)
	{
		Position = position;
		_selectedColor = initialColor ?? RadialColor.Black;

		// Calculate the total size of the control
		var totalWidth = (BUTTON_SIZE + BUTTON_PADDING) * GRID_SIZE + 24; // Add space for preview
		var totalHeight = (BUTTON_SIZE + BUTTON_PADDING) * (GRID_SIZE + 1) + 20; // Add space for label

		// Set content size and padding
		ContentSize = new Vector2(totalWidth, totalHeight);
		Padding = new Thickness(1);

		// Setup disposal
		DisposalEvents.Subscribe(e =>
		{
			if (e.Stage == DisposalStage.Started)
			{
				_colorSelectedSubject.OnCompleted();
			}
		});

		InitializeUI(initialColor);
	}

	#endregion

	#region Properties

	public RadialColor SelectedColor
	{
		get => _selectedColor;
		set
		{
			ThrowIfDisposed();

			if (_selectedColor != value)
			{
				_selectedColor = value;

				// Find and select the appropriate base color
				var baseColor = _baseColors.FirstOrDefault(x => x.DerivedColor.B == value.B);
				if (baseColor != null)
				{
					SelectBaseColor(baseColor);
				}

				// Find and select the appropriate derived color
				var derivedColor = _derivedColors.FirstOrDefault(
					x => x.DerivedColor.R == value.R && x.DerivedColor.G == value.G);
				if (derivedColor != null)
				{
					SelectDerivedColor(derivedColor);
				}

				OnPropertyChanged();
				_colorSelectedSubject.OnNext(_selectedColor);
				ColorSelected?.Invoke(this, new ColorSelectedEventArgs(_selectedColor));
			}
		}
	}

	#endregion

	#region Initialization

	private void InitializeUI(RadialColor? initialColor)
	{
		var selectedBaseColor = initialColor.HasValue ? new RadialColor(0, 0, initialColor.Value.B) : RadialColor.Black;

		// Create base colors (blue component)
		for (byte xc = 0; xc < GRID_SIZE; xc++)
		{
			var x = xc * (BUTTON_SIZE + BUTTON_PADDING);
			var y = 0;
			var color = new RadialColor(0, 0, xc);
			var elem = new SelectableColor(Resources, RC, new Vector2(x, y), color);

			// Subscribe to events
			elem.ClickEvents.Subscribe(_ => OnBaseColorClicked(elem));
			elem.ScrollEvents.Subscribe(e => OnBaseColorScrolled(elem, e));

			if (initialColor?.B == xc)
			{
				elem.IsSelected = true;
				_selectedBaseColor = elem;
			}

			_baseColors.Add(elem);
			AddChild(elem);
		}

		// Create derived colors grid (red and green components)
		for (byte xc = 0; xc < GRID_SIZE; xc++)
		{
			for (byte yc = 0; yc < GRID_SIZE; yc++)
			{
				// Inverting to make mouse scrolling more intuitive
				var y = (BUTTON_SIZE + BUTTON_PADDING + 2) + xc * (BUTTON_SIZE + BUTTON_PADDING);
				var x = yc * (BUTTON_SIZE + BUTTON_PADDING);

				var color = new RadialColor(xc, yc, 0);
				var elem = new SelectableColor(Resources, RC, new Vector2(x, y), color);
				elem.BaseColor = selectedBaseColor;

				// Subscribe to events
				elem.ClickEvents.Subscribe(_ => OnDerivedColorClicked(elem));
				elem.ScrollEvents.Subscribe(e => OnDerivedColorScrolled(elem, e));

				if (initialColor?.R == xc && initialColor?.G == yc)
				{
					elem.IsSelected = true;
					_selectedDerivedColor = elem;
				}

				_derivedColors.Add(elem);
				AddChild(elem);
			}
		}

		// Set default selections if none were set
		if (_selectedBaseColor == null && _baseColors.Count > 0)
		{
			_selectedBaseColor = _baseColors[0];
			_selectedBaseColor.IsSelected = true;
		}

		if (_selectedDerivedColor == null && _derivedColors.Count > 0)
		{
			_selectedDerivedColor = _derivedColors[0];
			_selectedDerivedColor.IsSelected = true;
		}

		// Create color preview rectangle
		var previewX = (BUTTON_SIZE + BUTTON_PADDING) * GRID_SIZE + 2;
		var previewWidth = 22;
		var previewHeight = (BUTTON_SIZE + BUTTON_PADDING) * (GRID_SIZE + 1);

		_selectedColorPreview = new Rectangle(
			Resources,
			RC,
			new Box2(previewX, 0, previewX + previewWidth, previewHeight),
			new RadialColor(5, 5, 5),
			_selectedDerivedColor?.DerivedColor ?? RadialColor.Black
		);
		AddChild(_selectedColorPreview);

		// Create color label
		_colorLabel = new Label(
			Resources,
			RC,
			"000==0",
			new Vector2(0, (BUTTON_SIZE + BUTTON_PADDING) * (GRID_SIZE + 1) + 4),
			new RadialColor(5, 5, 5),
			RadialColor.Black
		);
		AddChild(_colorLabel);

		// Update the color preview and label
		UpdateColorDisplay();
	}

	#endregion

	#region Methods

	public override void Render(GameTime gameTime)
	{
		ThrowIfDisposed();

		if (!IsVisible)
			return;

		// Render background panels
		var baseColorsRect = new Box2(
			AbsolutePosition,
			AbsolutePosition + new Vector2((BUTTON_SIZE + BUTTON_PADDING) * GRID_SIZE, BUTTON_SIZE + BUTTON_PADDING)
		);

		var derivedColorsRect = new Box2(
			AbsolutePosition + new Vector2(0, BUTTON_SIZE + BUTTON_PADDING + 2),
			AbsolutePosition + new Vector2(
				(BUTTON_SIZE + BUTTON_PADDING) * GRID_SIZE,
				2 + (GRID_SIZE + 1) * (BUTTON_SIZE + BUTTON_PADDING)
			)
		);

		RC.RenderFilledRect(baseColorsRect, new RadialColor(5, 5, 5));
		RC.RenderFilledRect(derivedColorsRect, new RadialColor(5, 5, 5));

		// Render children (which includes all the color buttons)
		base.Render(gameTime);
	}

	private void SelectBaseColor(SelectableColor color)
	{
		ThrowIfDisposed();

		if (_selectedBaseColor != null)
		{
			_selectedBaseColor.IsSelected = false;
		}

		_selectedBaseColor = color;
		_selectedBaseColor.IsSelected = true;

		// Update all derived colors with the new base color
		foreach (var derivedColor in _derivedColors)
		{
			derivedColor.BaseColor = _selectedBaseColor.DerivedColor;
		}

		// Update the selected color
		_selectedColor = _selectedDerivedColor?.DerivedColor ?? RadialColor.Black;

		// Update display
		UpdateColorDisplay();
	}

	private void SelectDerivedColor(SelectableColor color)
	{
		ThrowIfDisposed();

		if (_selectedDerivedColor != null)
		{
			_selectedDerivedColor.IsSelected = false;
		}

		_selectedDerivedColor = color;
		_selectedDerivedColor.IsSelected = true;

		// Update the selected color
		_selectedColor = _selectedDerivedColor.DerivedColor;

		// Update display
		UpdateColorDisplay();
	}

	private void UpdateColorDisplay()
	{
		if (_selectedColorPreview != null && _selectedDerivedColor != null)
		{
			_selectedColorPreview.FillColor = _selectedDerivedColor.DerivedColor;
		}

		if (_colorLabel != null && _selectedDerivedColor != null)
		{
			var color = _selectedDerivedColor.DerivedColor;
			_colorLabel.Text = StringProvider.From($"{color.R}{color.G}{color.B}=={color.Index}");
		}
	}

	private void OnBaseColorClicked(SelectableColor color)
	{
		SelectBaseColor(color);
		_colorSelectedSubject.OnNext(_selectedColor);
		ColorSelected?.Invoke(this, new ColorSelectedEventArgs(_selectedColor));
		OnPropertyChanged(nameof(SelectedColor));
	}

	private void OnBaseColorScrolled(SelectableColor color, MouseWheelEventArgs e)
	{
		var delta = Math.Sign(e.OffsetY);

		for (var n = 0; n < _baseColors.Count; n++)
		{
			if (_baseColors[n].IsSelected)
			{
				var newIndex = (n + delta + _baseColors.Count) % _baseColors.Count;
				SelectBaseColor(_baseColors[newIndex]);
				_colorSelectedSubject.OnNext(_selectedColor);
				ColorSelected?.Invoke(this, new ColorSelectedEventArgs(_selectedColor));
				OnPropertyChanged(nameof(SelectedColor));
				break;
			}
		}
	}

	private void OnDerivedColorClicked(SelectableColor color)
	{
		SelectDerivedColor(color);
		_colorSelectedSubject.OnNext(_selectedColor);
		ColorSelected?.Invoke(this, new ColorSelectedEventArgs(_selectedColor));
		OnPropertyChanged(nameof(SelectedColor));
	}

	private void OnDerivedColorScrolled(SelectableColor color, MouseWheelEventArgs e)
	{
		var delta = -Math.Sign(e.OffsetY);

		for (var n = 0; n < _derivedColors.Count; n++)
		{
			if (_derivedColors[n].IsSelected)
			{
				var newIndex = (n + delta + _derivedColors.Count) % _derivedColors.Count;
				SelectDerivedColor(_derivedColors[newIndex]);
				_colorSelectedSubject.OnNext(_selectedColor);
				ColorSelected?.Invoke(this, new ColorSelectedEventArgs(_selectedColor));
				OnPropertyChanged(nameof(SelectedColor));
				break;
			}
		}
	}

	#endregion

	#region Disposable Implementation

	protected override void DisposeManagedResources()
	{
		// Dispose the subject
		_colorSelectedSubject.Dispose();

		// Clear event handlers
		ColorSelected = null;

		// Clear references
		_selectedBaseColor = null;
		_selectedDerivedColor = null;
		_selectedColorPreview = null;
		_colorLabel = null;

		// The base class will handle disposing all children
		base.DisposeManagedResources();
	}

	#endregion
}
