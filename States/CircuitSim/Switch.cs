using Critters.Gfx;
using OpenTK.Mathematics;

namespace Critters.States.CircuitSim;

/// <summary>
/// Switch component that can be toggled on/off to control circuit flow
/// </summary>
class Switch : CircuitComponent, IInteractiveComponent
{
	/// <summary>
	/// Whether the switch is in the ON position
	/// </summary>
	public bool IsOn { get; private set; } = false;

	/// <summary>
	/// Resistance when switch is on (very low)
	/// </summary>
	public float OnResistance { get; set; } = 0.05f;

	/// <summary>
	/// Resistance when switch is off (very high)
	/// </summary>
	public float OffResistance { get; set; } = 100.0f;

	public Switch()
	{
		MaxCharge = 0.8f;
	}

	public override void Update(CircuitSimulator simulator, int x, int y, float deltaTime)
	{
		var pos = new Vector2i(x, y);

		// Update connections
		UpdateConnections(simulator, pos);

		if (!IsOn)
		{
			// When off, slowly dissipate any charge
			if (Charge > 0.001f)
			{
				float dissipation = Charge * 0.5f * deltaTime;
				SetCharge(Charge - dissipation);
			}
			return;
		}

		// When on, behave similar to a wire with low resistance
		if (Charge <= 0.001f)
			return;

		// Calculate how much charge can flow out
		float chargeFlowRate = (1.0f / OnResistance) * deltaTime * 5.0f;

		// Define directions: up, right, down, left
		int[] dx = { 0, 1, 0, -1 };
		int[] dy = { -1, 0, 1, 0 };

		// Track components that can receive charge
		List<(CircuitComponent component, int direction)> receivers = new List<(CircuitComponent, int)>();

		// Find all potential receivers
		for (int i = 0; i < 4; i++)
		{
			if (!_connections[i]) continue;

			int nx = x + dx[i];
			int ny = y + dy[i];

			var neighbor = simulator.GetComponentAt(nx, ny);
			if (neighbor != null && neighbor.Charge < this.Charge)
			{
				receivers.Add((neighbor, i));
			}
		}

		// If we have charge and receivers, distribute it
		if (Charge > 0.001f && receivers.Count > 0)
		{
			float totalChargeToTransfer = Math.Min(Charge * chargeFlowRate, Charge * 0.8f);
			float remainingCharge = totalChargeToTransfer;

			foreach (var (neighbor, _) in receivers)
			{
				float amountToTransfer = totalChargeToTransfer / receivers.Count;

				if (amountToTransfer > 0.001f && remainingCharge > 0)
				{
					float actualTransfer = Math.Min(amountToTransfer, remainingCharge);
					neighbor.SetCharge(neighbor.Charge + actualTransfer);
					remainingCharge -= actualTransfer;
					neighbor.IsDirty = true;
				}
			}

			// Update our own charge
			float actualTotalTransferred = totalChargeToTransfer - remainingCharge;
			if (actualTotalTransferred > 0)
			{
				SetCharge(Charge - actualTotalTransferred);
				IsDirty = true;
			}
		}
	}

	public override void Render(IRenderingContext rc, Vector2 screenPos, int tileSize)
	{
		// Switch background
		rc.RenderFilledRect(
			new Box2(screenPos, screenPos + new Vector2(tileSize)),
			new RadialColor(1, 1, 2) // Light blue background
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

				// Draw connection line - colored based on switch state
				RadialColor lineColor = IsOn ?
					new RadialColor(0, Math.Max((byte)1, (byte)Math.Ceiling(Charge * 5)), 5) : // Blue when on
					new RadialColor(2, 2, 2); // Gray when off

				if (dx[i] == 0) // Vertical connection (up/down)
				{
					rc.RenderFilledRect(
						new Box2(
							center.X - lineThickness / 2,
							Math.Min(center.Y, edgePoint.Y),
							center.X + lineThickness / 2,
							Math.Max(center.Y, edgePoint.Y)
						),
						lineColor
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
						lineColor
					);
				}
			}
		}

		// Draw switch border
		rc.RenderRect(
			new Box2(screenPos, screenPos + new Vector2(tileSize)),
			new RadialColor(3, 3, 4)
		);

		// Draw switch lever
		int leverLength = tileSize / 2;
		int leverWidth = tileSize / 8;

		// Calculate lever position based on state
		float leverAngle = (float)(IsOn ? Math.PI / 4 : -Math.PI / 4);
		Vector2 leverEnd = center + new Vector2(
			(float)Math.Cos(leverAngle) * leverLength,
			(float)Math.Sin(leverAngle) * leverLength
		);

		// Draw lever
		rc.RenderLine(
			(int)center.X, (int)center.Y,
			(int)leverEnd.X, (int)leverEnd.Y,
			IsOn ? (byte)5 : (byte)3
		);

		// Draw lever knob
		int knobSize = tileSize / 5;
		rc.RenderFilledCircle(
			(int)leverEnd.X, (int)leverEnd.Y,
			knobSize / 2,
			(IsOn ? new RadialColor(0, 5, 0) : new RadialColor(5, 0, 0)).Index
		);

		// Show charge level if any
		if (Charge > 0.01f)
		{
			byte chargeIntensity = (byte)Math.Min(5, Math.Ceiling(Charge * 5));
			rc.RenderFilledCircle(
				(int)center.X, (int)center.Y,
				tileSize / 8,
				new RadialColor(0, chargeIntensity, chargeIntensity).Index
			);
		}
	}

	/// <summary>
	/// Toggles the switch between ON and OFF states
	/// </summary>
	public void Toggle()
	{
		IsOn = !IsOn;
		IsDirty = true;
	}

	#region IInteractiveComponent Implementation

	public bool HandleClick(Vector2 worldPosition)
	{
		Toggle();
		return true;
	}

	public bool HandlePress(Vector2 worldPosition)
	{
		// Switch only cares about clicks, not presses
		return true;
	}

	public bool HandleRelease(Vector2 worldPosition)
	{
		// Switch only cares about clicks, not releases
		return false;
	}

	#endregion
}
