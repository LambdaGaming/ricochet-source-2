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
					DamageInfo dmg = new() { Damage = 1000, Attacker = ply.LastAttacker };
					ply.TakeDamage( dmg );
					ply.PlaySound( "scream" );
				}
			}
		}
	}
}
