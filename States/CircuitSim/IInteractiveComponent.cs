using OpenTK.Mathematics;

namespace Critters.States.CircuitSim;

/// <summary>
/// Interface for components that can be interacted with
/// </summary>
interface IInteractiveComponent
{
	bool HandleClick(Vector2 worldPosition);
	bool HandlePress(Vector2 worldPosition);
	bool HandleRelease(Vector2 worldPosition);
}
