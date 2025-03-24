#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

using System.Runtime.CompilerServices;
using Critters.Events;
using Critters.Gfx;
using Critters.IO;
using OpenTK.Mathematics;

namespace Critters.UI;

interface IStringProvider
{
}

class ConstantStringProvider(string text) : IStringProvider
{
	public override string ToString()
	{
		return text;
	}
}

class FuncStringProvider(Func<string> func) : IStringProvider
{
	public override string ToString()
	{
		return func();
	}
}

static class StringProvider
{
	public static IStringProvider From(object text)
	{
		return (text is Func<string>) switch
		{
			true => new FuncStringProvider((Func<string>)text),
			_ => new ConstantStringProvider(text?.ToString() ?? "")
		};
	}
}

class Label : UIElement
{
	#region Fields

	private Font? _font;
	private IStringProvider _text;
	private RadialColor _foregroundColor;
	private RadialColor? _backgroundColor;

	#endregion

	#region Constructors

	public Label(object text, Vector2 position, RadialColor fgColor, RadialColor? bgColor = null)
		: this(null, text, position, fgColor, bgColor)
	{
	}

	public Label(UIElement? parent, object text, Vector2 position, RadialColor fgColor, RadialColor? bgColor = null)
		: base(parent)
	{
		Text = StringProvider.From(text);
		Position = position;
		ForegroundColor = fgColor;
		BackgroundColor = bgColor;
	}

	#endregion

	#region Properties

	public IStringProvider Text
	{
		get
		{
			return _text;
		}
		set
		{
			if (_text != value)
			{
				_text = value;
				OnPropertyChanged();
			}
		}
	}

	public RadialColor ForegroundColor
	{
		get
		{
			return _foregroundColor;
		}
		set
		{
			if (_foregroundColor != value)
			{
				_foregroundColor = value;
				OnPropertyChanged();
			}
		}
	}

	public RadialColor? BackgroundColor
	{
		get
		{
			return _backgroundColor;
		}
		set
		{
			if (_backgroundColor != value)
			{
				_backgroundColor = value;
				OnPropertyChanged();
			}
		}
	}

	#endregion

	#region Methods

	public override void Load(ResourceManager resources, EventBus eventBus)
	{
		base.Load(resources, eventBus);

    var image = resources.Load<Image>("oem437_8.png");
		var bmp = new Bitmap(image);
    var tiles = new GlyphSet<Bitmap>(bmp, 8, 8);
    _font = new Font(tiles);
		Size = _font?.MeasureString(Text.ToString()!) ?? Vector2.Zero;
	}

	public override void Unload(ResourceManager resources, EventBus eventBus)
	{
		base.Unload(resources, eventBus);
	}

	public override void Render(RenderingContext rc, GameTime gameTime)
	{
		base.Render(rc, gameTime);

		_font?.WriteString(rc, Text.ToString() ?? "", AbsolutePosition, ForegroundColor, BackgroundColor);
	}

	protected override void OnPropertyChanged([CallerMemberName] string propertyName = "")
	{
		base.OnPropertyChanged(propertyName);

		if (propertyName == nameof(Text))
		{
			Size = _font?.MeasureString(Text.ToString() ?? "") ?? Vector2.Zero;
		}
	}

	#endregion
}
