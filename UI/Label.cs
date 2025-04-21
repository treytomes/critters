using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using Critters.Core;
using Critters.Gfx;
using Critters.Services;
using OpenTK.Mathematics;

namespace Critters.UI;

interface IStringProvider
{
	string GetText();
	IObservable<string> TextChanged { get; }
}

class ConstantStringProvider : IStringProvider
{
	private readonly string _text;
	private readonly Subject<string> _textChanged = new();

	public ConstantStringProvider(string text)
	{
		_text = text ?? string.Empty;
	}

	public string GetText() => _text;
	public IObservable<string> TextChanged => _textChanged.AsObservable();

	public override string ToString() => _text;
}

class FuncStringProvider : Disposable, IStringProvider
{
	private readonly Func<string> _func;
	private readonly Subject<string> _textChanged = new();
	private string _lastValue;

	public FuncStringProvider(Func<string> func)
	{
		_func = func ?? throw new ArgumentNullException(nameof(func));
		_lastValue = func();
	}

	public string GetText()
	{
		string newValue = _func();
		if (newValue != _lastValue)
		{
			_lastValue = newValue;
			_textChanged.OnNext(newValue);
		}
		return newValue;
	}

	public IObservable<string> TextChanged => _textChanged.AsObservable();

	public override string ToString() => GetText();

	protected override void DisposeManagedResources()
	{
		_textChanged.OnCompleted();
		_textChanged.Dispose();
		base.DisposeManagedResources();
	}
}

static class StringProvider
{
	public static IStringProvider From(object? text)
	{
		if (text is Func<string> func)
			return new FuncStringProvider(func);
		return new ConstantStringProvider(text?.ToString() ?? string.Empty);
	}
}

class Label : UIElement
{
	#region Fields

	private Font? _font;
	private IStringProvider _text;
	private RadialColor _foregroundColor = RadialColor.White;
	private RadialColor? _backgroundColor;
	private IDisposable? _textChangedSubscription;
	private bool _textMeasurementNeeded = true;
	private Vector2 _measuredSize = Vector2.Zero;

	#endregion

	#region Constructors

	public Label(IResourceManager resources, IRenderingContext rc, object text)
		: base(resources, rc)
	{
		_text = StringProvider.From(text);
		SubscribeToTextChanges();
	}

	public Label(IResourceManager resources, IRenderingContext rc, object text, Vector2 position,
				 RadialColor fgColor, RadialColor? bgColor = null)
		: base(resources, rc)
	{
		_text = StringProvider.From(text);
		Position = position;
		_foregroundColor = fgColor;
		_backgroundColor = bgColor;
		SubscribeToTextChanges();
	}

	#endregion

	#region Properties

	public IStringProvider Text
	{
		get => _text;
		set
		{
			ThrowIfDisposed();

			if (_text != value)
			{
				// Unsubscribe from old text provider
				_textChangedSubscription?.Dispose();

				_text = value ?? StringProvider.From(string.Empty);
				_textMeasurementNeeded = true;

				// Subscribe to new text provider
				SubscribeToTextChanges();

				OnPropertyChanged();
				UpdateSize();
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
				OnPropertyChanged();
			}
		}
	}

	public RadialColor? BackgroundColor
	{
		get => _backgroundColor;
		set
		{
			ThrowIfDisposed();

			if (_backgroundColor != value)
			{
				_backgroundColor = value;
				OnPropertyChanged();
			}
		}
	}

	#endregion

	#region Methods

	private void SubscribeToTextChanges()
	{
		_textChangedSubscription = _text.TextChanged.Subscribe(_ =>
		{
			_textMeasurementNeeded = true;
			UpdateSize();
		});
	}

	private void UpdateSize()
	{
		if (_textMeasurementNeeded && _font != null)
		{
			_measuredSize = _font.MeasureString(_text.GetText());
			_textMeasurementNeeded = false;
			ContentSize = _measuredSize;
		}
	}

	public override void Load()
	{
		if (IsLoaded)
			return;

		base.Load();

		var image = Resources.Load<Image>("oem437_8.png");
		var bmp = new Bitmap(image);
		var tiles = new GlyphSet<Bitmap>(bmp, 8, 8);
		_font = new Font(tiles);

		_textMeasurementNeeded = true;
		UpdateSize();
	}

	public override void Unload()
	{
		if (!IsLoaded)
			return;

		_font = null;
		base.Unload();
	}

	public override void Render(GameTime gameTime)
	{
		ThrowIfDisposed();

		if (!IsVisible)
			return;

		base.Render(gameTime);

		if (_font != null)
		{
			string text = _text.GetText();
			_font.WriteString(RC, text, AbsolutePosition, ForegroundColor, BackgroundColor);
		}
	}

	protected override void DisposeManagedResources()
	{
		_textChangedSubscription?.Dispose();

		// If the text provider is disposable, dispose it
		if (_text is IDisposable disposableText)
		{
			disposableText.Dispose();
		}

		base.DisposeManagedResources();
	}

	#endregion
}