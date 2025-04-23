using Critters.Gfx;
using OpenTK.Mathematics;

namespace Critters.States.CircuitSim;

/// <summary>
/// Button component that allows current to flow only while pressed
/// </summary>
class Button : CircuitComponent, IInteractiveComponent
{
	/// <summary>
	/// Whether the button is currently pressed
	/// </summary>
	public bool IsPressed { get; private set; } = false;

	/// <summary>
	/// Resistance when button is pressed (very low)
	/// </summary>
	public float PressedResistance { get; set; } = 0.05f;

	/// <summary>
	/// Resistance when button is released (very high)
	/// </summary>
	public float ReleasedResistance { get; set; } = 100.0f;

	/// <summary>
	/// Animation time for button press/release
	/// </summary>
	private float _animationTime = 0;

	/// <summary>
	/// Animation duration in seconds
	/// </summary>
	private const float ANIMATION_DURATION = 0.2f;

	// Connection flags for each direction (up, right, down, left)
	private bool[] _connections = new bool[4];

	public Button()
	{
		MaxCharge = 0.8f;
	}

	public override void Update(CircuitSimulator simulator, int x, int y, float deltaTime)
	{
		// Update connections
		UpdateConnections(simulator, x, y);

		// Update animation
		if (IsPressed && _animationTime < ANIMATION_DURATION)
		{
			_animationTime = Math.Min(_animationTime + deltaTime, ANIMATION_DURATION);
			IsDirty = true;
		}
		else if (!IsPressed && _animationTime > 0)
		{
			_animationTime = Math.Max(_animationTime - deltaTime, 0);
			IsDirty = true;
		}

		if (!IsPressed)
		{
			// When not pressed, slowly dissipate any charge
			if (Charge > 0.001f)
			{
				float dissipation = Charge * 0.5f * deltaTime;
				SetCharge(Charge - dissipation);
			}
			return;
		}

		// When pressed, behave similar to a wire with low resistance
		if (Charge <= 0.001f)
			return;

		// Calculate how much charge can flow out
		float chargeFlowRate = (1.0f / PressedResistance) * deltaTime * 5.0f;

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
		// Button background
		rc.RenderFilledRect(
			new Box2(screenPos, screenPos + new Vector2(tileSize)),
			new RadialColor(2, 1, 2) // Light purple background
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

				// Draw connection line - colored based on button state
				RadialColor lineColor = IsPressed ?
					new RadialColor(5, Math.Max((byte)1, (byte)Math.Ceiling(Charge * 5)), 0) : // Yellow when pressed
					new RadialColor(2, 2, 2); // Gray when released

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

		// Draw button border
		rc.RenderRect(
			new Box2(screenPos, screenPos + new Vector2(tileSize)),
			new RadialColor(4, 3, 4)
		);

		// Calculate button press animation
		float animationProgress = _animationTime / ANIMATION_DURATION;
		int buttonSize = tileSize * 3 / 4;
		int pressDepth = tileSize / 6;
		int currentDepth = (int)(pressDepth * animationProgress);

		// Draw button base (shadow)
		int shadowOffset = 2;
		rc.RenderFilledRect(
			new Box2(
				center.X - buttonSize / 2 + shadowOffset,
				center.Y - buttonSize / 2 + shadowOffset + pressDepth,
				center.X + buttonSize / 2 + shadowOffset,
				center.Y + buttonSize / 2 + shadowOffset + pressDepth
			),
			new RadialColor(1, 1, 1)
		);

		// Draw button face
		rc.RenderFilledRect(
			new Box2(
				center.X - buttonSize / 2,
				center.Y - buttonSize / 2 + currentDepth,
				center.X + buttonSize / 2,
				center.Y + buttonSize / 2 + currentDepth
			),
			IsPressed ? new RadialColor(5, 3, 0) : new RadialColor(5, 0, 0)
		);

		// Draw button border
		rc.RenderRect(
			new Box2(
				center.X - buttonSize / 2,
				center.Y - buttonSize / 2 + currentDepth,
				center.X + buttonSize / 2,
				center.Y + buttonSize / 2 + currentDepth
			),
			new RadialColor(5, 5, 5)
		);

		// Show charge level if any
		if (Charge > 0.01f)
		{
			byte chargeIntensity = (byte)Math.Min(5, Math.Ceiling(Charge * 5));
			rc.RenderFilledCircle(
				(int)center.X,
				(int)(center.Y + currentDepth),
				tileSize / 10,
				new RadialColor(chargeIntensity, chargeIntensity, 0).Index
			);
		}
	}

	/// <summary>
	/// Sets the pressed state of the button
	/// </summary>
	public void SetPressed(bool pressed)
	{
		if (IsPressed != pressed)
		{
			IsPressed = pressed;
			IsDirty = true;
		}
	}

	#region IInteractiveComponent Implementation

	public bool HandleClick(Vector2 worldPosition)
	{
		// Buttons respond to press and release, not clicks
		return false;
	}

	public bool HandlePress(Vector2 worldPosition)
	{
		SetPressed(true);
		return true;
	}

	public bool HandleRelease(Vector2 worldPosition)
	{
		SetPressed(false);
		return true;
	}

	#endregion
}
