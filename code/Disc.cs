﻿using Sandbox;
using System.Threading.Tasks;

namespace Ricochet;

public class Disc : ModelEntity
{
	public float DiscVelocity { get; set; }
	public int TotalBounces { get; set; } = 0;
	public Player LockTarget { get; set; }
	public float NextThink { get; set; } = 0;
	public bool IsDecap { get; set; } = false;
	public bool IsExtra { get; set; } = false;
	public Powerup PowerupFlags { get; set; }
	public int Team { get; set; } = 0;
	private bool IsSecondary { get; set; } = false;
	private Sound DecapLoop { get; set; }
	private float SetZ { get; set; }
	private Vector3 FireDir { get; set; }

	public static readonly int DiscPushMultiplier = 1000;
	private PointLightEntity DiscLight;

	public new void Spawn()
	{
		if ( !Owner.IsValid() ) return;
		base.Spawn();
		SetModel( IsDecap ? "models/disc_hard/disc_hard.vmdl" : "models/disc/disc.vmdl" );
		DiscVelocity = HasPowerup( Powerup.Fast ) ? 1500 : 1000;
		SetupPhysicsFromModel( PhysicsMotionType.Dynamic );
		Velocity = ( FireDir * DiscVelocity ).WithZ( 0 );
		SetZ = Position.z;
		PhysicsBody.GravityEnabled = false;
		PhysicsBody.DragEnabled = false;
		Tags.Add( "trigger", "disc" );
		RenderColor = ( Owner as Player ).TeamColor;

		DiscLight = new PointLightEntity
		{
			Color = RenderColor,
			LightSize = 0.01f,
			Brightness = 0.1f,
			Position = Position,
			Parent = this
		};

		using ( Prediction.Off() )
		{
			if ( IsDecap )
			{
				DecapLoop = PlaySound( "rocket1" );
			}
		}
	}

	public static Disc CreateDisc( Vector3 position, Vector3 firedir, Player owner, bool decap, Powerup flags )
	{
		Disc disc = new();
		disc.Position = position;
		disc.PowerupFlags = flags;
		disc.IsDecap = flags.HasFlag( Powerup.Hard ) || decap;
		disc.IsSecondary = decap;
		disc.FireDir = firedir;
		disc.Owner = owner;
		disc.Team = owner.Team;
		disc.Spawn();
		return disc;
	}
	
	public override void StartTouch( Entity ent )
	{
		var owner = Owner as Player;
		if ( ent is PowerupEnt || !Game.IsServer ) return;
		if ( ent is Player ply && ply.IsValid() )
		{
			if ( ent == Owner )
			{
				if ( ply.OwnerTouchCooldown < Time.Now )
				{
					ply.PlaySound( "gunpickup2" );
					ReturnToThrower();
				}
				return;
			}
			else if ( ply.EnemyTouchCooldown < Time.Now )
			{
				if ( owner.Team != ply.Team && ply.Alive() )
				{
					if ( owner.HasPowerup( Powerup.Freeze ) && !owner.Frozen )
					{
						PlaySound( "electro5" );
						ply.Freeze();
						if ( !IsDecap )
						{
							owner.EnemyTouchCooldown = Time.Now + 2;
							return;
						}
					}

					if ( IsDecap )
					{
						ply.LastDeathReason = DeathReason.Decap;
						ply.Decapitate( owner, this );
						owner.EnemyTouchCooldown = Time.Now + 0.5f;
					}
					else
					{
						if ( Game.IsServer )
						{
							PlaySound( "cbar_hitbod" );
							Vector3 direction = Velocity.Normal;
							ply.Velocity = direction * DiscPushMultiplier;
						}

						if ( !ply.Frozen )
						{
							// TODO: Shield flash
						}

						ply.EnemyTouchCooldown = Time.Now + 2;
					}
				}

				ply.LastAttackWeaponBounces = TotalBounces;
				ply.LastAttacker = owner;
				ply.LastAttackerWeapon = this;
				_ = ResetAttacker( ply );
			}
		}
		else if ( ent is Disc disc )
		{
			if ( ent.Owner != Owner )
			{
				var spr = Particles.Create( "particles/discreturn.vpcf", Position );
				spr.Destroy();
				Sound.FromWorld( "dischit", Position );
				disc.ReturnToThrower();
				ReturnToThrower();
			}
		}
		else
		{
			TotalBounces++;
			var particle = Particles.Create( "particles/disc_spark.vpcf", Position );
			particle.Destroy();
			PlaySound( "xbow_hit" );
			Velocity = Vector3.Reflect( Velocity, Position.Normal );
		}
	}

	async Task ResetAttacker( Player ply )
	{
		await Task.DelaySeconds( 10 );
		ply.LastAttackWeaponBounces = 0;
		ply.LastAttacker = null;
		ply.LastAttackerWeapon = null;
	}
	
	[GameEvent.Tick.Server]
	protected void Tick()
	{
		Velocity = ( DiscVelocity * Velocity.Normal ).WithZ( 0 );
		Position = Position.WithZ( SetZ );
		Rotation = Rotation.From( Angles.Zero );
		if ( NextThink > Time.Now ) return;
		if ( HasPowerup( Powerup.Freeze ) && TotalBounces == 0 )
		{
			if ( LockTarget.IsValid() )
			{
				Vector3 direction = ( LockTarget.Position - Position ).Normal;
				float dot = Vector3.Dot( Vector3.Forward, direction );
				if ( dot < 0.6f || ( Owner as Player ).Team == LockTarget.Team )
				{
					LockTarget = null;
				}
			}

			if ( !LockTarget.IsValid() )
			{
				foreach ( Player ply in FindAllByName( "RicochetPlayer" ) )
				{
					if ( !ply.IsValid() || ply == Owner ) continue;
					Vector3 direction = ( ply.Position - Position ).Normal;
					float dot = Vector3.Dot( Vector3.Forward, direction );
					if ( dot > 0.6f )
					{
						LockTarget = ply;
						break;
					}
				}
			}

			if ( LockTarget.IsValid() )
			{
				Vector3 direction = ( LockTarget.Position - Position ).Normal;
				Velocity = ( Velocity.Normal + ( direction.Normal * 0.25f ) ).Normal;
				Velocity *= DiscVelocity;
				Rotation = Rotation.From( Vector3.VectorAngle( Velocity ) );
			}
		}

		if ( TotalBounces >= 3 || ( HasPowerup( Powerup.Fast ) && TotalBounces >= 1 ) )
		{
			if ( TotalBounces > 7 )
			{
				ReturnToThrower();
				return;
			}

			if ( Owner.IsValid() && ( Owner as Player ).Alive() )
			{
				Vector3 direction = ( Owner.Position - Position ).Normal;
				Velocity = direction * DiscVelocity;
			}
			else
			{
				if ( IsDecap )
				{
					DecapLoop.Stop();
				}
				Delete();
			}
		}

		if ( Velocity == Vector3.Zero )
		{
			ReturnToThrower();
		}

		NextThink = Time.Now + 0.1f;
	}

	public bool HasPowerup( Powerup powerup )
	{
		return PowerupFlags.HasFlag( powerup );
	}

	public void ReturnToThrower()
	{
		var ply = Owner as Player;
		if ( IsDecap )
		{
			DecapLoop.Stop();
		}

		if ( IsExtra || !ply.IsValid() )
		{
			if ( Game.IsServer )
				Delete();
			return;
		}
		else if ( IsSecondary )
		{
			ply.GiveDisc( Player.MaxDiscs );
		}
		else
		{
			ply.GiveDisc( 1 );
		}

		if ( Game.IsServer )
			Delete();
	}
}
