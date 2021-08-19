using Sandbox;

namespace Ricochet
{
	class Disc : ModelEntity
	{
		public float DiscVelocity { get; set; }
		public int TotalBounces { get; set; }
		public RicochetPlayer LockTarget { get; set; }
		public float NextThink { get; set; }
		public static readonly int DiscPushMultiplier = 1200;

		public new void Spawn()
		{
			if ( !Owner.IsValid() ) return;
			base.Spawn();
			SetModel( "models/disc/disc.vmdl" );
			DiscVelocity = HasPowerup( Powerup.Fast ) ? 1500 : 1000;
			Vector3 vel = Owner.EyeRot.Forward * DiscVelocity;
			vel.z = 0;
			Position = Owner.Position + ( Owner.EyeRot.Forward.WithZ( 0 ) * 100 ) + ( Owner.Rotation.Up * 50 );
			SetupPhysicsFromModel( PhysicsMotionType.Dynamic, false );
			PhysicsGroup.Velocity = vel;
			PhysicsBody.GravityEnabled = false;
			TotalBounces = 0;
			NextThink = 0;
		}
		
		public override void StartTouch( Entity ent )
		{
			var ply = ent as RicochetPlayer;
			var owner = Owner as RicochetPlayer;
			if ( ply.IsValid() )
			{
				if ( ent == Owner )
				{
					if ( ply.OwnerTouchCooldown < Time.Now )
					{
						// TODO: Emit disc pickup sound
						ReturnToThrower();
					}
					return;
				}
				else if ( ply.EnemyTouchCooldown < Time.Now )
				{
					if ( owner.Team != ply.Team )
					{
						if ( owner.HasPowerup( Powerup.Freeze ) && !owner.Frozen )
						{
							// TODO: Emit freeze sound
							ply.Freeze();
							if ( !owner.HasPowerup( Powerup.Hard ) )
							{
								owner.EnemyTouchCooldown = Time.Now + 2;
								return;
							}
						}

						if ( owner.HasPowerup( Powerup.Hard ) )
						{
							ply.Decapitate( owner );
							owner.EnemyTouchCooldown = Time.Now + 0.5f;
						}
						else
						{
							// TODO: Play random hit sound
							Vector3 direction = ply.Velocity.Normal;
							ply.Velocity = direction * DiscPushMultiplier;

							if ( !ply.Frozen )
							{
								// TODO: Shield flash
							}

							ply.LastPlayerToHitMe = owner;
							ply.EnemyTouchCooldown = Time.Now + 2;
						}
					}
				}
			}
			else if ( ent is Disc )
			{
				if ( ent != this )
				{
					// TODO: Emit warp sound and spawn sprite
					( ent as Disc ).ReturnToThrower();
					ReturnToThrower();
				}
			}
			else
			{
				TotalBounces++;
				// TODO: Emit hit sound and spark effects
			}
		}

		[Event.Tick.Server]
		protected void Tick()
		{
			if ( NextThink > Time.Now ) return;
			if ( HasPowerup( Powerup.Freeze ) && TotalBounces == 0 )
			{
				if ( LockTarget.IsValid() )
				{
					// No clue if this works or if its actually needed
					Vector3 direction = ( LockTarget.Position - Position ).Normal;
					float dot = Vector3.Dot( Vector3.Forward, direction );
					if ( dot < 0.6f || ( Owner as RicochetPlayer ).Team == LockTarget.Team )
					{
						LockTarget = null;
					}
				}

				if ( !LockTarget.IsValid() )
				{
					foreach ( RicochetPlayer ply in FindAllByName( "RicochetPlayer" ) )
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
					WorldAng = Vector3.VectorAngle( Velocity );
				}
			}

			if ( TotalBounces >= 3 || ( HasPowerup( Powerup.Fast ) && TotalBounces >= 1 ) )
			{
				if ( TotalBounces > 7 )
				{
					ReturnToThrower();
					return;
				}

				if ( Owner.IsValid() && ( Owner as RicochetPlayer ).Alive() )
				{
					Vector3 direction = ( Owner.Position - Position ).Normal;
					Velocity = direction * DiscVelocity;
				}
				else
				{
					Delete();
				}
			}
			NextThink = Time.Now + 0.1f;
		}

		public bool HasPowerup( Powerup powerup )
		{
			var ply = Owner as RicochetPlayer;
			return ply.PowerupFlags.HasFlag( powerup );
		}

		public void ReturnToThrower()
		{
			var ply = Owner as RicochetPlayer;
			if ( HasPowerup( Powerup.Hard ) )
			{
				ply.GiveDisc( RicochetPlayer.MaxDiscs );
			}
			else
			{
				ply.GiveDisc( 1 );
			}
			Delete();
		}
	}
}
