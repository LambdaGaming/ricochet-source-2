using Editor;
using Sandbox;

namespace Ricochet;

[Library( "trigger_fall" ), HammerEntity, Solid, AutoApplyMaterial( "materials/tools/toolstrigger.vmat" )]
public partial class TriggerFall : BaseTrigger
{
	public override void StartTouch( Entity ent )
	{
		base.StartTouch( ent );
		if ( ent.IsValid() )
		{
			var ply = ent as Player;
			if ( ply.IsValid() && ply.Alive() )
			{
				if ( Ricochet.CurrentRound.CurrentState == RoundState.Waiting )
				{
					ply.Respawn();
					return;
				}
				else if ( Ricochet.CurrentRound.CurrentState == RoundState.End )
				{
					return;
				}

				if ( ply.LastAttackWeaponBounces <= 0 && ply.LastAttacker == null )
				{
					ply.LastDeathReason = DeathReason.Fall;
				}
				else
				{
					ply.LastDeathReason = DeathReason.Disc;
				}

				PlayerCorpse body = new();
				body.Position = ply.Position;
				body.Velocity = ply.Velocity.WithX( 0 ).WithY( 0 );
				body.Rotation = Rotation.FromPitch( 90 );
				ply.Corpse = body;
				ply.CorpsePosition = body.Position + Vector3.Down * 100;

				DamageInfo dmg = new() {
					Damage = 1000,
					Attacker = ply.LastAttacker,
					Weapon = ply.LastAttackerWeapon
				};
				ply.TakeDamage( dmg );
				Sound.FromWorld( "scream", ply.CorpsePosition + Vector3.Down * 2 );
			}
		}
	}
}
