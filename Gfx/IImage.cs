namespace Critters.Gfx;

interface IImage<TImage>
	where TImage: IImage<TImage> 
{
	int Width { get; }
	int Height { get; }
	TImage Crop(int x, int y, int width, int height);
}

interface IImage<TImage, TPixel> : IImage<TImage>
	where TImage : IImage<TImage, TPixel>
{
	TPixel this[int x, int y] { get; set; }
	
	TPixel GetPixel(int x, int y);
	void SetPixel(int x, int y, TPixel color);
}