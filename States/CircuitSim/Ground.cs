using Critters.Gfx;
using OpenTK.Mathematics;

namespace Critters.States.CircuitSim;

/// <summary>
/// Ground component that absorbs electricity
/// </summary>
class Ground : CircuitComponent
{
	/// <summary>
	/// How quickly the ground absorbs charge
	/// </summary>
	/// <remarks>
	/// Reduced from 0.8 to 0.4.
	/// </remarks>
	public float AbsorptionRate { get; set; } = 0.4f;

	public Ground()
	{
		MaxCharge = 0.1f; // Can hold a small charge before dissipating
	}

	public override void Update(CircuitSimulator simulator, int x, int y, float deltaTime)
	{
		// Ground dissipates charge quickly
		if (Charge > 0)
		{
			// SetCharge(0.0f);
			var dissipation = Charge * AbsorptionRate * deltaTime * 3.0f; // Not sure if this 3.0f factor is needed.
			SetCharge(Charge - dissipation);
			IsDirty = true;
		}
	}

	public override void Render(IRenderingContext rc, Vector2 screenPos, int tileSize)
	{
		// Ground background
		rc.RenderFilledRect(
			new Box2(screenPos, screenPos + new Vector2(tileSize)),
			new RadialColor(1, 1, 1)
		);

		// Ground border
		rc.RenderRect(
			new Box2(screenPos, screenPos + new Vector2(tileSize)),
			new RadialColor(3, 3, 3)
		);

		// Ground symbol (multiple horizontal lines)
		int symbolWidth = tileSize * 3 / 4;
		int lineSpacing = tileSize / 6;
		Vector2 center = screenPos + new Vector2(tileSize / 2);

		for (int i = 0; i < 3; i++)
		{
			int lineLength = symbolWidth - (i * tileSize / 6);
			int yPos = (int)(center.Y + i * lineSpacing);

			rc.RenderHLine(
				(int)(center.X - lineLength / 2),
				(int)(center.X + lineLength / 2),
				yPos,
				(byte)5
			);
		}

		// Show charge level if any
		if (Charge > 0.01f)
		{
			byte chargeIntensity = (byte)Math.Min(5, Math.Ceiling(Charge * 10));
			rc.RenderFilledCircle(
				(int)center.X,
				(int)center.Y - lineSpacing,
				tileSize / 8,
				new RadialColor(chargeIntensity, 0, 0).Index
			);
		}
	}
}
