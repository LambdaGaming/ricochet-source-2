using Sandbox;
using System.Threading.Tasks;

namespace Ricochet
{
	[Library( "powerup" )]
	[Hammer.Model]
	public partial class PowerupEnt : ModelEntity
	{
		[Property( Title = "Powerup Type" )]
		public Powerup SetPowerup { get; set; } = Powerup.None;

		public bool Hidden { get; set; } = false;

		public override void StartTouch( Entity ent )
		{
			base.StartTouch( ent );
			if ( !Hidden )
			{
				var ply = ent as RicochetPlayer;
				if ( ply.IsValid() )
				{
					ToggleHide();
				}
			}
		}

		public void ToggleHide()
		{
			Hidden = !Hidden;
			if ( Hidden )
			{
				RenderColorAndAlpha = Color32.White;
				PlaySound( "pspawn" );
			}
			else
			{
				RenderColorAndAlpha = Color32.Transparent;
				_ = WaitForRespawn();
			}
		}

		async Task WaitForRespawn()
		{
			await Task.DelaySeconds( 10 );
			ToggleHide();
		}
	}
}
