using Critters.Gfx;
using SixLaborsImage = SixLabors.ImageSharp.Image;
using SixLabors.ImageSharp.PixelFormats;

namespace Critters.IO
{
	/// <summary>
	/// The image is loaded with 24-bit RGB colors, then compressed into the radial palette.
	/// </summary>
	class ImageLoader : IResourceLoader
	{
		private const int SRC_BPP = 4;
		private const int DST_BPP = 1;

		public object Load(string path)
		{
			if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Image file not found: {path}");
        }

        using var image = SixLaborsImage.Load<Rgba32>(path);

        var pixels0 = new byte[image.Width * image.Height * SRC_BPP];
        image.CopyPixelDataTo(pixels0);

				var pixels1 = new byte[image.Width * image.Height * DST_BPP];
				for (int n0 = 0, n1 = 0; n0 < pixels0.Length; n0 += SRC_BPP, n1++)
				{
					var r = pixels0[n0];
					var g = pixels0[n0 + 1];
					var b = pixels0[n0 + 2];

					var r6 = (byte)(r / (255.0 / 5));
					var g6 = (byte)(g / (255.0 / 5));
					var b6 = (byte)(b / (255.0 / 5));

					var index = (byte)(r6 * 6 * 6 + g6 * 6 + b6);

					pixels1[n1] = index;
				}

				return new Image(image.Width, image.Height, pixels1);
		}
	}
}