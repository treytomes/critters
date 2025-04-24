using Critters.Gfx;
using OpenTK.Mathematics;

namespace Critters.States.CircuitSim
{
	/// <summary>
	/// Resistor component that limits the flow of electricity
	/// </summary>
	class Resistor : CircuitComponent
	{
		/// <summary>
		/// Resistance value (higher means more resistance to current flow)
		/// </summary>
		public float ResistanceValue { get; set; } = 20.0f;

		/// <summary>
		/// Conductivity of the resistor (inverse of resistance)
		/// </summary>
		public float Conductivity => 1.0f / Math.Max(0.01f, ResistanceValue);

		// Orientation of the resistor (0 = horizontal, 1 = vertical)
		private int _orientation = 0;

		public Resistor()
		{
			MaxCharge = 0.6f;
		}

		/// <summary>
		/// Toggles the orientation of the resistor
		/// </summary>
		public void ToggleOrientation()
		{
			_orientation = (_orientation + 1) % 2;
			IsDirty = true;
		}

		public override void Update(CircuitSimulator simulator, int x, int y, float deltaTime)
		{
			var pos = new Vector2i(x, y);

			// Update connections
			UpdateConnections(simulator, pos);

			if (Charge <= 0.001f)
				return;

			// Calculate how much charge can flow through the resistor
			float chargeFlowRate = Conductivity * deltaTime;

			// Define directions: up, right, down, left
			int[] dx = { 0, 1, 0, -1 };
			int[] dy = { -1, 0, 1, 0 };

			// Determine which directions to check based on orientation
			int[] validDirections;
			if (_orientation == 0) // Horizontal
				validDirections = new int[] { 1, 3 }; // Right, Left
			else // Vertical
				validDirections = new int[] { 0, 2 }; // Up, Down

			// Track components that can receive charge
			List<(CircuitComponent component, int direction)> receivers = new List<(CircuitComponent, int)>();

			// Find all potential receivers in valid directions
			foreach (int dir in validDirections)
			{
				if (!_connections[dir]) continue;

				int nx = x + dx[dir];
				int ny = y + dy[dir];

				var neighbor = simulator.GetComponentAt(nx, ny);
				if (neighbor != null && neighbor.Charge < this.Charge)
				{
					receivers.Add((neighbor, dir));
				}
			}

			// If we have charge and receivers, distribute it
			if (Charge > 0.001f && receivers.Count > 0)
			{
				// Calculate total charge differential
				float totalDiff = 0;
				foreach (var (neighbor, _) in receivers)
				{
					totalDiff += Math.Max(0, this.Charge - neighbor.Charge);
				}

				if (totalDiff > 0)
				{
					// Determine how much total charge to transfer - limited by resistance
					float totalChargeToTransfer = Math.Min(Charge * chargeFlowRate, Charge * 0.5f);
					float remainingCharge = totalChargeToTransfer;

					// Distribute charge proportionally based on charge differential
					foreach (var (neighbor, _) in receivers)
					{
						float diff = Math.Max(0, this.Charge - neighbor.Charge);
						float proportion = diff / totalDiff;
						float amountToTransfer = totalChargeToTransfer * proportion;

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

			// Apply internal resistance loss
			if (Charge > 0.001f)
			{
				float resistanceLoss = Charge * (ResistanceValue * 0.01f) * deltaTime;
				SetCharge(Charge - resistanceLoss);
			}
		}

		public override void Render(IRenderingContext rc, Vector2 screenPos, int tileSize)
		{
			// Resistor background
			rc.RenderFilledRect(
				new Box2(screenPos, screenPos + new Vector2(tileSize)),
				new RadialColor(1, 1, 1) // Light background
			);

			// Calculate center point
			Vector2 center = screenPos + new Vector2(tileSize / 2);

			// Draw connection lines to adjacent components
			int lineThickness = Math.Max(2, tileSize / 10);
			int[] dx = { 0, 1, 0, -1 }; // Directions: up, right, down, left
			int[] dy = { -1, 0, 1, 0 };

			// Determine which directions to render connections based on orientation
			int[] validDirections;
			if (_orientation == 0) // Horizontal
				validDirections = new int[] { 1, 3 }; // Right, Left
			else // Vertical
				validDirections = new int[] { 0, 2 }; // Up, Down

			// Draw valid connections
			foreach (int i in validDirections)
			{
				if (_connections[i])
				{
					// Calculate endpoint at edge of tile
					Vector2 edgePoint = center + new Vector2(
						dx[i] * tileSize / 2,
						dy[i] * tileSize / 2
					);

					// Draw connection line
					byte colorIntensity = (byte)Math.Min(5, Math.Ceiling(Charge * 5));
					RadialColor wireColor = new RadialColor(colorIntensity, 0, 0); // Red for resistor

					if (dx[i] == 0) // Vertical connection (up/down)
					{
						// Calculate where the resistor body starts/ends
						float resistorStart = center.Y - tileSize / 4;
						float resistorEnd = center.Y + tileSize / 4;

						if (i == 0) // Up
						{
							rc.RenderFilledRect(
								new Box2(
									center.X - lineThickness / 2,
									edgePoint.Y,
									center.X + lineThickness / 2,
									resistorStart
								),
								wireColor
							);
						}
						else // Down
						{
							rc.RenderFilledRect(
								new Box2(
									center.X - lineThickness / 2,
									resistorEnd,
									center.X + lineThickness / 2,
									edgePoint.Y
								),
								wireColor
							);
						}
					}
					else // Horizontal connection (left/right)
					{
						// Calculate where the resistor body starts/ends
						float resistorStart = center.X - tileSize / 4;
						float resistorEnd = center.X + tileSize / 4;

						if (i == 1) // Right
						{
							rc.RenderFilledRect(
								new Box2(
									resistorEnd,
									center.Y - lineThickness / 2,
									edgePoint.X,
									center.Y + lineThickness / 2
								),
								wireColor
							);
						}
						else // Left
						{
							rc.RenderFilledRect(
								new Box2(
									edgePoint.X,
									center.Y - lineThickness / 2,
									resistorStart,
									center.Y + lineThickness / 2
								),
								wireColor
							);
						}
					}
				}
			}

			// Draw resistor symbol
			int resistorWidth, resistorHeight;
			if (_orientation == 0) // Horizontal
			{
				resistorWidth = tileSize / 2;
				resistorHeight = tileSize / 3;
			}
			else // Vertical
			{
				resistorWidth = tileSize / 3;
				resistorHeight = tileSize / 2;
			}

			// Draw resistor body
			RadialColor resistorColor = new RadialColor(4, 1, 0); // Brownish
			rc.RenderFilledRect(
				new Box2(
					center.X - resistorWidth / 2,
					center.Y - resistorHeight / 2,
					center.X + resistorWidth / 2,
					center.Y + resistorHeight / 2
				),
				resistorColor
			);

			// Draw zigzag pattern inside resistor
			int zigzagCount = 3;
			int zigzagWidth = resistorWidth - 4;
			int zigzagHeight = resistorHeight / 3;

			if (_orientation == 0) // Horizontal zigzag
			{
				float startX = center.X - zigzagWidth / 2;
				float endX = center.X + zigzagWidth / 2;
				float segmentWidth = zigzagWidth / zigzagCount;

				for (int i = 0; i < zigzagCount; i++)
				{
					float x1 = startX + i * segmentWidth;
					float x2 = startX + (i + 1) * segmentWidth;
					float y1 = center.Y - zigzagHeight / 2;
					float y2 = center.Y + zigzagHeight / 2;

					if (i % 2 == 0)
					{
						rc.RenderLine((int)x1, (int)y1, (int)x2, (int)y2, 5);
					}
					else
					{
						rc.RenderLine((int)x1, (int)y2, (int)x2, (int)y1, 5);
					}
				}
			}
			else // Vertical zigzag
			{
				float startY = center.Y - zigzagWidth / 2;
				float endY = center.Y + zigzagWidth / 2;
				float segmentHeight = zigzagWidth / zigzagCount;

				for (int i = 0; i < zigzagCount; i++)
				{
					float y1 = startY + i * segmentHeight;
					float y2 = startY + (i + 1) * segmentHeight;
					float x1 = center.X - zigzagHeight / 2;
					float x2 = center.X + zigzagHeight / 2;

					if (i % 2 == 0)
					{
						rc.RenderLine((int)x1, (int)y1, (int)x2, (int)y2, 5);
					}
					else
					{
						rc.RenderLine((int)x2, (int)y1, (int)x1, (int)y2, 5);
					}
				}
			}

			// Draw component border
			rc.RenderRect(
				new Box2(screenPos, screenPos + new Vector2(tileSize)),
				new RadialColor(3, 3, 3)
			);

			// Show charge level
			if (Charge > 0.01f)
			{
				byte chargeIntensity = (byte)Math.Min(5, Math.Ceiling(Charge * 5));
				rc.RenderFilledCircle(
					(int)center.X,
					(int)center.Y,
					tileSize / 10,
					new RadialColor(chargeIntensity, 0, 0).Index
				);
			}
		}
	}
}