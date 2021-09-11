using Sandbox;
using System;

namespace Ricochet
{
	[Flags]
	public enum Powerup {
		Triple = 1,
		Fast = 2,
		Hard = 4,
		Freeze = 8
	}

	public partial class RicochetPlayer : Player
	{
		public float DiscCooldown { get; set; }
		public float OwnerTouchCooldown { get; set; }
		public float EnemyTouchCooldown { get; set; }
		public float FreezeTimer { get; set; }
		public int NumDiscs { get; set; }
		public int Team { get; set; } = 0;
		public Color TeamColor { get; set; }
		public bool Frozen { get; set; }
		public RicochetPlayer LastPlayerToHitMe { get; set; }
		public Powerup PowerupFlags { get; set; }
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
			PowerupFlags = 0;
			DiscCooldown = 0;
			OwnerTouchCooldown = 0;
			EnemyTouchCooldown = 0;
			FreezeTimer = 0;
			NumDiscs = MaxDiscs;
			Team = Team == 0 ? AutoAssignTeam() : Team;
			TeamColor = GetTeamColor();
			Frozen = false;
			using ( Prediction.Off() )
			{
				PlaySound( "r_tele1" );
			}
		}
		
		public override void Simulate( Client cl )
		{
			base.Simulate( cl );
			SimulateActiveChild( cl, ActiveChild );
			if ( IsServer && DiscCooldown < Time.Now && Alive() )
			{
				if ( Input.Pressed( InputButton.Attack1 ) )
				{
					if ( NumDiscs > 0 )
					{
						LaunchDisc();
						float cooldown = HasPowerup( Powerup.Fast ) ? 0.2f : 0.5f;
						DiscCooldown = Time.Now + cooldown;
						OwnerTouchCooldown = Time.Now + 0.1f;
						RemoveDisc( 1 );
						using ( Prediction.Off() )
						{
							PlaySound( "cbar_miss1" );
						}
					}
				}
				else if ( Input.Pressed( InputButton.Attack2 ) )
				{
					if ( NumDiscs == MaxDiscs )
					{
						AddPowerup( Powerup.Hard );
						LaunchDisc();
						float cooldown = HasPowerup( Powerup.Fast ) ? 0.2f : 0.5f;
						DiscCooldown = Time.Now + cooldown;
						OwnerTouchCooldown = Time.Now + 0.1f;
						RemovePowerup( Powerup.Hard );
						RemoveDisc( MaxDiscs );
						using ( Prediction.Off() )
						{
							PlaySound( "altfire" );
						}
					}
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
		
		public void AddPowerup( Powerup powerup )
		{
			PowerupFlags |= powerup;
		}

		public void RemovePowerup( Powerup powerup )
		{
			PowerupFlags &= ~powerup;
		}

		public bool HasPowerup( Powerup powerup )
		{
			return PowerupFlags.HasFlag( powerup );
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
			RenderColorAndAlpha = new Color( 0, 0, 200, 230 );
			( Controller as MinimalWalkController ).WalkSpeed = FreezeSpeed;
			Frozen = true;
			FreezeTimer = Time.Now + FreezeTime;
		}

		public void ClearFreeze()
		{
			var walk = Controller as MinimalWalkController;
			// TODO: Glowing render effect
			RenderColorAndAlpha = Color.White;
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
				using ( Prediction.Off() )
				{
					PlaySound( "decap" );
				}
				SetBodyGroup( 1, 1 );
				SpawnHeadModel();
			}
		}

		public void Shatter( RicochetPlayer killer )
		{
			if ( Alive() )
			{
				LastPlayerToHitMe = killer;
				DamageInfo dmg = new() { Damage = 500 };
				TakeDamage( dmg );
				RenderAlpha = 1;
				EnableSolidCollisions = false;
				using ( Prediction.Off() )
				{
					PlaySound( "shatter" );
				}
				SpawnHeadModel();
			}
		}

		public Color GetTeamColor()
		{
			if ( Team < 0 || Team > Ricochet.TeamColors.Length )
			{
				return Color.White;
			}
			return Color.FromBytes( Ricochet.TeamColors[Team, 0], Ricochet.TeamColors[Team, 1], Ricochet.TeamColors[Team, 2] );
		}

		public int GetClientIndex()
		{
			return Ricochet.TotalClients.IndexOf( this );
		}

		public int AutoAssignTeam()
		{
			if ( Ricochet.IsTDM )
			{
				int lowestTeam = 0;
				int lowestAmount = 0;
				for ( int i = 0; i < Ricochet.TotalTeams.Length; i++ )
				{
					if ( Ricochet.TotalTeams[i] == 0 )
					{
						Ricochet.TotalTeams[i]++;
						return i;
					}

					int amount = Ricochet.TotalTeams[i];
					if ( amount < lowestAmount )
					{
						lowestTeam = i;
					}
				}
				return lowestTeam;
			}
			return GetClientIndex();
		}

		public void SpawnHeadModel()
		{
			ModelEntity head = new();
			head.SetModel( "models/citizen/citizen.vmdl" );
			head.SetBodyGroup( 2, 1 );
			head.SetBodyGroup( 3, 1 );
			head.SetBodyGroup( 4, 1 );
			head.SetBodyGroup( 5, 1 );
			head.Position = Position;
			head.SetupPhysicsFromModel( PhysicsMotionType.Dynamic, false );
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

		public override void Simulate()
		{
			base.Simulate();
			if ( NearJumpPad() )
			{
				GroundEntity = null;
			}
		}

		private bool NearJumpPad()
		{
			float currentdist = int.MaxValue;
			foreach ( Entity ent in Entity.All )
			{
				if ( ent is TriggerJump )
				{
					float dist = Vector3.DistanceBetween( ent.Position, Position );
					if ( dist < currentdist )
					{
						currentdist = dist;
					}
				}
			}
			return currentdist < 30;
		}
	}
}
