using Sandbox;
using System.Collections.Generic;

namespace Ricochet
{
	public partial class Ricochet : Game
	{
		public static List<RicochetPlayer> TotalClients { get; set; } = new();
		public static bool IsTDM { get; set; } = false;
		public static int TeamCount { get; set; } = 2;
		public static int[] TotalTeams { get; set; } = new int[TeamCount];
		public static readonly int[,] TeamColors = new int[31, 3] {
			{ 250, 0, 0 },
			{ 0, 0, 250 },
			{ 0, 250, 0 },
			{ 128, 128, 0 },
			{ 128, 0, 128 },
			{ 0, 128, 128 },
			{ 250, 160, 0 },
			{ 64, 128, 0 },
			{ 0, 250, 128 },
			{ 128, 0, 64 },
			{ 64, 0, 128 },
			{ 0, 64, 128 },
			{ 64, 64, 128 },
			{ 128, 64, 64 },
			{ 64, 128, 64 },
			{ 128, 128, 64 },
			{ 128, 64, 128 },
			{ 64, 128, 128 },
			{ 250, 128, 0 },
			{ 128, 250, 0 },
			{ 128, 0, 250 },
			{ 250, 0, 128 },
			{ 128, 128, 128 },
			{ 250, 250, 128 },
			{ 250, 128, 250 },
			{ 128, 250, 250 },
			{ 250, 128, 64 },
			{ 250, 64, 128 },
			{ 128, 250, 64 },
			{ 64, 128, 250 },
			{ 128, 64, 250 }
		};

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
			TotalClients.Add( client.Pawn as RicochetPlayer );
			player.Respawn();
		}

		public override void ClientDisconnect( Client client, NetworkDisconnectionReason reason )
		{
			var ply = client.Pawn as RicochetPlayer;
			TotalTeams[ply.Team - 1]--;
			TotalClients.Remove( ply );
			base.ClientDisconnect( client, reason );
		}

		public override void OnKilled( Client client, Entity pawn )
		{
			Host.AssertServer();
			Log.Info( $"{client.Name} was killed by {( pawn as RicochetPlayer ).LastDeathReason}." );
			if ( pawn.LastAttacker != null )
			{
				if ( pawn.LastAttacker.Client != null )
				{
					OnKilledMessage( pawn.LastAttacker.Client.PlayerId, pawn.LastAttacker.Client.Name, client.PlayerId, client.Name, GetDeathImage( pawn ) );
				}
				else
				{
					OnKilledMessage( pawn.LastAttacker.NetworkIdent, pawn.LastAttacker.ToString(), client.PlayerId, client.Name, GetDeathImage( pawn ) );
				}
			}
			else
			{
				OnKilledMessage( 0, "", client.PlayerId, client.Name, GetDeathImage( pawn ) );
			}
		}

		private string GetDeathImage( Entity pawn )
		{
			var ply = pawn as RicochetPlayer;
			if ( ply.LastDeathReason == DeathReason.Decap )
			{
				return "/ui/hud/icons/decapitate.png";
			}
			else if ( ply.LastDeathReason == DeathReason.Fall )
			{
				return "/ui/hud/icons/falling.png";
			}
			else if ( ply.LastAttackWeaponBounces <= 0 )
			{
				return "/ui/hud/icons/0bounce.png";
			}
			else if ( ply.LastAttackWeaponBounces == 1 )
			{
				return "/ui/hud/icons/1bounce.png";
			}
			else if ( ply.LastAttackWeaponBounces == 2 )
			{
				return "/ui/hud/icons/2bounce.png";
			}
			return "/ui/hud/icons/3bounce.png";
		}

		[ClientRpc]
		public override void OnKilledMessage( long leftid, string left, long rightid, string right, string method )
		{
			RicochetKillFeed.Current?.AddEntry( leftid, left, rightid, right, method );
		}

		[AdminCmd( "rc_tdm" )]
		public static void SetTDM( bool parameter )
		{
			IsTDM = parameter;
		}
	}
}
