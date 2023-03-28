using Editor;
using Sandbox;

namespace Ricochet
{
	[Library( "trigger_discreturn" ), HammerEntity, Solid, AutoApplyMaterial( "materials/tools/toolstrigger.vmat" )]
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
				Sound.FromWorld( "discreturn", disc.Position );
				disc.ReturnToThrower();
			}
		}
	}
}
