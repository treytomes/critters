using Critters.Gfx;
using Critters.World;
using OpenTK.Mathematics;

namespace Critters.States.CircuitSim
{
	/// <summary>
	/// Cursor for placing and selecting circuit components
	/// </summary>
	class CircuitMapCursor
	{
		private Vector2 _position;
		private float _pulseTime = 0;
		private const float PULSE_RATE = 2.0f;

		/// <summary>
		/// Current position of the cursor in world coordinates
		/// </summary>
		public Vector2 Position => _position;

		/// <summary>
		/// Currently selected component type for placement
		/// </summary>
		public Type SelectedComponentType { get; set; } = typeof(Wire);

		/// <summary>
		/// Creates a new circuit map cursor
		/// </summary>
		public CircuitMapCursor()
		{
			_position = Vector2.Zero;
		}

		/// <summary>
		/// Moves the cursor to a new position
		/// </summary>
		/// <param name="position">New position in world coordinates</param>
		public void MoveTo(Vector2 position)
		{
			_position = position;
		}

		/// <summary>
		/// Renders the cursor
		/// </summary>
		/// <param name="rc">Rendering context</param>
		/// <param name="gameTime">Current game time</param>
		/// <param name="camera">Camera for viewport transformation</param>
		public void Render(IRenderingContext rc, GameTime gameTime, Camera camera)
		{
			// Update pulse animation
			_pulseTime += (float)gameTime.ElapsedTime.TotalSeconds * PULSE_RATE;
			if (_pulseTime > 1.0f)
				_pulseTime -= 1.0f;

			// Calculate pulse intensity
			byte pulseIntensity = (byte)(2 + Math.Sin(_pulseTime * Math.PI * 2) * 3);

			// Convert world position to screen position
			Vector2 screenPos = camera.WorldToScreen(_position);

			// Determine cursor color based on selected component
			RadialColor cursorColor;
			if (SelectedComponentType == typeof(PowerSource))
				cursorColor = new RadialColor(5, pulseIntensity, 0);
			else if (SelectedComponentType == typeof(Wire))
				cursorColor = new RadialColor(0, pulseIntensity, 5);
			else if (SelectedComponentType == typeof(Ground))
				cursorColor = new RadialColor(pulseIntensity, pulseIntensity, pulseIntensity);
			else if (SelectedComponentType == typeof(Switch))
				cursorColor = new RadialColor(0, pulseIntensity, 5);
			else if (SelectedComponentType == typeof(Button))
				cursorColor = new RadialColor(5, 0, pulseIntensity);
			else
				cursorColor = new RadialColor(5, 0, 5);

			// Draw cursor rectangle
			int tileSize = CircuitSimGameState.TILE_SIZE;
			rc.RenderRect(
				new Box2(screenPos, screenPos + new Vector2(tileSize)),
				cursorColor
			);

			// Draw corner markers
			int markerSize = 3;
			byte markerColor = 5;

			// Top-left
			rc.RenderHLine((int)screenPos.X, (int)screenPos.X + markerSize, (int)screenPos.Y, markerColor);
			rc.RenderVLine((int)screenPos.X, (int)screenPos.Y, (int)screenPos.Y + markerSize, markerColor);

			// Top-right
			rc.RenderHLine((int)screenPos.X + tileSize - markerSize, (int)screenPos.X + tileSize, (int)screenPos.Y, markerColor);
			rc.RenderVLine((int)screenPos.X + tileSize, (int)screenPos.Y, (int)screenPos.Y + markerSize, markerColor);

			// Bottom-left
			rc.RenderHLine((int)screenPos.X, (int)screenPos.X + markerSize, (int)screenPos.Y + tileSize, markerColor);
			rc.RenderVLine((int)screenPos.X, (int)screenPos.Y + tileSize - markerSize, (int)screenPos.Y + tileSize, markerColor);

			// Bottom-right
			rc.RenderHLine((int)screenPos.X + tileSize - markerSize, (int)screenPos.X + tileSize, (int)screenPos.Y + tileSize, markerColor);
			rc.RenderVLine((int)screenPos.X + tileSize, (int)screenPos.Y + tileSize - markerSize, (int)screenPos.Y + tileSize, markerColor);
		}

		/// <summary>
		/// Creates a new component of the selected type
		/// </summary>
		/// <returns>A new component instance</returns>
		public CircuitComponent CreateSelectedComponent()
		{
			if (SelectedComponentType == typeof(PowerSource))
				return new PowerSource();
			else if (SelectedComponentType == typeof(Wire))
				return new Wire();
			else if (SelectedComponentType == typeof(Ground))
				return new Ground();
			else if (SelectedComponentType == typeof(Switch))
				return new Switch();
			else if (SelectedComponentType == typeof(Button))
				return new Button();
			else
				return new Wire(); // Default to wire
		}

		/// <summary>
		/// Cycles to the next component type
		/// </summary>
		public void CycleComponentType()
		{
			if (SelectedComponentType == typeof(Wire))
				SelectedComponentType = typeof(PowerSource);
			else if (SelectedComponentType == typeof(PowerSource))
				SelectedComponentType = typeof(Ground);
			else if (SelectedComponentType == typeof(Ground))
				SelectedComponentType = typeof(Switch);
			else if (SelectedComponentType == typeof(Switch))
				SelectedComponentType = typeof(Button);
			else
				SelectedComponentType = typeof(Wire);
		}
	}
}