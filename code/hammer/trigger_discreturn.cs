using Sandbox;

namespace Ricochet
{
	[Library( "trigger_discreturn" )]
	[Editor.AutoApplyMaterial( "materials/tools/toolstrigger.vmat" )]
	[Editor.Solid]
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
