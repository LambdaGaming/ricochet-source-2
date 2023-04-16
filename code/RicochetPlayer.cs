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

	public enum DeathReason {
		Disc,
		Decap,
		Fall
	}

	public partial class RicochetPlayer : Player // TODO: Stop relying on Player class from base game since it's obsolete
	{
		[Net] public int NumDiscs { get; set; }
		[Net] public int PowerupDiscs { get; set; }
		[Net] public int Team { get; set; } = -1;
		[Net] public Color TeamColor { get; set; }
		[Net] public Powerup PowerupFlags { get; set; }
		[Net] public int LastAttackWeaponBounces { get; set; } = 0;
		[Net] public DeathReason LastDeathReason { get; set; }
		[Net] public bool IsSpectator { get; set; } = false;
		[Net, Local] public RightHand RightHand { get; set; } // Apparently the Local attribute isn't implemented yet, so hands will appear for other clients for now
		[Net, Local] public LeftHand LeftHand { get; set; }
		[Net, Predicted] public bool DeathCamera { get; set; }
		public float DiscCooldown { get; set; }
		public float OwnerTouchCooldown { get; set; }
		public float EnemyTouchCooldown { get; set; }
		public float FreezeTimer { get; set; }
		public bool Frozen { get; set; }
		public static readonly int MaxDiscs = 3;
		public static readonly int FreezeSpeed = 50;
		public static readonly int FreezeTime = 7;

		public override void Respawn()
		{
			if ( Ricochet.CurrentRound is ArenaRound && Ricochet.CurrentRound.CurrentState == RoundState.Active )
				return;

			base.Respawn();
			SetModel( "models/citizen/citizen.vmdl" );

			if ( Client.IsUsingVr )
			{
				Controller = new VRWalkController();
				CreateVRHands();
			}
			else
			{
				Controller = new RicochetWalkController();
			}
			
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
			Team = Team < 0 ? AutoAssignTeam() : Team;
			TeamColor = GetTeamColor();
			Frozen = false;
			RenderColor = Color.White;
			LastAttacker = null;
			LastAttackerWeapon = null;
			LastAttackWeaponBounces = 0;
			DeathCamera = false;
			Tags.Add( "player" );

			if ( IsSpectator )
			{
				RemoveSpectator();
			}
			
			using ( Prediction.Off() )
			{
				PlaySound( "r_tele1" );
			}
			Event.Run( "PlayerRespawn" );
		}
		
		public override void Simulate( IClient cl )
		{
			base.Simulate( cl );
			SimulateActiveChild( cl, ActiveChild );
			RightHand?.Simulate( cl );
			LeftHand?.Simulate( cl );

			PawnController controller = GetActiveController();
			if ( controller != null )
			{
				SimulateAnimation( controller );
			}

			if ( Game.IsServer && DiscCooldown < Time.Now && Alive() && !IsSpectator && Ricochet.CurrentRound.CurrentState == RoundState.Active )
			{
				if ( Input.Pressed( InputButton.PrimaryAttack ) || ( RightHand.IsValid() && RightHand.TriggerPressed ) )
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
				else if ( Input.Pressed( InputButton.SecondaryAttack ) || ( LeftHand.IsValid() && LeftHand.TriggerPressed ) )
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

		private void FireDisc( bool decap = false )
		{
			Vector3 vecsrc;
			Vector3 vecdir;
			if ( Client.IsUsingVr )
			{
				if ( decap )
				{
					vecsrc = LeftHand.Position + ( ( LeftHand.Rotation.Forward.WithZ( 0 ) * 25 ) + ( Rotation.Up * 1 ) );
					vecdir = LeftHand.Rotation.Forward;
				}
				else
				{
					vecsrc = RightHand.Position + ( ( RightHand.Rotation.Forward.WithZ( 0 ) * 25 ) + ( Rotation.Up * 1 ) );
					vecdir = RightHand.Rotation.Forward;
				}
			}
			else
			{
				vecsrc = Position + ( ( EyeRotation.Forward.WithZ( 0 ) * 25 ) + ( Rotation.Up * 35 ) );
				vecdir = EyeRotation.Forward;
			}

			Disc maindisc = Disc.CreateDisc( vecsrc, vecdir, this, decap, PowerupFlags );
			if ( HasPowerup( Powerup.Triple ) )
			{
				Disc disc = Disc.CreateDisc( vecsrc, EyeRotation.Right, this, decap, PowerupFlags );
				disc.IsExtra = true;

				Disc disc2 = Disc.CreateDisc( vecsrc, EyeRotation.Right * -1, this, decap, PowerupFlags );
				disc2.IsExtra = true;
			}

			PowerupDiscs--;
			if ( PowerupDiscs <= 0 )
			{
				RemoveAllPowerups();
			}

			DiscCooldown = Time.Now + ( HasPowerup( Powerup.Fast ) ? 0.2f : 0.5f );
			OwnerTouchCooldown = Time.Now + 0.1f;
		}
		
		public override void OnKilled()
		{
			base.OnKilled();
			EnableAllCollisions = false;
			EnableDrawing = false;

			if ( !Client.IsUsingVr )
			{
				DeathCamera = true;
			}
			
			if ( Ricochet.CurrentRound is ArenaRound )
			{
				Ricochet.CurrentRound.EndRound();
			}
		}

		public override void TakeDamage( DamageInfo info )
		{
			base.TakeDamage( info );
			if ( info.Weapon is Disc disc && disc.IsDecap )
			{
				// Add 2 extra points for decap kills
				info.Attacker.Client.AddInt( "kills", 2 );
			}
		}

		[ClientRpc]
		public void SyncCorpse( ModelEntity ent )
		{
			// Update the corpse on the client since it's not automatically networked
			Corpse = ent;
		}

		public bool Alive()
		{
			return LifeState == LifeState.Alive;
		}

		public void AddPowerup( Powerup powerup )
		{
			int additional = powerup == Powerup.Fast ? 3 : 0;
			PowerupFlags |= powerup;
			PowerupDiscs = MaxDiscs + additional;
		}

		public void RemovePowerup( Powerup powerup )
		{
			PowerupFlags &= ~powerup;
		}

		public void RemoveAllPowerups()
		{
			PowerupFlags = 0;
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
			RenderColor = new Color( 0, 0, 200, 230 );
			( Controller as RicochetWalkController ).WalkSpeed = FreezeSpeed;
			Frozen = true;
			FreezeTimer = Time.Now + FreezeTime;
		}

		public void ClearFreeze()
		{
			var walk = Controller as RicochetWalkController;
			// TODO: Glowing render effect
			RenderColor = Color.White;
			walk.WalkSpeed = walk.DefaultSpeed;
			Frozen = false;
		}

		public void Decapitate( RicochetPlayer killer, Disc disc )
		{
			if ( Frozen )
			{
				Shatter( killer, disc );
				return;
			}

			if ( Alive() )
			{
				LastAttacker = killer;
				DamageInfo dmg = new() {
					Damage = 500,
					Attacker = killer,
					Weapon = disc
				};
				TakeDamage( dmg );
				using ( Prediction.Off() )
				{
					PlaySound( "decap" );
				}
				SpawnHeadModel();
			}
		}

		public void Shatter( RicochetPlayer killer, Disc disc )
		{
			if ( Alive() )
			{
				LastAttacker = killer;
				DamageInfo dmg = new() {
					Damage = 500,
					Attacker = killer,
					Weapon = disc
				};
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
			return Ricochet.GetPlayers().IndexOf( this );
		}

		public int AutoAssignTeam()
		{
			if ( Ricochet.CurrentRound is DeathmatchRound && DeathmatchRound.IsTDM )
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
			Corpse = body;
			SyncCorpse( body );
		}

		public void ApplyForce( Vector3 force )
		{
			if ( Controller is RicochetWalkController controller )
			{
				controller.Impulse += force;
			}
		}

		public void SetSpectator()
		{
			IsSpectator = true;
			DeathCamera = true;
			Controller = null;
			EnableAllCollisions = false;
			EnableDrawing = false;
		}

		public void RemoveSpectator()
		{
			IsSpectator = false;
			Controller = new RicochetWalkController();
			DeathCamera = false;
			EnableAllCollisions = true;
			EnableDrawing = true;
		}

		private void CreateVRHands()
		{
			DeleteVRHands();
			RightHand = new() { Owner = this, Scale = 0.5f };
			LeftHand = new() { Owner = this, Scale = 0.5f };
		}
		
		public void DeleteVRHands()
		{
			RightHand?.Delete();
			LeftHand?.Delete();
		}

		public override void FrameSimulate( IClient cl )
		{
			Camera.Rotation = ViewAngles.ToRotation();
			RightHand?.FrameSimulate( cl );
			LeftHand?.FrameSimulate( cl );

			if ( DeathCamera )
			{
				Camera.FieldOfView = 90;
				Camera.FirstPersonViewer = null;

				Vector3? targetpos = null;
				if ( IsSpectator )
				{
					foreach ( IClient client in Game.Clients )
					{
						var ply = cl.Pawn as RicochetPlayer;
						if ( ply.Alive() && !ply.IsSpectator )
						{
							targetpos = ply.Position; // Pick first player thats still alive
							break;
						}
					}
				}
				else
				{
					if ( Corpse.IsValid() )
					{
						Position = Corpse.Position;
					}
				}
				targetpos ??= Position;
				Camera.Position = ( Vector3 ) ( targetpos + ViewAngles.ToRotation().Forward * ( -130 * 1 ) + Vector3.Up * ( 20 * 1 ) );
			}
			else
			{
				Camera.Position = EyePosition;
				Camera.FieldOfView = Screen.CreateVerticalFieldOfView( Game.Preferences.FieldOfView );
				Camera.FirstPersonViewer = this;
				Camera.Main.SetViewModelCamera( Camera.FieldOfView );
			}
		}

		void SimulateAnimation( PawnController controller )
		{
			if ( controller == null ) return;

			// where should we be rotated to
			var turnSpeed = 0.02f;

			Rotation rotation;

			// If we're a bot, spin us around 180 degrees.
			if ( Client.IsBot )
				rotation = ViewAngles.WithYaw( ViewAngles.yaw + 180f ).ToRotation();
			else
				rotation = ViewAngles.ToRotation();

			var idealRotation = Rotation.LookAt( rotation.Forward.WithZ( 0 ), Vector3.Up );
			Rotation = Rotation.Slerp( Rotation, idealRotation, controller.WishVelocity.Length * Time.Delta * turnSpeed );
			Rotation = Rotation.Clamp( idealRotation, 45.0f, out var shuffle ); // lock facing to within 45 degrees of look direction

			CitizenAnimationHelper animHelper = new( this );
			animHelper.WithWishVelocity( controller.WishVelocity );
			animHelper.WithVelocity( controller.Velocity );
			animHelper.WithLookAt( EyePosition + EyeRotation.Forward * 100.0f, 1.0f, 1.0f, 0.5f );
			animHelper.AimAngle = rotation;
			animHelper.FootShuffle = shuffle;
			animHelper.VoiceLevel = (Game.IsClient && Client.IsValid()) ? Client.Voice.LastHeard < 0.5f ? Client.Voice.CurrentLevel : 0.0f : 0.0f;
			animHelper.IsGrounded = GroundEntity != null;

			if ( ActiveChild is BaseCarriable carry )
			{
				carry.SimulateAnimator( animHelper );
			}
			else
			{
				animHelper.HoldType = CitizenAnimationHelper.HoldTypes.None;
				animHelper.AimBodyWeight = 0.5f;
			}
		}
	}

	public class RicochetCorpse : ModelEntity
	{
		public override void Spawn()
		{
			base.Spawn();
			SetModel( "models/citizen/citizen.vmdl" );
			SetupPhysicsFromModel( PhysicsMotionType.Dynamic );
			Tags.Add( "ragdoll", "debris" );
		}
		
		public void SetHead()
		{
			for ( int i = 1; i < 6; i++ )
			{
				SetBodyGroup( i, 1 );
			}
			Velocity += Velocity.WithZ( 1000 );
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
}
