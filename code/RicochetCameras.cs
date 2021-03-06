using Sandbox;

namespace Ricochet
{
	public class RicochetDeathCam : CameraMode
	{
		Vector3 FocusPoint;

		public override void Activated()
		{
			base.Activated();
			FocusPoint = CurrentView.Position - GetViewOffset();
			FieldOfView = CurrentView.FieldOfView;
		}

		public override void Update()
		{
			var player = Local.Client;
			if ( player == null ) return;
			FocusPoint = GetSpectatePoint();
			Position = FocusPoint + GetViewOffset();
			Rotation = Input.Rotation;
			FieldOfView = 50;
			Viewer = null;
		}

		public virtual Vector3 GetSpectatePoint()
		{
			if ( Local.Pawn is Player player && player.Corpse.IsValid() )
			{
				return player.Corpse.Position;
			}
			return Local.Pawn.Position;
		}

		public virtual Vector3 GetViewOffset()
		{
			var player = Local.Client;
			if ( player == null ) return Vector3.Zero;
			return Input.Rotation.Forward * ( -130 * 1 ) + Vector3.Up * ( 20 * 1 );
		}
	}

	public class RicochetSpectateCam : CameraMode
	{
		Vector3 FocusPoint;

		public override void Activated()
		{
			base.Activated();
			FocusPoint = CurrentView.Position - GetViewOffset();
			FieldOfView = CurrentView.FieldOfView;
		}

		public override void Update()
		{
			var player = Local.Client;
			if ( player == null ) return;
			FocusPoint = GetSpectatePoint();
			Position = FocusPoint + GetViewOffset();
			Rotation = Input.Rotation;
			FieldOfView = 70;
			Viewer = null;
		}

		public virtual Vector3 GetSpectatePoint()
		{
			if ( Client.All.Count < ArenaRound.MinPlayers )
			{
				return Vector3.Zero;
			}

			foreach ( Client cl in Client.All )
			{
				var ply = cl.Pawn as RicochetPlayer;
				if ( ply.Alive() && !ply.IsSpectator )
				{
					return ply.Position;
				}
			}
			return Local.Pawn.Position;
		}

		public virtual Vector3 GetViewOffset()
		{
			var player = Local.Client;
			if ( player == null ) return Vector3.Zero;
			return Input.Rotation.Forward * ( -130 * 1 ) + Vector3.Up * ( 20 * 1 );
		}
	}
}
