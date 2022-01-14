using Sandbox;

namespace Ricochet
{
	[Library( "trigger_fall" )]
	[Hammer.AutoApplyMaterial( "materials/tools/toolstrigger.vmat" )]
	[Hammer.Solid]
	public partial class TriggerFall : BaseTrigger
	{
		public override void StartTouch( Entity ent )
		{
			base.StartTouch( ent );
			if ( ent.IsValid() )
			{
				var ply = ent as RicochetPlayer;
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

					RicochetCorpse body = new();
					body.Position = ply.Position;
					body.Velocity = ply.Velocity;
					ply.Corpse = body;
					ply.SyncCorpse( body );

					DamageInfo dmg = new() {
						Damage = 1000,
						Attacker = ply.LastAttacker,
						Weapon = ply.LastAttackerWeapon
					};
					ply.TakeDamage( dmg );
					ply.PlaySound( "scream" );
				}
			}
		}
	}
}
