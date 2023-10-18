using Sandbox;
using System;
using System.ComponentModel;

namespace Ricochet;

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

public partial class Player : AnimatedEntity
{
	[Net] public int NumDiscs { get; set; }
	[Net] public int PowerupDiscs { get; set; }
	[Net] public int Team { get; set; } = -1;
	[Net] public Color TeamColor { get; set; }
	[Net] public Powerup PowerupFlags { get; set; }
	[Net] public int LastAttackWeaponBounces { get; set; } = 0;
	[Net] public DeathReason LastDeathReason { get; set; }
	[Net] public bool IsSpectator { get; set; } = false;
	[Net] public Vector3 CorpsePosition { get; set; }
	[Net] public ModelEntity Corpse { get; set; }
	[Net, Local] public RightHand RightHand { get; set; } // Apparently the Local attribute isn't implemented yet, so hands will appear for other clients for now
	[Net, Local] public LeftHand LeftHand { get; set; }
	[Net, Predicted] public bool DeathCamera { get; set; }
	[Net, Predicted] public bool SpectateCamera { get; set; }
	[Net, Predicted] public PawnController Controller { get; set; }
	[Net, Predicted] public Vector3 EyeLocalPosition { get; set; }
	[Net, Predicted] public Rotation EyeLocalRotation { get; set; }
	[ClientInput] public Vector3 InputDirection { get; protected set; }
	[ClientInput] public Entity ActiveChildInput { get; set; }
	[ClientInput] public Angles ViewAngles { get; set; }
	public Angles OriginalViewAngles { get; private set; }
	public float DiscCooldown { get; set; }
	public float OwnerTouchCooldown { get; set; }
	public float EnemyTouchCooldown { get; set; }
	public float FreezeTimer { get; set; }
	public bool Frozen { get; set; }
	
	public Vector3 EyePosition
	{
		get => Transform.PointToWorld( EyeLocalPosition );
		set => EyeLocalPosition = Transform.PointToLocal( value );
	}

	public Rotation EyeRotation
	{
		get => Transform.RotationToWorld( EyeLocalRotation );
		set => EyeLocalRotation = Transform.RotationToLocal( value );
	}

	public static readonly int MaxDiscs = 3;
	public static readonly int FreezeSpeed = 50;
	public static readonly int FreezeTime = 7;

	public override void Spawn()
	{
		EnableLagCompensation = true;
		Tags.Add( "player" );
		base.Spawn();
	}

	public void Respawn()
	{
		if ( Ricochet.CurrentRound is ArenaRound && Ricochet.CurrentRound.CurrentState == RoundState.Active )
			return;

		Game.AssertServer();
		LifeState = LifeState.Alive;
		Health = 100;
		Velocity = Vector3.Zero;

		// Create hull
		SetupPhysicsFromAABB( PhysicsMotionType.Keyframed, new Vector3( -16, -16, 0 ), new Vector3( 16, 16, 72 ) );
		EnableHitboxes = true;
		GameManager.Current?.MoveToSpawnpoint( this );
		ResetInterpolation();
		SetModel( "models/citizen/citizen.vmdl" );

		if ( Client.IsUsingVr )
		{
			Controller = new VRWalkController();
			CreateVRHands();
		}
		else
		{
			Controller = new WalkController();
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
		SpectateCamera = false;
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

	public override void BuildInput()
	{
		if ( Client.IsUsingVr )
		{
			// Take input from vr controllers, from https://github.com/ShadowBrian/sbox-vr-addons/blob/master/boomervr/code/VRControls.cs
			Vector2 move = new( Input.VR.LeftHand.Joystick.Value.y, MathF.Round( -Input.VR.LeftHand.Joystick.Value.x ) );
			Input.AnalogMove = Input.VR.Head.Rotation * Game.LocalPawn.Rotation.Inverse * move;
		}

		OriginalViewAngles = ViewAngles;
		InputDirection = Input.AnalogMove;

		if ( Input.StopProcessing ) return;

		var look = Input.AnalogLook;
		if ( ViewAngles.pitch > 90f || ViewAngles.pitch < -90f )
		{
			look = look.WithYaw( look.yaw * -1f );
		}

		var viewAngles = ViewAngles;
		viewAngles += look;
		viewAngles.pitch = viewAngles.pitch.Clamp( -89f, 89f );
		viewAngles.roll = 0f;
		ViewAngles = viewAngles.Normal;
	}

	TimeSince timeSinceDied;
	public override void Simulate( IClient cl )
	{
		if ( LifeState == LifeState.Dead )
		{
			if ( timeSinceDied > 3 && Game.IsServer )
			{
				Respawn();
			}

			return;
		}

		Controller?.Simulate( cl, this );
		RightHand?.Simulate( cl );
		LeftHand?.Simulate( cl );
		SimulateAnimation( Controller );

		if ( Game.IsServer && DiscCooldown < Time.Now && Alive() && !IsSpectator && Ricochet.CurrentRound.CurrentState == RoundState.Active )
		{
			if ( Input.Pressed( "attack1" ) || ( RightHand.IsValid() && RightHand.TriggerPressed ) )
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
			else if ( Input.Pressed( "attack2" ) || ( LeftHand.IsValid() && LeftHand.TriggerPressed ) )
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

	public override void FrameSimulate( IClient cl )
	{
		RightHand?.FrameSimulate( cl );
		LeftHand?.FrameSimulate( cl );

		if ( DeathCamera )
		{
			Camera.FieldOfView = 90;
			Camera.FirstPersonViewer = null;
			Camera.Position = CorpsePosition;
			Camera.Rotation = Rotation.FromPitch( 90 ) * Rotation.FromRoll( 300 * Time.Now ); // TODO: Make this slowly speed up
		}
		else if ( SpectateCamera )
		{
			Camera.Rotation = ViewAngles.ToRotation();
			Camera.FieldOfView = 90;
			Camera.FirstPersonViewer = null;
			Vector3? targetpos = null;
			foreach ( IClient client in Game.Clients )
			{
				var ply = cl.Pawn as Player;
				if ( ply.Alive() && !ply.IsSpectator )
				{
					targetpos = ply.Position; // Pick first player thats still alive
					break;
				}
			}
			targetpos ??= Position;
			Camera.Position = (Vector3)(targetpos + ViewAngles.ToRotation().Forward * (-130 * 1) + Vector3.Up * (20 * 1));
		}
		else
		{
			Camera.Rotation = ViewAngles.ToRotation();
			Camera.Position = EyePosition;
			Camera.FieldOfView = Screen.CreateVerticalFieldOfView( Game.Preferences.FieldOfView );
			Camera.FirstPersonViewer = this;
			Camera.Main.SetViewModelCamera( Camera.FieldOfView );
		}
		Camera.Main.Tonemap.Enabled = false;
		Camera.Main.Bloom.Enabled = true;
		Camera.Main.Bloom.Strength = 0.1f;
	}

	void SimulateAnimation( PawnController controller )
	{
		if ( controller == null ) return;

		var turnSpeed = 0.02f;
		Rotation rotation;
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
		animHelper.VoiceLevel = ( Game.IsClient && Client.IsValid() ) ? Client.Voice.LastHeard < 0.5f ? Client.Voice.CurrentLevel : 0.0f : 0.0f;
		animHelper.IsGrounded = GroundEntity != null;
		animHelper.HoldType = CitizenAnimationHelper.HoldTypes.None;
		animHelper.AimBodyWeight = 0.5f;
	}

	public override void OnKilled()
	{
		GameManager.Current?.OnKilled( this );
		timeSinceDied = 0;
		LifeState = LifeState.Dead;
		Client?.AddInt( "deaths", 1 );
		EnableAllCollisions = false;
		EnableDrawing = false;

		if ( !Client.IsValid() ) return;

		if ( !Client.IsUsingVr )
		{
			DeathCamera = true;
		}

		if ( Ricochet.CurrentRound is ArenaRound )
		{
			ArenaRound.CurrentPlayers.Remove( this );
			Ricochet.CurrentRound.EndRound();
		}
	}

	public override void TakeDamage( DamageInfo info )
	{
		if ( LifeState == LifeState.Dead ) return;

		base.TakeDamage( info );
		this.ProceduralHitReaction( info );
		if ( LifeState == LifeState.Dead && info.Attacker != null )
		{
			if ( info.Weapon is Disc disc && disc.IsDecap )
			{
				// Add 2 extra points for decap kills
				info.Attacker.Client.AddInt( "kills", 2 );
			}
			else if ( info.Attacker.Client != null && info.Attacker != this )
			{
				info.Attacker.Client.AddInt( "kills" );
			}
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
		( Controller as WalkController ).WalkSpeed = FreezeSpeed;
		Frozen = true;
		FreezeTimer = Time.Now + FreezeTime;
	}

	public void ClearFreeze()
	{
		var walk = Controller as WalkController;
		// TODO: Glowing render effect
		RenderColor = Color.White;
		walk.WalkSpeed = walk.DefaultSpeed;
		Frozen = false;
	}

	public void Decapitate( Player killer, Disc disc )
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

	public void Shatter( Player killer, Disc disc )
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
		PlayerCorpse head = new();
		head.Position = Position;
		head.SetHead();
		PlayerCorpse body = new();
		body.Position = Position;
		body.SetBody();
		Corpse = body;
	}

	public void ApplyForce( Vector3 force )
	{
		if ( Controller is WalkController controller )
		{
			controller.Impulse += force;
		}
	}

	public void SetSpectator()
	{
		IsSpectator = true;
		SpectateCamera = true;
		Controller = null;
		EnableAllCollisions = false;
		EnableDrawing = false;
	}

	public void RemoveSpectator()
	{
		IsSpectator = false;
		Controller = new WalkController();
		SpectateCamera = false;
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
}
