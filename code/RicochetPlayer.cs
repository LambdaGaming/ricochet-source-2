using Sandbox;
using System.Collections.Generic;

namespace Ricochet
{
	enum Powerup {
		Triple,
		Fast,
		Hard,
		Freeze
	}

	partial class RicochetPlayer : Player
	{
		public float DiscCooldown { get; set; }
		public List<Powerup> Powerups = new();

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
			Powerups.Clear();
			DiscCooldown = 0;
			base.Respawn();
		}
		
		public override void Simulate( Client cl )
		{
			base.Simulate( cl );
			SimulateActiveChild( cl, ActiveChild );
			if ( IsServer && Input.Pressed( InputButton.Attack1 ) && DiscCooldown <= Time.Now )
			{
				LaunchDisc();
				float cooldown = Powerups.Contains( Powerup.Fast ) ? 0.2f : 0.5f;
				DiscCooldown = Time.Now + cooldown;
			}
		}

		public override void OnKilled()
		{
			base.OnKilled();
			EnableDrawing = false;
		}

		public bool Alive()
		{
			return LifeState == LifeState.Alive;
		}

		public void LaunchDisc()
		{
			Disc disc = new();
			disc.Owner = this;
			disc.Spawn();
		}
		
		public bool AddPowerup( Powerup powerup )
		{
			if ( Powerups.Contains( powerup ) )
			{
				return false;
			}
			Powerups.Add( powerup );
			return true;
		}

		public bool RemovePowerup( Powerup powerup )
		{
			return Powerups.Remove( powerup );
		}
	}

	public class MinimalWalkController : WalkController
	{
		public new float WalkSpeed = 250.0f;
		public override void CheckJumpButton() {}
		public override void StayOnGround() {}

		public override float GetWishSpeed()
		{
			return DefaultSpeed;
		}
	}
}
