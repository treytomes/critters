using Critters.Gfx;
using OpenTK.Mathematics;
using System;

namespace Critters.States.CircuitSim
{
	/// <summary>
	/// Transistor component that allows current flow only when the base is energized
	/// </summary>
	class Transistor : CircuitComponent, IInteractiveComponent
	{
		/// <summary>
		/// Orientation of the transistor (0 = up, 1 = right, 2 = down, 3 = left)
		/// Base is always on the side opposite to the orientation
		/// </summary>
		public int Orientation { get; private set; } = 0;

		/// <summary>
		/// Minimum charge required at the base to activate the transistor
		/// </summary>
		public float ActivationThreshold { get; set; } = 0.1f;

		/// <summary>
		/// Resistance when transistor is activated (lower = more flow)
		/// </summary>
		/// <remarks>
		/// Reduced from 0.1f
		/// </remarks>
		public float OnResistance { get; set; } = 0.05f;

		/// <summary>
		/// Resistance when transistor is deactivated
		/// </summary>
		public float OffResistance { get; set; } = 100.0f;

		/// <summary>
		/// Whether the transistor is currently activated
		/// </summary>
		private bool _isActivated = false;

		/// <summary>
		/// Charge stored at the collector terminal
		/// </summary>
		private float _collectorCharge = 0;

		/// <summary>
		/// Charge stored at the base terminal
		/// </summary>
		private float _baseCharge = 0;

		/// <summary>
		/// Charge stored at the emitter terminal
		/// </summary>
		private float _emitterCharge = 0;

		// Debug flag to help troubleshoot
		private bool _debug = true;

		public Transistor()
		{
			MaxCharge = 1.0f;
		}

		/// <summary>
		/// Rotates the transistor to the next orientation
		/// </summary>
		public void Rotate()
		{
			Orientation = (Orientation + 1) % 4;
			IsDirty = true;
		}

		/// <summary>
		/// Gets the index of the collector terminal based on current orientation
		/// </summary>
		private int GetCollectorIndex()
		{
			return Orientation;
		}

		/// <summary>
		/// Gets the index of the base terminal based on current orientation
		/// </summary>
		private int GetBaseIndex()
		{
			// Base is perpendicular to the orientation (clockwise)
			return (Orientation + 1) % 4;
		}

		/// <summary>
		/// Gets the index of the emitter terminal based on current orientation
		/// </summary>
		private int GetEmitterIndex()
		{
			// Emitter is opposite to the collector
			return (Orientation + 2) % 4;
		}

		public override void Update(CircuitSimulator simulator, int x, int y, float deltaTime)
		{
			var pos = new Vector2i(x, y);

			// Update connections
			UpdateConnections(simulator, pos);

			// Define directions: up, right, down, left
			int[] dx = { 0, 1, 0, -1 };
			int[] dy = { -1, 0, 1, 0 };

			// Get indices for terminals
			int collectorIdx = GetCollectorIndex();
			int baseIdx = GetBaseIndex();
			int emitterIdx = GetEmitterIndex();

			// Get neighboring components for each terminal
			CircuitComponent? collectorNeighbor = null;
			CircuitComponent? baseNeighbor = null;
			CircuitComponent? emitterNeighbor = null;

			if (_connections[collectorIdx])
			{
				collectorNeighbor = simulator.GetComponentAt(
					x + dx[collectorIdx],
					y + dy[collectorIdx]
				);
			}

			if (_connections[baseIdx])
			{
				baseNeighbor = simulator.GetComponentAt(
					x + dx[baseIdx],
					y + dy[baseIdx]
				);
			}

			if (_connections[emitterIdx])
			{
				emitterNeighbor = simulator.GetComponentAt(
					x + dx[emitterIdx],
					y + dy[emitterIdx]
				);
			}

			// Update base charge from neighbor
			if (baseNeighbor != null)
			{
				float chargeDifference = baseNeighbor.Charge - _baseCharge;
				if (chargeDifference > 0)
				{
					float transferAmount = chargeDifference * 0.9f * deltaTime * 8.0f; // Increased transfer rate
					_baseCharge += transferAmount;
					IsDirty = true;
				}
			}

			// Base slowly discharges - reduced discharge rate
			_baseCharge *= (1.0f - 0.1f * deltaTime); // Reduced from 0.2f
			if (_baseCharge < 0.001f) _baseCharge = 0;

			// Update collector charge from neighbor - increased transfer rate
			if (collectorNeighbor != null)
			{
				float chargeDifference = collectorNeighbor.Charge - _collectorCharge;
				if (chargeDifference > 0)
				{
					float transferAmount = chargeDifference * 0.9f * deltaTime * 8.0f; // Increased from 0.8f and 5.0f
					_collectorCharge += transferAmount;
					IsDirty = true;
				}
			}

			// Update transistor activation state
			bool wasActivated = _isActivated;
			_isActivated = _baseCharge >= ActivationThreshold;

			// If activation state changed, mark as dirty
			if (wasActivated != _isActivated)
			{
				IsDirty = true;
			}

			// If transistor is activated, allow charge to flow from collector to emitter
			if (_isActivated && _collectorCharge > 0.001f)
			{
				// Calculate flow rate based on resistance - increased flow rate
				float chargeFlowRate = (1.0f / OnResistance) * deltaTime * 10.0f; // Increased from 5.0f
				float transferAmount = _collectorCharge * chargeFlowRate;

				// Limit transfer amount to avoid complete drainage in one step
				transferAmount = Math.Min(transferAmount, _collectorCharge * 0.8f);

				// Transfer charge to emitter
				_emitterCharge += transferAmount;
				_collectorCharge -= transferAmount;

				// Cap emitter charge at max
				if (_emitterCharge > MaxCharge)
				{
					_emitterCharge = MaxCharge;
				}

				IsDirty = true;
			}
			else
			{
				// When not activated, emitter slowly discharges - reduced discharge rate
				_emitterCharge *= (1.0f - 0.2f * deltaTime); // Reduced from 0.5f
				if (_emitterCharge < 0.001f) _emitterCharge = 0;
			}

			// Transfer charge from emitter to neighbor - increased transfer rate
			if (emitterNeighbor != null && _emitterCharge > 0.001f)
			{
				float chargeDifference = _emitterCharge - emitterNeighbor.Charge;
				if (chargeDifference > 0)
				{
					float transferAmount = chargeDifference * 0.9f * deltaTime * 8.0f; // Increased from 0.8f and 5.0f
					transferAmount = Math.Min(transferAmount, _emitterCharge);

					emitterNeighbor.SetCharge(emitterNeighbor.Charge + transferAmount);
					_emitterCharge -= transferAmount;

					emitterNeighbor.IsDirty = true;
					IsDirty = true;
				}
			}

			// Update overall component charge (for visual purposes)
			Charge = Math.Max(Math.Max(_baseCharge, _collectorCharge), _emitterCharge);
		}

		public override void Render(IRenderingContext rc, Vector2 screenPos, int tileSize)
		{
			// Transistor background
			rc.RenderFilledRect(
				new Box2(screenPos, screenPos + new Vector2(tileSize)),
				new RadialColor(1, 1, 1)
			);

			// Calculate center point
			Vector2 center = screenPos + new Vector2(tileSize / 2);

			// Draw connection lines to adjacent components
			int lineThickness = Math.Max(2, tileSize / 8);
			int[] dx = { 0, 1, 0, -1 }; // Directions: up, right, down, left
			int[] dy = { -1, 0, 1, 0 };

			int collectorIdx = GetCollectorIndex();
			int baseIdx = GetBaseIndex();
			int emitterIdx = GetEmitterIndex();

			for (int i = 0; i < 4; i++)
			{
				if (_connections[i])
				{
					// Calculate endpoint at edge of tile
					Vector2 edgePoint = center + new Vector2(
						dx[i] * tileSize / 2,
						dy[i] * tileSize / 2
					);

					// Determine line color based on terminal type
					RadialColor lineColor;
					if (i == collectorIdx)
					{
						// Collector - Red
						lineColor = new RadialColor(
							Math.Max((byte)2, (byte)Math.Ceiling(_collectorCharge * 5)),
							0,
							0
						);
					}
					else if (i == baseIdx)
					{
						// Base - Green
						lineColor = new RadialColor(
							0,
							Math.Max((byte)2, (byte)Math.Ceiling(_baseCharge * 5)),
							0
						);
					}
					else if (i == emitterIdx)
					{
						// Emitter - Blue
						lineColor = new RadialColor(
							0,
							0,
							Math.Max((byte)2, (byte)Math.Ceiling(_emitterCharge * 5))
						);
					}
					else
					{
						// Unused terminal - Gray
						lineColor = new RadialColor(2, 2, 2);
					}

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

			// Draw transistor border
			rc.RenderRect(
				new Box2(screenPos, screenPos + new Vector2(tileSize)),
				new RadialColor(3, 3, 3)
			);

			// Draw transistor symbol
			int symbolSize = tileSize * 3 / 5;

			// Calculate the points for the transistor symbol
			Vector2 collectorPoint = center + new Vector2(
				dx[collectorIdx] * symbolSize / 3,
				dy[collectorIdx] * symbolSize / 3
			);

			Vector2 emitterPoint = center + new Vector2(
				dx[emitterIdx] * symbolSize / 3,
				dy[emitterIdx] * symbolSize / 3
			);

			Vector2 basePoint = center + new Vector2(
				dx[baseIdx] * symbolSize / 3,
				dy[baseIdx] * symbolSize / 3
			);

			// Draw the base line
			rc.RenderLine(
				(int)center.X, (int)center.Y,
				(int)basePoint.X, (int)basePoint.Y,
				(byte)(_baseCharge > ActivationThreshold ? 5 : 3)
			);

			// Draw the collector-emitter line
			rc.RenderLine(
				(int)collectorPoint.X, (int)collectorPoint.Y,
				(int)emitterPoint.X, (int)emitterPoint.Y,
				(byte)(_isActivated ? 5 : 3)
			);

			// Draw arrow from collector to emitter
			Vector2 arrowMid = (collectorPoint + emitterPoint) / 2;
			Vector2 arrowDir = Vector2.Normalize(emitterPoint - collectorPoint);

			// Calculate arrow head points
			Vector2 arrowPerp = new Vector2(-arrowDir.Y, arrowDir.X) * symbolSize / 8;
			Vector2 arrowHead1 = arrowMid - arrowDir * symbolSize / 8 + arrowPerp;
			Vector2 arrowHead2 = arrowMid - arrowDir * symbolSize / 8 - arrowPerp;

			// Draw arrow head
			rc.RenderLine(
				(int)arrowMid.X, (int)arrowMid.Y,
				(int)arrowHead1.X, (int)arrowHead1.Y,
				(byte)(_isActivated ? 5 : 3)
			);
			rc.RenderLine(
				(int)arrowMid.X, (int)arrowMid.Y,
				(int)arrowHead2.X, (int)arrowHead2.Y,
				(byte)(_isActivated ? 5 : 3)
			);

			// Draw terminal indicators
			int dotSize = tileSize / 10;

			// Collector terminal (red)
			rc.RenderFilledCircle(
				(int)collectorPoint.X, (int)collectorPoint.Y,
				dotSize,
				new RadialColor(5, 0, 0).Index
			);

			// Base terminal (green)
			rc.RenderFilledCircle(
				(int)basePoint.X, (int)basePoint.Y,
				dotSize,
				new RadialColor(0, 5, 0).Index
			);

			// Emitter terminal (blue)
			rc.RenderFilledCircle(
				(int)emitterPoint.X, (int)emitterPoint.Y,
				dotSize,
				new RadialColor(0, 0, 5).Index
			);

			// Draw activation indicator at center
			if (_isActivated)
			{
				rc.RenderFilledCircle(
					(int)center.X, (int)center.Y,
					dotSize,
					new RadialColor(5, 5, 0).Index
				);
			}

			// Draw charge values for debugging
			// TODO: Need a decent way to render text?
			// if (_debug)
			// {
			//     string collectorText = $"C:{_collectorCharge:F2}";
			//     string baseText = $"B:{_baseCharge:F2}";
			//     string emitterText = $"E:{_emitterCharge:F2}";

			//     rc.RenderText(
			//         (int)screenPos.X + 2,
			//         (int)screenPos.Y + 2,
			//         collectorText,
			//         new RadialColor(5, 0, 0)
			//     );

			//     rc.RenderText(
			//         (int)screenPos.X + 2,
			//         (int)screenPos.Y + 12,
			//         baseText,
			//         new RadialColor(0, 5, 0)
			//     );

			//     rc.RenderText(
			//         (int)screenPos.X + 2,
			//         (int)screenPos.Y + 22,
			//         emitterText,
			//         new RadialColor(0, 0, 5)
			//     );
			// }
		}

		public override bool SetCharge(float newCharge)
		{
			// Transistors manage their own charge distribution
			return false;
		}

		#region IInteractiveComponent Implementation

		public bool HandleClick(Vector2 worldPosition)
		{
			// Rotate the transistor when clicked
			Rotate();
			return true;
		}

		public bool HandlePress(Vector2 worldPosition)
		{
			// Transistor only cares about clicks for rotation
			return false;
		}

		public bool HandleRelease(Vector2 worldPosition)
		{
			// Transistor only cares about clicks for rotation
			return false;
		}

		#endregion
	}
}