using Critters.Gfx;
using OpenTK.Mathematics;

namespace Critters.States.CircuitSim;

/// <summary>
/// Wire component that conducts electricity
/// </summary>
class Wire : CircuitComponent
{
	#region Constants

	private const float DEFAULT_MAX_CHARGE = 1.0f;
	private const float DEFAULT_RESISTANCE = 0.01f;

	#endregion

	#region Constructors

	public Wire()
	{
		MaxCharge = DEFAULT_MAX_CHARGE;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Resistance of the wire (higher means slower charge transfer)
	/// </summary>
	public float Resistance { get; set; } = DEFAULT_RESISTANCE;

	/// <summary>
	/// Conductivity of the wire (inverse of resistance)
	/// </summary>
	public float Conductivity => 1.0f / Math.Max(CHARGE_INCONSEQUENTIAL, Resistance);

	#endregion

	#region Methods

	public override void Update(CircuitSimulator simulator, int x, int y, float deltaTime)
	{
		var pos = new Vector2i(x, y);

		// Check and update connections to adjacent components
		UpdateConnections(simulator, pos);

		// Calculate how much charge can flow out of this wire.
		// Lower the resistance to get a higher flow rate.
		var chargeFlowRate = Conductivity * deltaTime;

		// Define directions: up, right, down, left
		Vector2i[] deltas = [
			-Vector2i.UnitY,
			 Vector2i.UnitX,
			 Vector2i.UnitY,
			-Vector2i.UnitX,
		];

		// Track components that can receive charge (have lower charge than us)
		List<(CircuitComponent component, int direction)> receivers = new List<(CircuitComponent, int)>();

		// Find all potential receivers
		for (var i = 0; i < 4; i++)
		{
			if (!_connections[i]) continue;

			var offset = pos + deltas[i];
			var neighbor = simulator.GetComponentAt(offset);
			if (neighbor != null && neighbor.Charge < Charge)
			{
				receivers.Add((neighbor, i));
			}
		}

		// If we have charge and receivers, distribute it
		if (Charge > CHARGE_INCONSEQUENTIAL && receivers.Count > 0)
		{
			// Calculate total charge differential.
			var totalDiff = 0.0f;
			foreach (var (neighbor, _) in receivers)
			{
				// The Max function shouldn't be needed; we already know that Charge - neighbor.Charge > 0.
				// totalDiff += Math.Max(0, Charge - neighbor.Charge);
				totalDiff += Charge - neighbor.Charge;
			}

			if (totalDiff > 0)
			{
				// Determine how much total charge to transfer
				// var totalChargeToTransfer = Math.Min(Charge * chargeFlowRate, Charge * 0.8f); // Not sure what value the "* 0.8f" provides.
				var totalChargeToTransfer = Charge * chargeFlowRate;
				var remainingCharge = totalChargeToTransfer;

				// Distribute charge proportionally based on charge differential.
				foreach (var (neighbor, _) in receivers)
				{
					var diff = Charge - neighbor.Charge;
					var proportion = diff / totalDiff;
					var amountToTransfer = totalChargeToTransfer * proportion;

					if (amountToTransfer > CHARGE_INCONSEQUENTIAL && remainingCharge > 0)
					{
						var actualTransfer = Math.Min(amountToTransfer, remainingCharge);
						neighbor.SetCharge(neighbor.Charge + actualTransfer);
						remainingCharge -= actualTransfer;
					}
				}

				// Update our own charge
				var actualTotalTransferred = totalChargeToTransfer - remainingCharge;
				if (actualTotalTransferred > 0)
				{
					SetCharge(Charge - actualTotalTransferred);
					IsDirty = true;
				}
			}
		}

		// Apply resistance loss.
		if (Charge > CHARGE_INCONSEQUENTIAL)
		{
			var resistanceLoss = Charge * Resistance * deltaTime;
			SetCharge(Charge - resistanceLoss);
		}
		else
		{
			SetCharge(0.0f);
		}
	}

	public override void Render(IRenderingContext rc, Vector2 screenPos, int tileSize)
	{
		// Base wire color - intensity based on charge
		byte colorIntensity = (byte)Math.Min(5, Math.Ceiling(Charge * 5));
		RadialColor wireColor = new RadialColor(0, colorIntensity, Math.Max((byte)1, colorIntensity));

		// Draw wire background
		rc.RenderFilledRect(
			new Box2(screenPos, screenPos + new Vector2(tileSize)),
			new RadialColor(1, 1, 1) // Light background
		);

		// Calculate center point
		Vector2 center = screenPos + new Vector2(tileSize / 2);

		// Draw central connection node
		int nodeSize = tileSize / 5;
		rc.RenderFilledRect(
			new Box2(
				center - new Vector2(nodeSize / 2),
				center + new Vector2(nodeSize / 2)
			),
			wireColor
		);

		// Draw connection lines to adjacent components
		int lineThickness = Math.Max(2, tileSize / 8);
		int[] dx = { 0, 1, 0, -1 }; // Directions: up, right, down, left
		int[] dy = { -1, 0, 1, 0 };

		for (int i = 0; i < 4; i++)
		{
			if (_connections[i])
			{
				// Calculate endpoint at edge of tile
				Vector2 edgePoint = center + new Vector2(
					dx[i] * tileSize / 2,
					dy[i] * tileSize / 2
				);

				// Draw connection line
				if (dx[i] == 0) // Vertical connection (up/down)
				{
					rc.RenderFilledRect(
						new Box2(
							center.X - lineThickness / 2,
							Math.Min(center.Y, edgePoint.Y),
							center.X + lineThickness / 2,
							Math.Max(center.Y, edgePoint.Y)
						),
						wireColor
					);
				}
				else // Horizontal connection (left/right)
				{
					rc.RenderFilledRect(
						new Box2(
							Math.Min(center.X, edgePoint.X),
							center.Y - lineThickness / 2,
							Math.Max(center.X, edgePoint.X),
							center.Y + lineThickness / 2
						),
						wireColor
					);
				}
			}
		}

		// Draw wire border
		rc.RenderRect(
			new Box2(screenPos, screenPos + new Vector2(tileSize)),
			new RadialColor(3, 3, 3)
		);
	}

	#endregion
}