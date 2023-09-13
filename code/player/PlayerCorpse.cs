using Sandbox;

namespace Ricochet;
public class PlayerCorpse : ModelEntity
{
	public override void Spawn()
	{
		base.Spawn();
		SetModel( "models/citizen/citizen.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Dynamic );
		Tags.Add( "ragdoll", "debris" );
	}

	public void SetHead()
	{
		for ( int i = 1; i < 6; i++ )
		{
			SetBodyGroup( i, 1 );
		}
		Velocity += Velocity.WithZ( 1000 );
	}

	public void SetBody()
	{
		SetBodyGroup( 0, 1 );
	}

	[Event( "PlayerRespawn" )]
	private void OnPlayerRespawn()
	{
		Delete();
	}
}
