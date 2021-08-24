using Sandbox;

namespace Ricochet
{
	class Disc : ModelEntity
	{
		public float DiscVelocity { get; set; }
		public int TotalBounces { get; set; }
		public RicochetPlayer LockTarget { get; set; }
		public float NextThink { get; set; }
		public bool IsDecap { get; set; }
		private Sound DecapLoop { get; set; }
		public static readonly int DiscPushMultiplier = 1200;

		public new void Spawn()
		{
			if ( !Owner.IsValid() ) return;
			base.Spawn();
			string mdl = HasPowerup( Powerup.Hard ) ? "models/disc_hard/disc_hard.vmdl" : "models/disc/disc.vmdl";
			SetModel( mdl );
			DiscVelocity = HasPowerup( Powerup.Fast ) ? 1500 : 1000;
			Vector3 vel = ( Owner.EyeRot.Forward * DiscVelocity ).WithZ( 0 );
			Position = Owner.Position + ( Owner.EyeRot.Forward.WithZ( 0 ) * 25 ) + ( Owner.Rotation.Up * 35 );
			SetupPhysicsFromModel( PhysicsMotionType.Dynamic, false );
			PhysicsGroup.Velocity = vel;
			PhysicsBody.GravityEnabled = false;
			PhysicsBody.DragEnabled = false;
			TotalBounces = 0;
			NextThink = 0;
			IsDecap = HasPowerup( Powerup.Hard );
			GlowActive = true;
			GlowColor = Color.Red;
			GlowState = GlowStates.GlowStateOn;

			if ( IsDecap )
			{
				DecapLoop = Sound.FromEntity( "rocket1", this );
			}
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
						PlaySound( "gunpickup2" );
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
							ply.Decapitate( owner );
							owner.EnemyTouchCooldown = Time.Now + 0.5f;
						}
						else
						{
							PlaySound( "cbar_hitbod" );
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
					// TODO: Spawn warp sprite
					PlaySound( "dischit" );
					( ent as Disc ).ReturnToThrower();
					ReturnToThrower();
				}
			}
			else
			{
				TotalBounces++;
				PlaySound( "xbow_hit" );
				// TODO: Emit sparks
			}
		}
		
		[Event.Tick.Server]
		protected void Tick()
		{
			if ( NextThink > Time.Now ) return;
			Velocity = ( DiscVelocity * Velocity.Normal ).WithZ( 0 );
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

				if ( Owner.IsValid() )
				{
					Vector3 direction = ( Owner.Position - Position ).Normal;
					Velocity = direction * DiscVelocity;
				}
				else
				{
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
			var ply = Owner as RicochetPlayer;
			return ply.PowerupFlags.HasFlag( powerup );
		}

		public void ReturnToThrower()
		{
			var ply = Owner as RicochetPlayer;
			if ( IsDecap )
			{
				DecapLoop.Stop();
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
