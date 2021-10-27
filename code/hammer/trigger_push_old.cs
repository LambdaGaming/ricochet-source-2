﻿using Sandbox;

namespace Ricochet
{
	// Old GoldSrc version of trigger_push that somehow works better than the Source 2 version
	[Library( "trigger_push_old" )]
	[Hammer.AutoApplyMaterial( "materials/tools/toolstrigger.vmat" )]
	[Hammer.Solid]
	public partial class TriggerPushOld : BaseTrigger
	{
		[Property( Title = "Push once then remove" )]
		public bool PushOnce { get; set; } = false;

		[Property( Title = "Push Vector" )]
		public Vector3 PushVector { get; set; } = Vector3.Zero;

		[Property( Title = "Speed" )]
		public float Speed { get; set; } = 100;

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
