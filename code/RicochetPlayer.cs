using Sandbox;

namespace Ricochet
{
	partial class RicochetPlayer : Player
	{
		public int JumpCooldown;

		public override void Respawn()
		{
			SetModel( "models/citizen/citizen.vmdl" );
			Controller = new MinimalWalkController();
			Animator = new StandardPlayerAnimator();
			Camera = new FirstPersonCamera();
			EnableAllCollisions = true;
			EnableDrawing = true;
			EnableHideInFirstPerson = true;
			EnableShadowInFirstPerson = true;
			JumpCooldown = 0;
			base.Respawn();
		}

		public override void Simulate( Client cl )
		{
			base.Simulate( cl );
			SimulateActiveChild( cl, ActiveChild );
		}

		public override void OnKilled()
		{
			base.OnKilled();
			EnableDrawing = false;
		}
	}

	public class MinimalWalkController : WalkController
	{
		public override float GetWishSpeed()
		{
			return DefaultSpeed;
		}

		public override void CheckJumpButton() {}
	}
}
