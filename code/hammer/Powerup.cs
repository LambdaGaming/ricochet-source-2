using Sandbox;
using System;
using System.Threading.Tasks;

namespace Ricochet
{
	[Library( "powerup" )]
	public partial class PowerupEnt : AnimatedEntity
	{
		[Net] public Powerup CurrentPowerup { get; set; } = 0;
		public bool Hidden { get; set; } = false;

		public override void Spawn()
		{
			base.Spawn();
			SetupPhysicsFromModel( PhysicsMotionType.Static, false );
			Tags.Add( "trigger" );
			EnableSolidCollisions = false;
			EnableTouch = true;
			SetRandomPowerup();
		}

		public override void StartTouch( Entity ent )
		{
			var ply = ent as RicochetPlayer;
			if ( IsServer )
			{
				base.StartTouch( ent );
				if ( !Hidden )
				{
					if ( ply.IsValid() )
					{
						ply.AddPowerup( CurrentPowerup );
						ply.PlaySound( "powerup" );
						Hide();
					}
				}
			}
		}

		public void Hide()
		{
			RenderColor = Color.Transparent;
			_ = WaitForRespawn();
			Hidden = true;
		}

		public void Unhide()
		{
			SetRandomPowerup();
			RenderColor = Color.White;
			PlaySound( "pspawn" );
			Hidden = false;
		}

		async Task WaitForRespawn()
		{
			await Task.DelaySeconds( 10 );
			Unhide();
		}

		public void SetRandomPowerup()
		{
			Random rand = new();
			Array powerups = Enum.GetValues( typeof( Powerup ) );
			Powerup powerup = ( Powerup ) powerups.GetValue( rand.Next( powerups.Length ) );
			CurrentPowerup = powerup;
			SetPowerupModel();
		}

		public void SetPowerupModel()
		{
			string mdl;
			switch ( CurrentPowerup )
			{
				case Powerup.Fast:
				{
					mdl = "models/pow_fast/pow_fast.vmdl";
					break;
				}
				case Powerup.Freeze:
				{
					mdl = "models/pow_freeze/pow_freeze.vmdl";
					break;
				}
				case Powerup.Hard:
				{
					mdl = "models/pow_hard/pow_hard.vmdl";
					break;
				}
				case Powerup.Triple:
				{
					mdl = "models/pow_triple/pow_triple.vmdl";
					break;
				}
				default:
				{
					mdl = "models/pow_fast/pow_fast.vmdl";
					break;
				}
			}
			SetModel( mdl );
		}
	}
}
