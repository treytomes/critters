using Critters.Gfx;
using OpenTK.Mathematics;

namespace Critters.States.CircuitSim
{
	/// <summary>
	/// Capacitor component that stores and releases electrical charge
	/// </summary>
	class Capacitor : CircuitComponent
	{
		#region Fields

		// Orientation of the capacitor (0 = horizontal, 1 = vertical)
		private int _orientation = 0;

		// Track charge on each plate
		private float _leftPlateCharge = 0f;  // or top plate when vertical
		private float _rightPlateCharge = 0f; // or bottom plate when vertical

		#endregion

		#region Constructors

		public Capacitor()
		{
			// Higher capacitance means more charge storage
			MaxCharge = 1.0f * Capacitance;
		}

		#endregion

		#region Properties

		/// <summary>
		/// Capacitance value (higher means more charge storage)
		/// </summary>
		public float Capacitance { get; set; } = 8.0f;

		/// <summary>
		/// Internal resistance that affects charge/discharge rate
		/// </summary>
		public float InternalResistance { get; set; } = 0.1f;

		#endregion

		/// <summary>
		/// Toggles the orientation of the capacitor
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

			// Define directions: up, right, down, left
			int[] dx = { 0, 1, 0, -1 };
			int[] dy = { -1, 0, 1, 0 };

			// Determine input/output directions based on orientation
			int inputDir, outputDir;
			if (_orientation == 0) // Horizontal
			{
				inputDir = 3;  // Left
				outputDir = 1; // Right
			}
			else // Vertical
			{
				inputDir = 0;  // Up
				outputDir = 2; // Down
			}

			// Get connected components
			CircuitComponent? inputComponent = null;
			CircuitComponent? outputComponent = null;

			if (_connections[inputDir])
			{
				int nx = x + dx[inputDir];
				int ny = y + dy[inputDir];
				inputComponent = simulator.GetComponentAt(nx, ny);
			}

			if (_connections[outputDir])
			{
				int nx = x + dx[outputDir];
				int ny = y + dy[outputDir];
				outputComponent = simulator.GetComponentAt(nx, ny);
			}

			// Update charge on plates
			float chargeTransferRate = deltaTime / (InternalResistance + 1.0f);

			// Input side charging
			if (inputComponent != null && inputComponent.Charge > 0)
			{
				float maxTransfer = Math.Min(
					inputComponent.Charge * chargeTransferRate * 0.5f,
					(MaxCharge - _leftPlateCharge) * chargeTransferRate
				);

				if (maxTransfer > 0.001f)
				{
					inputComponent.SetCharge(inputComponent.Charge - maxTransfer);
					_leftPlateCharge += maxTransfer;
					inputComponent.IsDirty = true;
					IsDirty = true;
				}
			}

			// Calculate potential difference between plates
			float potentialDifference = _leftPlateCharge - _rightPlateCharge;

			// Current flow through the capacitor (charge transfer between plates)
			// In a real capacitor, current can flow briefly as it charges/discharges
			if (Math.Abs(potentialDifference) > 0.001f)
			{
				// The higher the capacitance, the more charge can be stored before plates equalize
				float currentFlow = potentialDifference * (deltaTime / Capacitance);

				// Limit the flow
				currentFlow = Math.Min(currentFlow, _leftPlateCharge);
				currentFlow = Math.Max(currentFlow, -_rightPlateCharge);

				_leftPlateCharge -= currentFlow;
				_rightPlateCharge += currentFlow;
				IsDirty = true;
			}

			// Output side discharging
			if (outputComponent != null && _rightPlateCharge > 0)
			{
				float maxTransfer = Math.Min(
					_rightPlateCharge * chargeTransferRate,
					(outputComponent.MaxCharge - outputComponent.Charge) * chargeTransferRate
				);

				if (maxTransfer > 0.001f)
				{
					outputComponent.SetCharge(outputComponent.Charge + maxTransfer);
					_rightPlateCharge -= maxTransfer;
					outputComponent.IsDirty = true;
					IsDirty = true;
				}
			}

			// Apply a tiny self-discharge (leakage)
			float leakage = 0.01f * deltaTime;
			if (_leftPlateCharge > 0)
			{
				float leftLeakage = _leftPlateCharge * leakage;
				_leftPlateCharge -= leftLeakage;
				IsDirty = true;
			}

			if (_rightPlateCharge > 0)
			{
				float rightLeakage = _rightPlateCharge * leakage;
				_rightPlateCharge -= rightLeakage;
				IsDirty = true;
			}

			// Update overall component charge (for visualization)
			SetCharge((_leftPlateCharge + _rightPlateCharge) / 2);
		}

		public override void Render(IRenderingContext rc, Vector2 screenPos, int tileSize)
		{
			// Capacitor background
			rc.RenderFilledRect(
				new Box2(screenPos, screenPos + new Vector2(tileSize)),
				new RadialColor(1, 1, 2) // Light blue background
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

					// Calculate where the capacitor plates start/end
					float plateDistance = tileSize / 6;

					// Determine wire color based on which plate it's connected to
					float plateCharge = (i == 3 || i == 0) ? _leftPlateCharge : _rightPlateCharge;
					byte colorIntensity = (byte)Math.Min(5, Math.Ceiling(plateCharge * 5));
					RadialColor wireColor = new RadialColor(0, colorIntensity, colorIntensity); // Cyan for capacitor

					if (dx[i] == 0) // Vertical connection (up/down)
					{
						float plateY = (i == 0) ? center.Y - plateDistance : center.Y + plateDistance;

						rc.RenderFilledRect(
							new Box2(
								center.X - lineThickness / 2,
								(i == 0) ? edgePoint.Y : plateY,
								center.X + lineThickness / 2,
								(i == 0) ? plateY : edgePoint.Y
							),
							wireColor
						);
					}
					else // Horizontal connection (left/right)
					{
						float plateX = (i == 3) ? center.X - plateDistance : center.X + plateDistance;

						rc.RenderFilledRect(
							new Box2(
								(i == 3) ? edgePoint.X : plateX,
								center.Y - lineThickness / 2,
								(i == 3) ? plateX : edgePoint.X,
								center.Y + lineThickness / 2
							),
							wireColor
						);
					}
				}
			}

			// Draw capacitor plates
			int plateWidth, plateHeight, plateGap;
			if (_orientation == 0) // Horizontal
			{
				plateWidth = tileSize / 12;
				plateHeight = tileSize / 2;
				plateGap = tileSize / 6;
			}
			else // Vertical
			{
				plateWidth = tileSize / 2;
				plateHeight = tileSize / 12;
				plateGap = tileSize / 6;
			}

			// Determine plate colors based on their individual charge
			byte leftChargeIntensity = (byte)Math.Min(5, Math.Ceiling(_leftPlateCharge * 5));
			byte rightChargeIntensity = (byte)Math.Min(5, Math.Ceiling(_rightPlateCharge * 5));
			RadialColor leftPlateColor = new RadialColor(0, leftChargeIntensity, leftChargeIntensity); // Cyan for negative
			RadialColor rightPlateColor = new RadialColor(rightChargeIntensity, 0, rightChargeIntensity); // Magenta for positive

			if (_orientation == 0) // Horizontal plates
			{
				// Left plate
				rc.RenderFilledRect(
					new Box2(
						center.X - plateGap - plateWidth,
						center.Y - plateHeight / 2,
						center.X - plateGap,
						center.Y + plateHeight / 2
					),
					leftPlateColor
				);

				// Right plate
				rc.RenderFilledRect(
					new Box2(
						center.X + plateGap,
						center.Y - plateHeight / 2,
						center.X + plateGap + plateWidth,
						center.Y + plateHeight / 2
					),
					rightPlateColor
				);

				// Draw electric field between plates if charged
				float chargeDifference = Math.Abs(_leftPlateCharge - _rightPlateCharge);
				if (chargeDifference > 0.01f)
				{
					byte fieldIntensity = (byte)Math.Min(5, Math.Ceiling(chargeDifference * 5));

					// Draw field lines
					int numLines = 3 + (int)(chargeDifference * 2);
					float lineSpacing = plateHeight / (numLines + 1);

					for (int i = 1; i <= numLines; i++)
					{
						float y = center.Y - plateHeight / 2 + i * lineSpacing;
						rc.RenderLine(
							(int)(center.X - plateGap + 1),
							(int)y,
							(int)(center.X + plateGap - 1),
							(int)y,
							fieldIntensity
						);
					}
				}
			}
			else // Vertical plates
			{
				// Top plate
				rc.RenderFilledRect(
					new Box2(
						center.X - plateWidth / 2,
						center.Y - plateGap - plateHeight,
						center.X + plateWidth / 2,
						center.Y - plateGap
					),
					leftPlateColor
				);

				// Bottom plate
				rc.RenderFilledRect(
					new Box2(
						center.X - plateWidth / 2,
						center.Y + plateGap,
						center.X + plateWidth / 2,
						center.Y + plateGap + plateHeight
					),
					rightPlateColor
				);

				// Draw electric field between plates if charged
				float chargeDifference = Math.Abs(_leftPlateCharge - _rightPlateCharge);
				if (chargeDifference > 0.01f)
				{
					byte fieldIntensity = (byte)Math.Min(5, Math.Ceiling(chargeDifference * 5));

					// Draw field lines
					int numLines = 3 + (int)(chargeDifference * 2);
					float lineSpacing = plateWidth / (numLines + 1);

					for (int i = 1; i <= numLines; i++)
					{
						float x = center.X - plateWidth / 2 + i * lineSpacing;
						rc.RenderLine(
							(int)x,
							(int)(center.Y - plateGap + 1),
							(int)x,
							(int)(center.Y + plateGap - 1),
							fieldIntensity
						);
					}
				}
			}

			// Draw component border
			rc.RenderRect(
				new Box2(screenPos, screenPos + new Vector2(tileSize)),
				new RadialColor(3, 3, 4)
			);
		}

		// TODO: Implement the interactivity interface.
		/// <summary>
		/// Toggles the orientation of the capacitor when clicked
		/// </summary>
		public bool HandleClick(Vector2 worldPosition)
		{
			ToggleOrientation();
			return true;
		}
	}
}