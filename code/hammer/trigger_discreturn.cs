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
				// TODO: Play return sound and sprite
				disc.ReturnToThrower();
			}
		}
	}
}
