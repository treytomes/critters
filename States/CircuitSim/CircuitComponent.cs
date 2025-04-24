using Critters.Gfx;
using OpenTK.Mathematics;

namespace Critters.States.CircuitSim;

/// <summary>
/// Base class for all circuit components
/// </summary>
abstract class CircuitComponent
{
	#region Constants

	protected const float CHARGE_INCONSEQUENTIAL = 0.001f;

	#endregion

	#region Fields

	/// <summary>
	/// Connection flags for each direction (up, right, down, left).
	/// </summary>
	protected bool[] _connections = new bool[4];

	#endregion

	#region Properties

	/// <summary>
	/// Current electrical charge of this component
	/// </summary>
	public float Charge { get; protected set; }

	/// <summary>
	/// Maximum charge this component can hold
	/// </summary>
	public float MaxCharge { get; protected set; } = 5.0f;

	/// <summary>
	/// Whether this component has changed since the last update
	/// </summary>
	public bool IsDirty { get; set; } = true;

	#endregion

	#region Methods

	/// <summary>
	/// Updates the component state
	/// </summary>
	/// <param name="simulator">The circuit simulator</param>
	/// <param name="x">X position in the grid</param>
	/// <param name="y">Y position in the grid</param>
	/// <param name="deltaTime">Time since last update</param>
	public abstract void Update(CircuitSimulator simulator, int x, int y, float deltaTime);

	/// <summary>
	/// Renders the component
	/// </summary>
	/// <param name="rc">Rendering context</param>
	/// <param name="screenPos">Position on screen</param>
	/// <param name="tileSize">Size of a tile</param>
	public abstract void Render(IRenderingContext rc, Vector2 screenPos, int tileSize);

	/// <summary>
	/// Sets the charge of this component
	/// </summary>
	/// <param name="newCharge">New charge value</param>
	/// <returns>True if the charge was changed</returns>
	public virtual bool SetCharge(float newCharge)
	{
		var clampedCharge = Math.Clamp(newCharge, 0, MaxCharge);
		var diff = Math.Abs(Charge - clampedCharge);
		if (diff > CHARGE_INCONSEQUENTIAL)
		{
			Charge = clampedCharge;
			IsDirty = true;
			return true;
		}
		return false;
	}

	/// <summary>
	/// Transfers charge to another component
	/// </summary>
	/// <param name="other">Component to transfer to</param>
	/// <param name="amount">Amount to transfer</param>
	/// <returns>Amount actually transferred</returns>
	public virtual float TransferChargeTo(CircuitComponent other, float amount)
	{
		if (amount <= 0 || Charge <= 0)
			return 0;

		float actualAmount = Math.Min(amount, Charge);
		Charge -= actualAmount;
		other.SetCharge(other.Charge + actualAmount);
		IsDirty = true;

		return actualAmount;
	}

	/// <summary>
	/// Updates connection information with adjacent components
	/// </summary>
	protected void UpdateConnections(CircuitSimulator simulator, Vector2i pos)
	{
		// Check adjacent cells (up, right, down, left).
		Vector2i[] deltas = [
			-Vector2i.UnitY,
			 Vector2i.UnitX,
			 Vector2i.UnitY,
			-Vector2i.UnitX,
		];

		for (var i = 0; i < 4; i++)
		{
			var offset = deltas[i] + pos;
			var neighbor = simulator.GetComponentAt(offset);
			_connections[i] = neighbor != null;
		}
	}

	#endregion
}
