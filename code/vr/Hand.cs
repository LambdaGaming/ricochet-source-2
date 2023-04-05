using Sandbox;

namespace Ricochet
{
	public partial class Hand : AnimatedEntity
	{
		protected virtual string ModelPath => "";
		public bool TriggerPressed => InputHand.Trigger > 0.5f;
		public virtual Input.VrHand InputHand { get; }

		public override void Spawn()
		{
			SetModel( ModelPath );
			Position = InputHand.Transform.Position;
			Rotation = InputHand.Transform.Rotation;
			Transmit = TransmitType.Always;
			Tags.Add( "hand" );
		}

		public override void FrameSimulate( IClient cl )
		{
			base.FrameSimulate( cl );
			Transform = InputHand.Transform;
		}

		public override void Simulate( IClient cl )
		{
			base.Simulate( cl );
			Transform = InputHand.Transform;
		}
	}

	public partial class RightHand : Hand
	{
		protected override string ModelPath => "models/disc/disc.vmdl";
		public override Input.VrHand InputHand => Input.VR.RightHand;
	}

	public partial class LeftHand : Hand
	{
		protected override string ModelPath => "models/disc_hard/disc_hard.vmdl";
		public override Input.VrHand InputHand => Input.VR.LeftHand;
	}
}
