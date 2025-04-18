using Critters.Gfx;
using Critters.IO;
using Critters.Services;
using Critters.States.Particles;
using Critters.UI;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Critters.States;

class ParticlesState : GameState
{
	#region Fields

	private Vector2 _mousePosition = Vector2.Zero;
	private ParticlesSim? _sim = null;
	bool _isDragging = false;

	#endregion

	#region Constructors

	public ParticlesState()
		: base()
	{
		UI.Add(new Label(() => _sim?.GetType().Name ?? "N/A", new Vector2(0, 0), new RadialColor(5, 5, 5)));
	}

	#endregion

	#region Properties

	#endregion

	#region Methods

	public override void Load(IResourceManager resources, IEventBus eventBus)
	{
		base.Load(resources, eventBus);
	}

	public override void Unload(IResourceManager resources, IEventBus eventBus)
	{
		base.Unload(resources, eventBus);
	}

	public override void AcquireFocus(IEventBus eventBus)
	{
		base.AcquireFocus(eventBus);

		eventBus.Subscribe<KeyEventArgs>(OnKey);
		eventBus.Subscribe<MouseMoveEventArgs>(OnMouseMove);
		eventBus.Subscribe<MouseButtonEventArgs>(OnMouseButton);
	}

	public override void LostFocus(IEventBus eventBus)
	{
		eventBus.Unsubscribe<KeyEventArgs>(OnKey);
		eventBus.Unsubscribe<MouseMoveEventArgs>(OnMouseMove);
		eventBus.Unsubscribe<MouseButtonEventArgs>(OnMouseButton);

		base.LostFocus(eventBus);
	}

	private const int NUM_PARTICLES = 4096 * 8;
	public override void Render(RenderingContext rc, GameTime gameTime)
	{
		rc.Clear();

		if (_sim == null)
		{
			_sim = new ParticlesSim(NUM_PARTICLES, rc.Width, rc.Height);
			_sim.SetGravity(0, 100); // Downward gravity
		}

		var particleData = _sim.GetParticleData();
		for (var i = 0; i < NUM_PARTICLES; i++)
		{
			if (particleData[i].Lifetime <= 0)
			{
				_sim.ResetParticle(i);
				continue;
			}

			var c = particleData[i].Color.Xyz * particleData[i].Color.W;
			var color = new RadialColor((byte)(c.X * 5), (byte)(c.Y * 5), (byte)(c.Z * 5));
			// var size = Vector2.One * particleData[i].Size;

			// rc.RenderFilledRect(new Box2(particleData[i].Position - size, particleData[i].Position + size), color);
			rc.SetPixel(particleData[i].Position, color);
		}

		base.Render(rc, gameTime);
	}

	public override void Update(GameTime gameTime)
	{
		_sim?.Update(gameTime);
		base.Update(gameTime);
	}

	private void OnKey(KeyEventArgs e)
	{
		if (e.IsPressed)
		{
			switch (e.Key)
			{
				case Keys.Escape:
					Leave();
					break;
			}
		}
	}

	private void OnMouseMove(MouseMoveEventArgs e)
	{
		_mousePosition = e.Position;
		if (_isDragging)
		{
			_sim?.SetAttractor(_mousePosition.X, _mousePosition.Y, 5000);
		}
	}

	private void OnMouseButton(MouseButtonEventArgs e)
	{
		if (e.Button == MouseButton.Left)
		{
			if (e.Action == InputAction.Press)
			{
				_isDragging = true;
				_sim?.SetAttractor(_mousePosition.X, _mousePosition.Y, 5000);
			}
			else
			{
				_sim?.SetAttractor(0, 0, 0);
			}
		}
	}

	#endregion
}