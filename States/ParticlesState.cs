// States\ParticlesState.cs

using Critters.Gfx;
using Critters.Services;
using Critters.States.Particles;
using Critters.UI;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Critters.States;

class ParticlesState : GameState
{
	#region Constants

	private const int NUM_PARTICLES = 4096 * 8;
	private const float ATTRACTOR_STRENGTH = 5000f;

	#endregion

	#region Fields

	private Vector2 _mousePosition = Vector2.Zero;
	private ParticlesSim? _sim = null;
	private bool _isDragging = false;
	private Label _infoLabel;
	private FpsCounter _fpsCounter = new FpsCounter();

	#endregion

	#region Constructors

	public ParticlesState(IResourceManager resources, IRenderingContext rc)
		: base(resources, rc)
	{
		_infoLabel = new Label(resources, rc, GetInfoText, new Vector2(10, 10), new RadialColor(5, 5, 5));
		UI.Add(_infoLabel);
	}

	#endregion

	#region Methods

	private string GetInfoText()
	{
		return $"{_sim?.GetType().Name ?? "N/A"} - {_fpsCounter.Fps:F1} FPS - {NUM_PARTICLES} particles";
	}

	public override void Load()
	{
		base.Load();

		_sim = new ParticlesSim(NUM_PARTICLES, RC.Width, RC.Height);
		_sim.SetGravity(0, 100); // Downward gravity
	}

	public override void Unload()
	{
		_sim?.Dispose();
		_sim = null;
		base.Unload();
	}

	public override void Render(GameTime gameTime)
	{
		_fpsCounter.Update(gameTime);
		RC.Clear();

		if (_sim != null)
		{
			// Only get particle data when we need to render
			var particleData = _sim.GetParticleData();

			for (var i = 0; i < particleData.Length; i++)
			{
				ref readonly var particle = ref particleData[i];

				if (particle.Lifetime <= 0)
					continue;

				var c = particle.Color.Xyz * particle.Color.W;
				var color = new RadialColor(
					(byte)(c.X * 5),
					(byte)(c.Y * 5),
					(byte)(c.Z * 5)
				);

				// Option to render as rectangles based on particle size
				if (particle.Size > 1.5f)
				{
					var size = Vector2.One * particle.Size * 0.5f;
					RC.RenderFilledRect(new Box2(
						particle.Position - size,
						particle.Position + size
					), color);
				}
				else
				{
					RC.SetPixel(particle.Position, color);
				}
			}
		}

		base.Render(gameTime);
	}

	public override void Update(GameTime gameTime)
	{
		_sim?.Update(gameTime);
		base.Update(gameTime);
	}

	public override bool KeyDown(KeyboardKeyEventArgs e)
	{
		switch (e.Key)
		{
			case Keys.Escape:
				Leave();
				return true;

			case Keys.R:
				_sim?.InitializeParticles();
				return true;

			case Keys.G:
				// Toggle gravity
				if (_sim != null)
				{
					var currentGravity = _sim.GetGravity();
					if (currentGravity.Y > 0)
						_sim.SetGravity(0, 0);
					else
						_sim.SetGravity(0, 100);
				}
				return true;
		}
		return base.KeyDown(e);
	}

	public override bool MouseMove(MouseMoveEventArgs e)
	{
		_mousePosition = e.Position;
		if (_isDragging)
		{
			_sim?.SetAttractor(_mousePosition.X, _mousePosition.Y, ATTRACTOR_STRENGTH);
		}
		return base.MouseMove(e);
	}

	public override bool MouseDown(MouseButtonEventArgs e)
	{
		return OnMouseButton(e) || base.MouseDown(e);
	}

	public override bool MouseUp(MouseButtonEventArgs e)
	{
		return OnMouseButton(e) || base.MouseUp(e);
	}

	private bool OnMouseButton(MouseButtonEventArgs e)
	{
		if (e.Button == MouseButton.Left)
		{
			_isDragging = e.Action == InputAction.Press;

			if (_isDragging)
				_sim?.SetAttractor(_mousePosition.X, _mousePosition.Y, ATTRACTOR_STRENGTH);
			else
				_sim?.SetAttractor(0, 0, 0);

			return true;
		}
		else if (e.Button == MouseButton.Right && e.Action == InputAction.Press)
		{
			// Right click creates a repulsion force
			_sim?.SetAttractor(_mousePosition.X, _mousePosition.Y, -ATTRACTOR_STRENGTH);
			return true;
		}
		return false;
	}

	#endregion
}

// Simple FPS counter helper class
class FpsCounter
{
	private double _elapsed = 0;
	private int _frameCount = 0;

	public float Fps { get; private set; } = 0;

	public void Update(GameTime gameTime)
	{
		_elapsed += gameTime.ElapsedTime.TotalSeconds;
		_frameCount++;

		if (_elapsed >= 0.5) // Update twice per second
		{
			Fps = (float)(_frameCount / _elapsed);
			_elapsed = 0;
			_frameCount = 0;
		}
	}
}