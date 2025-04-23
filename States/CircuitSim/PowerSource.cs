using Critters.Gfx;
using OpenTK.Mathematics;

namespace Critters.States.CircuitSim;

/// <summary>
/// Power source component that generates electricity
/// </summary>
class PowerSource : CircuitComponent
{
	/// <summary>
	/// Voltage of the power source.
	/// </summary>
	/// <remarks>
	/// Defaulting to 3; most circuits are 3-5V.
	/// </remarks>
	public float Voltage { get; set; } = 5.0f;

	/// <summary>
	/// Whether the power source is active
	/// </summary>
	public bool IsActive { get; set; } = true;

	// Connection flags for each direction (up, right, down, left)
	private bool[] _connections = new bool[4];

	public PowerSource()
	{
		MaxCharge = 1.0f;
		Charge = MaxCharge;
	}

	public override void Update(CircuitSimulator simulator, int x, int y, float deltaTime)
	{
		if (!IsActive)
			return;

		// Maintain full charge
		SetCharge(MaxCharge);

		// Update connections
		UpdateConnections(simulator, x, y);

		// Output charge to adjacent components
		float outputCharge = Voltage * deltaTime * 3.0f; // Increased output rate

		// Check adjacent cells (up, right, down, left)
		int[] dx = { 0, 1, 0, -1 };
		int[] dy = { -1, 0, 1, 0 };

		for (int i = 0; i < 4; i++)
		{
			if (!_connections[i]) continue;

			int nx = x + dx[i];
			int ny = y + dy[i];

			var neighbor = simulator.GetComponentAt(nx, ny);
			if (neighbor != null && !(neighbor is PowerSource) && neighbor.Charge < neighbor.MaxCharge)
			{
				// Calculate how much charge the neighbor can accept
				float chargeDeficit = neighbor.MaxCharge - neighbor.Charge;
				float transferAmount = Math.Min(outputCharge, chargeDeficit);

				if (transferAmount > 0.001f)
				{
					neighbor.SetCharge(neighbor.Charge + transferAmount);
					neighbor.IsDirty = true;
				}
			}
		}
	}

	/// <summary>
	/// Updates connection information with adjacent components
	/// </summary>
	private void UpdateConnections(CircuitSimulator simulator, int x, int y)
	{
		// Check adjacent cells (up, right, down, left)
		int[] dx = { 0, 1, 0, -1 };
		int[] dy = { -1, 0, 1, 0 };

		for (int i = 0; i < 4; i++)
		{
			int nx = x + dx[i];
			int ny = y + dy[i];

			var neighbor = simulator.GetComponentAt(nx, ny);
			_connections[i] = (neighbor != null);
		}
	}

	public override void Render(IRenderingContext rc, Vector2 screenPos, int tileSize)
	{
		// Power source background
		rc.RenderFilledRect(
			new Box2(screenPos, screenPos + new Vector2(tileSize)),
			new RadialColor(5, IsActive ? (byte)4 : (byte)1, 0)
		);

		// Calculate center point
		Vector2 center = screenPos + new Vector2(tileSize / 2);

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
						new RadialColor(5, 5, 0) // Yellow connection
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
						new RadialColor(5, 5, 0) // Yellow connection
					);
				}
			}
		}

		// Power source border
		rc.RenderRect(
			new Box2(screenPos, screenPos + new Vector2(tileSize)),
			new RadialColor(5, 5, 5)
		);

		// Draw + symbol
		int symbolSize = tileSize / 2;

		// Horizontal line
		rc.RenderFilledRect(
			new Box2(
				center - new Vector2(symbolSize / 2, 2),
				center + new Vector2(symbolSize / 2, 2)
			),
			new RadialColor(5, 5, 5)
		);

		// Vertical line
		rc.RenderFilledRect(
			new Box2(
				center - new Vector2(2, symbolSize / 2),
				center + new Vector2(2, symbolSize / 2)
			),
			new RadialColor(5, 5, 5)
		);
	}

	public override bool SetCharge(float newCharge)
	{
		// Power sources maintain their charge based on voltage
		if (IsActive && newCharge < MaxCharge)
		{
			Charge = MaxCharge;
			return true;
		}
		return false;
	}
}