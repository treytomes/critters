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

	public ParticlesState(IResourceManager resources, IEventBus eventBus, IRenderingContext rc)
		: base(resources, eventBus, rc)
	{
		UI.Add(new Label(null, resources, eventBus, rc, () => _sim?.GetType().Name ?? "N/A", new Vector2(0, 0), new RadialColor(5, 5, 5)));
	}

	#endregion

	#region Properties

	#endregion

	#region Methods

	public override void Load()
	{
		base.Load();
	}

	public override void Unload()
	{
		base.Unload();
	}

	public override void AcquireFocus()
	{
		base.AcquireFocus();

		EventBus.Subscribe<KeyEventArgs>(OnKey);
		EventBus.Subscribe<MouseMoveEventArgs>(OnMouseMove);
		EventBus.Subscribe<MouseButtonEventArgs>(OnMouseButton);
	}

	public override void LostFocus()
	{
		EventBus.Unsubscribe<KeyEventArgs>(OnKey);
		EventBus.Unsubscribe<MouseMoveEventArgs>(OnMouseMove);
		EventBus.Unsubscribe<MouseButtonEventArgs>(OnMouseButton);

		base.LostFocus();
	}

	private const int NUM_PARTICLES = 4096 * 8;
	public override void Render(GameTime gameTime)
	{
		RC.Clear();

		if (_sim == null)
		{
			_sim = new ParticlesSim(NUM_PARTICLES, RC.Width, RC.Height);
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
			RC.SetPixel(particleData[i].Position, color);
		}

		base.Render(gameTime);
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