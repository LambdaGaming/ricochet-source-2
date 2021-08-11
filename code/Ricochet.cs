using Sandbox;

namespace Ricochet
{
	public partial class Ricochet : Game
	{
		public Ricochet()
		{
			if ( IsServer )
			{
				new RicochetHUD();
			}
		}

		public override void ClientJoined( Client client )
		{
			base.ClientJoined( client );
			var player = new RicochetPlayer();
			client.Pawn = player;
			player.Respawn();
		}
	}

}
