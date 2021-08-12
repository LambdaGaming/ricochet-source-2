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

		[ClientRpc]
		private void PlayJumpSound( Entity ent )
		{
			Sound.FromEntity( "triggerjump", this );
		}
		
		public override void StartTouch( Entity ent )
		{
			base.StartTouch( ent );

			if ( !IsServer ) return;

			var ply = ent as RicochetPlayer;
			var target = FindByName( Target );
			if ( target.IsValid() && ply.IsValid() )
			{
				/* TODO: Make velocity work properly without sending player into the void
				var speed = ply.Velocity.Length;
				if ( speed < 0.1f ) return;

				float flMul = 268.3281572999747f * 1.2f;
				float startz = Velocity.z;
				ply.Velocity = ply.Velocity.WithZ( startz + flMul );
				ply.Velocity += new Vector3( 0, 0, 800.0f * 0.5f ) * Time.Delta;
				PlayJumpSound( ply );
				*/
			}
		}
	}
}
