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
			base.Respawn();
			SetModel( "models/citizen/citizen.vmdl" );
			Controller = new RicochetWalkController();
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
			if ( Game.IsServer && DiscCooldown < Time.Now && Alive() && !IsSpectator && Ricochet.CurrentRound.CurrentState == RoundState.Active )
			{
				if ( Input.Pressed( InputButton.PrimaryAttack ) )
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
				else if ( Input.Pressed( InputButton.SecondaryAttack ) )
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
			Vector3 vecsrc = Position + ( ( EyeRotation.Forward.WithZ( 0 ) * 25 ) + ( Rotation.Up * 35 ) );
			Disc maindisc = Disc.CreateDisc( vecsrc, EyeRotation.Forward, this, decap, PowerupFlags );

			if ( HasPowerup( Powerup.Triple ) )
			{
				Vector3 firedir1 = Vector3.Zero;
				firedir1.y = maindisc.Velocity.y + 7;
				Disc disc = Disc.CreateDisc( vecsrc, firedir1, this, decap, PowerupFlags );
				disc.IsExtra = true;

				Vector3 firedir2 = Vector3.Zero;
				firedir2.x = maindisc.Velocity.x - 7;
				Disc disc2 = Disc.CreateDisc( vecsrc, firedir2, this, decap, PowerupFlags );
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
			DeathCamera = true;
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

		public override void FrameSimulate( IClient cl )
		{
			Camera.Rotation = ViewAngles.ToRotation();

			if ( DeathCamera )
			{
				Camera.FieldOfView = 90;
				Camera.FirstPersonViewer = null;

				Vector3 targetpos = Position;
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

				Camera.Position = targetpos + ViewAngles.ToRotation().Forward * ( -130 * 1 ) + Vector3.Up * ( 20 * 1 );
			}
			else
			{
				Camera.Position = EyePosition;
				Camera.FieldOfView = Screen.CreateVerticalFieldOfView( Game.Preferences.FieldOfView );
				Camera.FirstPersonViewer = this;
				Camera.Main.SetViewModelCamera( Camera.FieldOfView );
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
			Tags.Add( "ragdoll", "solid", "debris" );
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
}
