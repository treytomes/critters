using Critters.Gfx;
using SixLaborsImage = SixLabors.ImageSharp.Image;
using SixLabors.ImageSharp.PixelFormats;

namespace Critters.IO
{
	class ImageLoader : IResourceLoader
	{
		private const int SRC_BPP = 4;
		private const int DST_BPP = 3;

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
				for (var n = 0; n < pixels0.Length; n += SRC_BPP)
				{
					pixels1[n / SRC_BPP * DST_BPP] = pixels0[n];
					pixels1[n / SRC_BPP * DST_BPP + 1] = pixels0[n + 1];
					pixels1[n / SRC_BPP * DST_BPP + 2] = pixels0[n + 2];
				}

				return new Image(image.Width, image.Height, pixels1);
		}
	}
}