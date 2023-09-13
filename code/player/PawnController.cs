using Sandbox;

namespace Ricochet;

public class PawnController : BaseNetworkable
{
	public Entity Pawn { get; protected set; }
	public IClient Client { get; protected set; }
	public Vector3 Position { get; set; }
	public Rotation Rotation { get; set; }
	public Vector3 Velocity { get; set; }
	public Rotation EyeRotation { get; set; }
	public Vector3 EyeLocalPosition { get; set; }
	public Vector3 BaseVelocity { get; set; }
	public Entity GroundEntity { get; set; }
	public Vector3 GroundNormal { get; set; }

	public Vector3 WishVelocity { get; set; }

	public void UpdateFromEntity( Entity entity )
	{
		Position = entity.Position;
		Rotation = entity.Rotation;
		Velocity = entity.Velocity;

		if ( entity is Player player )
		{
			EyeRotation = player.EyeRotation;
			EyeLocalPosition = player.EyeLocalPosition;
		}

		BaseVelocity = entity.BaseVelocity;
		GroundEntity = entity.GroundEntity;
		WishVelocity = entity.Velocity;
	}

	public void Finalize( Entity target )
	{
		target.Position = Position;
		target.Velocity = Velocity;
		target.Rotation = Rotation;
		target.GroundEntity = GroundEntity;
		target.BaseVelocity = BaseVelocity;

		if ( target is Player player )
		{
			player.EyeLocalPosition = EyeLocalPosition;
			player.EyeRotation = EyeRotation;
		}
	}

	public virtual void Simulate() {}

	public virtual void FrameSimulate()
	{
		Game.AssertClient();
	}
	
	public void Simulate( IClient client, Entity pawn )
	{
		Pawn = pawn;
		Client = client;
		UpdateFromEntity( pawn );
		Simulate();
		Finalize( pawn );
	}

	public void FrameSimulate( IClient client, Entity pawn )
	{
		Pawn = pawn;
		Client = client;

		UpdateFromEntity( pawn );

		FrameSimulate();

		Finalize( pawn );
	}
}
