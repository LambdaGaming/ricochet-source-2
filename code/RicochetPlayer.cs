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
		public int PowerupDiscs { get; set; }
		public int Team { get; set; } = 0;
		public Color TeamColor { get; set; }
		public bool Frozen { get; set; }
		public Powerup PowerupFlags { get; set; }
		public bool AllowedToFire { get; set; } = true;
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
			PowerupDiscs = 0;
			Team = Team == 0 ? AutoAssignTeam() : Team;
			TeamColor = GetTeamColor();
			Frozen = false;
			RenderColor = Color.White;
			using ( Prediction.Off() )
			{
				PlaySound( "r_tele1" );
			}
			Event.Run( "PlayerRespawn" );
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
						FireDisc();
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
						FireDisc( true );
						RemoveDisc( MaxDiscs );
						using ( Prediction.Off() )
						{
							PlaySound( "altfire" );
						}
					}
				}
			}
			if ( Frozen && FreezeTimer < Time.Now )
			{
				ClearFreeze();
			}
		}

		[ClientRpc]
		private void SyncClientData( int discs, int powerups )
		{
			NumDiscs = discs;
			PowerupFlags = ( Powerup ) powerups;
			Event.Run( "SyncClientData" );
		}

		public Disc FireDisc( bool decap = false )
		{
			Angles firedir = Angles.Zero;
			firedir.yaw = EyeRot.y;
			Vector3 vecsrc = Position + ( ( EyeRot.Forward.WithZ( 0 ) * 25 ) + ( Rotation.Up * 35 ) );
			Disc disc = Disc.CreateDisc( vecsrc, firedir, this, decap, PowerupFlags );
			Disc returndisc = disc;

			if ( HasPowerup( Powerup.Triple ) )
			{
				firedir.yaw = EyeRot.y - 7;
				disc = Disc.CreateDisc( vecsrc, firedir, this, decap, Powerup.Triple );
				disc.IsExtra = true;

				firedir.yaw = EyeRot.y + 7;
				disc = Disc.CreateDisc( vecsrc, firedir, this, decap, Powerup.Triple );
				disc.IsExtra = true;
			}

			PowerupDiscs--;
			if ( PowerupDiscs <= 0 )
			{
				RemoveAllPowerups();
			}

			DiscCooldown = Time.Now + ( HasPowerup( Powerup.Fast ) ? 0.2f : 0.5f );
			OwnerTouchCooldown = Time.Now + 0.1f;
			return returndisc;
		}
		
		public override void OnKilled()
		{
			base.OnKilled();
			EnableAllCollisions = false;
			EnableDrawing = false;
		}

		public bool Alive()
		{
			return LifeState == LifeState.Alive;
		}

		public void AddPowerup( Powerup powerup )
		{
			PowerupFlags |= powerup;
			PowerupDiscs = MaxDiscs;
			SyncClientData( NumDiscs, ( int ) PowerupFlags );
		}

		public void RemovePowerup( Powerup powerup )
		{
			PowerupFlags &= ~powerup;
			SyncClientData( NumDiscs, ( int ) PowerupFlags );
		}

		public void RemoveAllPowerups()
		{
			PowerupFlags = 0;
			SyncClientData( NumDiscs, ( int ) PowerupFlags );
		}

		public bool HasPowerup( Powerup powerup )
		{
			return PowerupFlags.HasFlag( powerup );
		}

		public void GiveDisc( int num )
		{
			NumDiscs = ( int ) MathX.Clamp( NumDiscs += num, 0, MaxDiscs );
			SyncClientData( NumDiscs, ( int ) PowerupFlags );
		}

		public void RemoveDisc( int num )
		{
			NumDiscs = ( int ) MathX.Clamp( NumDiscs -= num, 0, MaxDiscs );
			SyncClientData( NumDiscs, ( int ) PowerupFlags );
		}

		public void Freeze()
		{
			// TODO: Glowing render effect
			RenderColor = new Color( 0, 0, 200, 230 );
			( Controller as MinimalWalkController ).WalkSpeed = FreezeSpeed;
			Frozen = true;
			FreezeTimer = Time.Now + FreezeTime;
		}

		public void ClearFreeze()
		{
			var walk = Controller as MinimalWalkController;
			// TODO: Glowing render effect
			RenderColor = Color.White;
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
				LastAttacker = killer;
				DamageInfo dmg = new() { Damage = 500 };
				TakeDamage( dmg );
				using ( Prediction.Off() )
				{
					PlaySound( "decap" );
				}
				SpawnHeadModel();
			}
		}

		public void Shatter( RicochetPlayer killer )
		{
			if ( Alive() )
			{
				LastAttacker = killer;
				DamageInfo dmg = new() { Damage = 500 };
				TakeDamage( dmg );
				RenderColor = Color.Transparent;
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
			RicochetCorpse head = new();
			head.Position = Position;
			head.SetHead();
			RicochetCorpse body = new();
			body.Position = Position;
			body.SetBody();
		}
	}

	public class RicochetCorpse : ModelEntity
	{
		public override void Spawn()
		{
			base.Spawn();
			SetModel( "models/citizen/citizen.vmdl" );

			// Player corpse collision code from https://github.com/TTTReborn/tttreborn/blob/master/code/player/PlayerCorpse.cs#L22-L27
			MoveType = MoveType.Physics;
			UsePhysicsCollision = true;
			SetInteractsAs( CollisionLayer.Debris );
			SetInteractsWith( CollisionLayer.WORLD_GEOMETRY );
			SetInteractsExclude( CollisionLayer.Player );
		}
		
		public void SetHead()
		{
			for ( int i = 1; i < 6; i++ )
			{
				SetBodyGroup( i, 1 );
			}
			Velocity += Velocity.WithZ( 3000 );
		}

		public void SetBody()
		{
			SetBodyGroup( 0, 1 );
		}

		[Event( "PlayerRespawn" )]
		private void OnPlayerRespawn()
		{
			Delete();
		}
	}

	public class MinimalWalkController : WalkController
	{
		public override void CheckJumpButton() {}
		public override void StayOnGround() {}

		public MinimalWalkController()
		{
			WalkSpeed = 250.0f;
			DefaultSpeed = 250.0f;
		}

		public override float GetWishSpeed()
		{
			return WalkSpeed;
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
			return currentdist < 40;
		}
	}
}
