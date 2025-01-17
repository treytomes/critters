using Critters.Gfx;
using SixLaborsImage = SixLabors.ImageSharp.Image;
using SixLabors.ImageSharp.PixelFormats;

namespace Critters.IO
{
	class ImageLoader : IResourceLoader
	{
		public object Load(string path)
		{
			if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Image file not found: {path}");
        }

        using var image = SixLaborsImage.Load<Rgba32>(path);

        var pixels = new byte[image.Width * image.Height * 4];
        image.CopyPixelDataTo(pixels);

				return new Image(image.Width, image.Height, pixels);
		}
	}
}