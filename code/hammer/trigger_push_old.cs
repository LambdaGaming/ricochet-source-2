using Sandbox;

namespace Ricochet
{
	// Old GoldSrc version of trigger_push that somehow works better than the Source 2 version
	[Library( "trigger_push_old" )]
	[Hammer.AutoApplyMaterial( "materials/tools/toolstrigger.vmat" )]
	[Hammer.Solid]
	public partial class TriggerPushOld : BaseTrigger
	{
		[Property( Title = "Push once then remove" )]
		public bool PushOnce { get; set; }

		[Property( Title = "Push Vector" )]
		public Vector3 PushVector { get; set; }

		[Property( Title = "Speed" )]
		public float Speed { get; set; }

		public override void Spawn()
		{
			base.Spawn();
			if ( Speed == 0 )
			{
				Speed = 100;
			}
		}

		public override void StartTouch( Entity ent )
		{
			ent.Velocity += Speed * PushVector;
			if ( PushOnce )
			{
				Delete();
			}
		}
	}
}
