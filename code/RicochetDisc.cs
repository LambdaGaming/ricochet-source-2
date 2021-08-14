using Sandbox;

namespace Ricochet
{
	class Disc : ModelEntity
	{
		public new void Spawn()
		{
			base.Spawn();
			SetModel( "models/light_arrow.vmdl" ); // Temporary model until the real one gets ported over
			Vector3 vel = Owner.EyeRot.Forward * 1000;
			Position = Owner.Position + Owner.EyeRot.Forward * 100;
			SetupPhysicsFromModel( PhysicsMotionType.Dynamic, false );
			PhysicsGroup.Velocity = vel; // TODO: Make it so the disc isn't affected by gravity and doesn't travel along the z-axis
		}
		
		public override void StartTouch( Entity ent )
		{
			var ply = ent as RicochetPlayer;
			if ( ply.IsValid() && ply == Owner ) return;
			Delete();
		}
	}
}
