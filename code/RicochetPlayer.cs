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
		public float OwnerTouchCooldown { get; set; }
		public float EnemyTouchCooldown { get; set; }
		public float FreezeTimer { get; set; }
		public int NumDiscs { get; set; }
		public int Team { get; set; }
		public bool Frozen { get; set; }
		public RicochetPlayer LastPlayerToHitMe { get; set; }
		public List<Powerup> Powerups = new();
		public static readonly int MaxDiscs = 3;
		public static readonly int FreezeSpeed = 50;
		public static readonly int FreezeTime = 7;

		public override void Respawn()
		{
			base.Respawn();
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
			OwnerTouchCooldown = 0;
			EnemyTouchCooldown = 0;
			FreezeTimer = 0;
			NumDiscs = MaxDiscs;
			Team = 0;
			Frozen = false;
		}
		
		public override void Simulate( Client cl )
		{
			base.Simulate( cl );
			SimulateActiveChild( cl, ActiveChild );
			if ( IsServer && DiscCooldown < Time.Now && NumDiscs > 0 )
			{
				if ( Input.Pressed( InputButton.Attack1 ) )
				{
					LaunchDisc();
					float cooldown = Powerups.Contains( Powerup.Fast ) ? 0.2f : 0.5f;
					DiscCooldown = Time.Now + cooldown;
					RemoveDisc( 1 );
				}
				else if ( Input.Pressed( InputButton.Attack2 ) )
				{
					AddPowerup( Powerup.Hard );
					LaunchDisc();
					float cooldown = Powerups.Contains( Powerup.Fast ) ? 0.2f : 0.5f;
					DiscCooldown = Time.Now + cooldown;
					RemovePowerup( Powerup.Hard );
					RemoveDisc( MaxDiscs );
				}
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

		public bool HasPowerup( Powerup powerup )
		{
			if ( Powerups.Contains( powerup ) )
			{
				return true;
			}
			return false;
		}

		public void GiveDisc( int num )
		{
			NumDiscs = ( int ) MathX.Clamp( NumDiscs += num, 0, MaxDiscs );
		}

		public void RemoveDisc( int num )
		{
			NumDiscs = ( int ) MathX.Clamp( NumDiscs -= num, 0, MaxDiscs );
		}

		public void Freeze()
		{
			// TODO: Glowing render effect
			RenderColorAndAlpha = new Color32( 0, 0, 200, 230 );
			( Controller as MinimalWalkController ).WalkSpeed = FreezeSpeed;
			Frozen = true;
			FreezeTimer = Time.Now + FreezeTime;
		}

		public void ClearFreeze()
		{
			var walk = Controller as MinimalWalkController;
			// TODO: Glowing render effect
			RenderColorAndAlpha = Color32.White;
			walk.WalkSpeed = walk.DefaultSpeed;
			Frozen = false;
		}

		public void Decapitate( RicochetPlayer killer )
		{
			if ( Frozen )
			{
				Shatter( killer );
				return;
			}

			if ( Alive() )
			{
				LastPlayerToHitMe = killer;
				DamageInfo dmg = new() { Damage = 500 };
				TakeDamage( dmg );
				// TODO: Emit decap sound, change bodygroup of playermodel, and spawn head model
			}
		}

		public void Shatter( RicochetPlayer killer )
		{
			if ( Alive() )
			{
				LastPlayerToHitMe = killer;
				DamageInfo dmg = new() { Damage = 500 };
				TakeDamage( dmg );
				// TODO: Emit shatter sound
				RenderAlpha = 1;
				EnableSolidCollisions = false;
				// TODO: Spawn head model
			}
		}
	}

	public class MinimalWalkController : WalkController
	{
		public new float WalkSpeed = 250.0f;
		public new float DefaultSpeed = 250.0f;
		public override void CheckJumpButton() {}
		public override void StayOnGround() {}

		public override float GetWishSpeed()
		{
			return DefaultSpeed;
		}
	}
}
