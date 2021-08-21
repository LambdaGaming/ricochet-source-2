using Sandbox;
using System;

namespace Ricochet
{
	[Library( "trigger_jump" )]
	[Hammer.AutoApplyMaterial( "materials/tools/toolstrigger.vmat" )]
	[Hammer.Solid]
	public partial class TriggerJump : BaseTrigger
	{
		[Property( Title = "Target" )]
		public string Target { get; set; }
		
		// Based off of the trigger_jump entity in triggers.cpp of the Ricochet source code
		public override void StartTouch( Entity ent )
		{
			base.StartTouch( ent );

			if ( IsClient ) return;

			var ply = ent as RicochetPlayer;
			Entity target = FindByName( Target );
			if ( target.IsValid() && ply.IsValid() )
			{
				var gravity = 600.0f;
				Vector3 midpoint = ply.Position + ( target.Position - ply.Position ) * 0.5f;
				TraceResult tr = Trace.Ray( midpoint, midpoint + new Vector3( 0, 0, 128 ) ).WorldOnly().Run();
				midpoint = tr.EndPos;
				//midpoint.z -= 15;

				float distance1 = ( midpoint.z - ply.Position.z );
				float distance2 = ( midpoint.z - target.Position.z );
				float time1 = ( float ) Math.Sqrt( distance1 / ( 0.5f * gravity ) );
				float time2 = ( float ) Math.Sqrt( distance2 / ( 0.5f * gravity ) );

				if ( time1 < 0.1f ) return;

				Vector3 velocity = ( target.Position - ply.Position ) * ( time1 + time2 );
				velocity.z = gravity * time1;
				ply.Velocity += velocity; // TODO: Figure out how to stop the player controller from keeping the player on the ground
				ply.PlaySound( "triggerjump" );
			}
		}
	}
}
