namespace Critters.IO;

interface IResourceLoader
{
	object Load(ResourceManager resources, string resourcePath);
}
