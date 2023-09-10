using Sandbox;

namespace Ricochet;

public class VRWalkController : RicochetWalkController
{
	public override void UpdateBBox()
	{
		Transform headLocal = Pawn.Transform.ToLocal( Input.VR.Head );
		var girth = BodyGirth * 0.5f;
		var mins = ( new Vector3( -girth, -girth, 0 ) + headLocal.Position.WithZ( 0 ) * Rotation ) * Pawn.Scale;
		var maxs = ( new Vector3( +girth, +girth, BodyHeight ) + headLocal.Position.WithZ( 0 ) * Rotation ) * Pawn.Scale;
		SetBBox( mins, maxs );
	}

	public override void FrameSimulate()
	{
		base.FrameSimulate();
		EyeRotation = Input.VR.Head.Rotation;
	}

	public override void Simulate()
	{
		EyeLocalPosition = Vector3.Up * ( EyeHeight * Pawn.Scale );
		UpdateBBox();

		EyeLocalPosition += TraceOffset;
		EyeRotation = Input.VR.Head.Rotation;

		if ( Unstuck.TestAndFix() )
			return;

		if ( Impulse.Length > 0 )
		{
			ClearGroundEntity();
			Velocity = Impulse;
			Impulse = 0f;
		}

		// Start Gravity
		Velocity -= new Vector3( 0, 0, Gravity * 0.5f ) * Time.Delta;
		Velocity += new Vector3( 0, 0, BaseVelocity.z ) * Time.Delta;
		BaseVelocity = BaseVelocity.WithZ( 0 );

		// Fricion is handled before we add in any base velocity. That way, if we are on a conveyor,
		//  we don't slow when standing still, relative to the conveyor.
		bool bStartOnGround = GroundEntity != null;
		if ( bStartOnGround )
		{
			Velocity = Velocity.WithZ( 0 );
			if ( GroundEntity != null )
			{
				ApplyFriction( GroundFriction * SurfaceFriction );
			}
		}

		// Work out wish velocity.. just take input, rotate it to view, clamp to -1, 1
		WishVelocity = new Vector3( Input.VR.LeftHand.Joystick.Value.y.Clamp( -1f, 1f ), Input.VR.LeftHand.Joystick.Value.x.Clamp( -1f, 1f ), 0 );
		var inSpeed = WishVelocity.Length.Clamp( 0, 1 );
		WishVelocity = WishVelocity.WithZ( 0 );
		WishVelocity = WishVelocity.Normal * inSpeed;
		WishVelocity *= GetWishSpeed();

		bool bStayOnGround = false;
		if ( GroundEntity != null )
		{
			bStayOnGround = true;
			WalkMove();
		}
		else
		{
			AirMove();
		}

		CategorizePosition( bStayOnGround );

		// FinishGravity
		Velocity -= new Vector3( 0, 0, Gravity * 0.5f ) * Time.Delta;

		if ( GroundEntity != null )
		{
			Velocity = Velocity.WithZ( 0 );
		}
	}
}
