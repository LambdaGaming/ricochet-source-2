using Sandbox;
using System;
using System.Threading.Tasks;

namespace Ricochet
{
	[Library( "powerup" )]
	public partial class PowerupEnt : AnimEntity
	{
		public Powerup CurrentPowerup { get; set; } = Powerup.None;
		public bool Hidden { get; set; } = false;

		public override void Spawn()
		{
			base.Spawn();
			SetupPhysicsFromModel( PhysicsMotionType.Static, false );
			CollisionGroup = CollisionGroup.Trigger;
			EnableSolidCollisions = false;
			EnableTouch = true;
			SetRandomPowerup();
		}

		public override void StartTouch( Entity ent )
		{
			if ( !IsClient ) return;
			base.StartTouch( ent );
			if ( !Hidden )
			{
				var ply = ent as RicochetPlayer;
				if ( ply.IsValid() )
				{
					ply.AddPowerup( CurrentPowerup );
					ply.PlaySound( "powerup" );
					Hide();
				}
			}
		}

		public void Hide()
		{
			RenderAlpha = 0;
			_ = WaitForRespawn();
			Hidden = true;
		}

		public void Unhide()
		{
			SetRandomPowerup();
			RenderAlpha = 1;
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
						mdl = "models/pow_fast/pow_fast.vmdl"; // TODO: Replace with freeze model
						break;
					}
				case Powerup.Hard:
					{
						mdl = "models/pow_fast/pow_fast.vmdl"; // TODO: Replace with hard model
						break;
					}
				case Powerup.Triple:
					{
						mdl = "models/pow_fast/pow_fast.vmdl"; // TODO: Replace with triple model
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
