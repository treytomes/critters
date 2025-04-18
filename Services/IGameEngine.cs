// Services/IGameEngine.cs

namespace Critters.Services;

public interface IGameEngine
{
	Task RunAsync(CancellationToken cancellationToken = default);
}