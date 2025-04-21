using System.Reactive.Linq;
using System.Reactive.Subjects;
using Critters.Core;
using Critters.Events;
using Critters.Gfx;
using Critters.Services;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;

namespace Critters.UI;

class GlyphPicker : UIElement
{
	#region Constants

	private const int GLYPH_SIZE = 8;
	private const int GLYPH_SPACING = 1;
	private const int GLYPH_CELL_SIZE = GLYPH_SIZE + GLYPH_SPACING;
	private const int NUM_GLYPHS = 256;
	private const string DEFAULT_FONT_PATH = "oem437_8.png";

	#endregion

	#region Subjects

	private readonly Subject<byte> _glyphSelectedSubject = new();

	#endregion

	#region Events (Legacy)

	public event EventHandler<GlyphSelectedEventArgs>? GlyphSelected;

	#endregion

	#region Observables

	public IObservable<byte> GlyphSelections => _glyphSelectedSubject.AsObservable();

	#endregion

	#region Fields

	private readonly List<SelectableGlyph> _selectableGlyphs = new();
	private SelectableGlyph? _selectedGlyph;
	private Label? _glyphLabel;
	private readonly string _fontPath;
	private readonly int _glyphsPerRow;
	private readonly int _numGlyphRows;
	private RadialColor _foregroundColor = new(5, 5, 0);
	private RadialColor _backgroundColor = new(0, 0, 5);

	#endregion

	#region Constructors

	public GlyphPicker(IResourceManager resources, IRenderingContext rc, Vector2 position,
					  string? fontPath = null, byte? initialGlyph = null)
		: base(resources, rc)
	{
		Position = position;
		_fontPath = fontPath ?? DEFAULT_FONT_PATH;

		// Calculate layout
		_glyphsPerRow = (int)Math.Sqrt(NUM_GLYPHS);
		_numGlyphRows = (int)Math.Ceiling((double)NUM_GLYPHS / _glyphsPerRow);

		// Set content size based on grid dimensions plus space for label
		ContentSize = new Vector2(
			_glyphsPerRow * GLYPH_CELL_SIZE,
			_numGlyphRows * GLYPH_CELL_SIZE + 20); // Extra space for label

		Padding = new Thickness(1);

		// Setup disposal
		DisposalEvents.Subscribe(e =>
		{
			if (e.Stage == DisposalStage.Started)
			{
				_glyphSelectedSubject.OnCompleted();
			}
		});

		InitializeUI(initialGlyph ?? 0);
	}

	#endregion

	#region Properties

	public byte SelectedGlyphIndex
	{
		get => _selectedGlyph?.GlyphIndex ?? 0;
		set
		{
			ThrowIfDisposed();

			var glyph = _selectableGlyphs.FirstOrDefault(x => x.GlyphIndex == value);
			if (glyph != null && (_selectedGlyph == null || _selectedGlyph.GlyphIndex != value))
			{
				SelectGlyph(glyph);
				_glyphSelectedSubject.OnNext(value);
				GlyphSelected?.Invoke(this, new GlyphSelectedEventArgs(value));
				OnPropertyChanged();
			}
		}
	}

	public RadialColor ForegroundColor
	{
		get => _foregroundColor;
		set
		{
			ThrowIfDisposed();

			if (_foregroundColor != value)
			{
				_foregroundColor = value;

				foreach (var glyph in _selectableGlyphs)
				{
					glyph.ForegroundColor = value;
				}

				OnPropertyChanged();
			}
		}
	}

	public RadialColor BackgroundColor
	{
		get => _backgroundColor;
		set
		{
			ThrowIfDisposed();

			if (_backgroundColor != value)
			{
				_backgroundColor = value;

				foreach (var glyph in _selectableGlyphs)
				{
					glyph.BackgroundColor = value;
				}

				OnPropertyChanged();
			}
		}
	}

	#endregion

	#region Initialization

	private void InitializeUI(byte initialGlyphIndex)
	{
		// Create all the selectable glyphs
		var glyphIndex = 0;

		for (int y = 0; y < _numGlyphRows; y++)
		{
			for (int x = 0; x < _glyphsPerRow && glyphIndex < NUM_GLYPHS; x++)
			{
				var position = new Vector2(x * GLYPH_CELL_SIZE, y * GLYPH_CELL_SIZE);

				var glyph = new SelectableGlyph(Resources, RC, position, _fontPath, (byte)glyphIndex);
				glyph.ForegroundColor = _foregroundColor;
				glyph.BackgroundColor = _backgroundColor;

				// Subscribe to events using Rx
				glyph.ClickEvents.Subscribe(_ => OnGlyphClicked(glyph));
				glyph.ScrollEvents.Subscribe(e => OnGlyphScrolled(glyph, e));

				// Set selected state if this is the initial glyph
				if (glyphIndex == initialGlyphIndex)
				{
					glyph.IsSelected = true;
					_selectedGlyph = glyph;
				}

				_selectableGlyphs.Add(glyph);
				AddChild(glyph);

				glyphIndex++;
			}
		}

		// If no glyph was selected, select the first one
		if (_selectedGlyph == null && _selectableGlyphs.Count > 0)
		{
			_selectedGlyph = _selectableGlyphs[0];
			_selectedGlyph.IsSelected = true;
		}

		// Create the label showing the current glyph index
		_glyphLabel = new Label(
			Resources,
			RC,
			$"glyph index={_selectedGlyph?.GlyphIndex ?? 0}",
			new Vector2(0, _numGlyphRows * GLYPH_CELL_SIZE + 2),
			new RadialColor(5, 5, 5),
			new RadialColor(0, 0, 0)
		);

		AddChild(_glyphLabel);

		// Update the label
		UpdateGlyphLabel();
	}

	#endregion

	#region Methods

	public override void Render(GameTime gameTime)
	{
		ThrowIfDisposed();

		if (!IsVisible)
			return;

		// Render background panel
		RC.RenderFilledRect(
			new Box2(
				AbsolutePosition,
				AbsolutePosition + new Vector2(_glyphsPerRow * GLYPH_CELL_SIZE, _numGlyphRows * GLYPH_CELL_SIZE)
			),
			new RadialColor(5, 5, 5).Index
		);

		// Render children (which includes all the glyphs and the label)
		base.Render(gameTime);
	}

	private void SelectGlyph(SelectableGlyph glyph)
	{
		ThrowIfDisposed();

		if (_selectedGlyph != null)
		{
			_selectedGlyph.IsSelected = false;
		}

		_selectedGlyph = glyph;
		_selectedGlyph.IsSelected = true;

		UpdateGlyphLabel();
	}

	private void UpdateGlyphLabel()
	{
		if (_glyphLabel != null && _selectedGlyph != null)
		{
			_glyphLabel.Text = StringProvider.From($"glyph index={_selectedGlyph.GlyphIndex}");
		}
	}

	private void OnGlyphClicked(SelectableGlyph glyph)
	{
		SelectGlyph(glyph);
		_glyphSelectedSubject.OnNext(glyph.GlyphIndex);
		GlyphSelected?.Invoke(this, new GlyphSelectedEventArgs(glyph.GlyphIndex));
		OnPropertyChanged(nameof(SelectedGlyphIndex));
	}

	private void OnGlyphScrolled(SelectableGlyph glyph, MouseWheelEventArgs e)
	{
		var delta = -Math.Sign(e.OffsetY);

		for (var n = 0; n < _selectableGlyphs.Count; n++)
		{
			if (_selectableGlyphs[n].IsSelected)
			{
				var newIndex = (n + delta + _selectableGlyphs.Count) % _selectableGlyphs.Count;
				var newGlyph = _selectableGlyphs[newIndex];

				SelectGlyph(newGlyph);
				_glyphSelectedSubject.OnNext(newGlyph.GlyphIndex);
				GlyphSelected?.Invoke(this, new GlyphSelectedEventArgs(newGlyph.GlyphIndex));
				OnPropertyChanged(nameof(SelectedGlyphIndex));
				break;
			}
		}
	}

	#endregion

	#region Disposable Implementation

	protected override void DisposeManagedResources()
	{
		// Dispose the subject
		_glyphSelectedSubject.Dispose();

		// Clear event handlers
		GlyphSelected = null;

		// Clear references
		_selectedGlyph = null;
		_glyphLabel = null;

		// The base class will handle disposing all children
		base.DisposeManagedResources();
	}

	#endregion
}
