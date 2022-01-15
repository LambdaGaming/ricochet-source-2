using Sandbox;

namespace Ricochet
{
	[Library( "trigger_discreturn" )]
	[Hammer.AutoApplyMaterial( "materials/tools/toolstrigger.vmat" )]
	[Hammer.Solid]
	public partial class TriggerDiscReturn : BaseTrigger
	{
		public override void StartTouch( Entity ent )
		{
			base.StartTouch( ent );
			var disc = ent as Disc;
			if ( disc.IsValid() )
			{
				var spr = Particles.Create( "particles/discreturn.vpcf", disc.Position );
				spr.Destroy();
				var snd = Sound.FromWorld( "discreturn", disc.Position );
				snd.SetVolume( 0.25f );
				disc.ReturnToThrower();
			}
		}
	}
}
