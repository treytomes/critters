namespace Critters.IO;

class ResourceManager
{
	#region Fields

	private string _rootPath;
	private Dictionary<Type, IResourceLoader> _resourceLoaders = new Dictionary<Type, IResourceLoader>();
	private Dictionary<string, object> _resources = new Dictionary<string, object>();

	#endregion

	#region Constructors

	public ResourceManager(string rootPath)
	{
		_rootPath = rootPath;
	}

	#endregion

	#region Methods

	public void Register<TResource, TResourceLoader>()
		where TResourceLoader : IResourceLoader, new()
	{
		_resourceLoaders.Add(typeof(TResource), Activator.CreateInstance<TResourceLoader>());
	}

	public T Load<T>(string relativePath)
	{
		var key = $"{typeof(T).Name}.{relativePath}";

		if (_resources.ContainsKey(key))
		{
			return (T)_resources[key];
		}
		else
		{
			var path = GetResourcePath(relativePath);
			var loader = GetResourceLoader<T>();
			var resource = loader.Load(path);
			_resources.Add(key, resource);
			return (T)resource;
		}
	}

	private IResourceLoader GetResourceLoader<T>()
	{
		return GetResourceLoader(typeof(T));
	}

	private IResourceLoader GetResourceLoader(Type type)
	{
		if (!_resourceLoaders.ContainsKey(type))
		{
			throw new ApplicationException($"No resource loader available for type: {type.Name}");
		}
		return _resourceLoaders[type];
	}

	private string GetResourcePath(string relativePath)
	{
		return Path.Combine(_rootPath, relativePath);
	}

	#endregion
}