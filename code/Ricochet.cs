using Sandbox;
using Sandbox.UI;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ricochet
{
	public enum RoundState {
		Waiting,
		Countdown,
		Active,
		End
	}

	public partial class Ricochet : Game
	{
		public static int TotalClients { get; set; } = 0;
		public static int TeamCount { get; set; } = 2;
		public static int[] TotalTeams { get; set; } = new int[TeamCount];
		public static RoundState CurrentState { get; set; } = RoundState.Waiting;
		public static int RoundCount { get; set; } = 0;

		[ServerVar( "rc_tdm", Help = "Enable or disable teams." )]
		public static bool IsTDM { get; set; } = false;

		[ServerVar( "rc_minplayers", Help = "Minimum amount of players required to start." )]
		public static int MinPlayers { get; set; } = 2;

		[ServerVar( "rc_maxrounds", Help = "Max rounds to play before the map changes." )]
		public static int MaxRounds { get; set; } = 3;

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

		private async Task RoundCountdown()
		{
			CurrentState = RoundState.Countdown;
			for ( int i = 0; i < 3; i++ )
			{
				Sound.FromScreen( "one" );
				await Task.DelaySeconds( 1 );
			}
			Sound.FromScreen( "die" );
			CurrentState = RoundState.Active;
			SpawnSpectators();
		}

		private void CheckRoundState()
		{
			switch ( CurrentState )
			{
				case RoundState.Waiting:
				{
					if ( TotalClients >= MinPlayers )
					{
						_ = RoundCountdown();
						break;
					}
					ChatBox.AddInformation( To.Everyone, $"Waiting for {MinPlayers - TotalClients} more players..." );
					break;
				}
				case RoundState.Active:
				{
					if ( TotalClients < MinPlayers )
					{
						CurrentState = RoundState.Waiting;
						ChatBox.AddInformation( To.Everyone, $"Waiting for {MinPlayers - TotalClients} more players..." );
						break;
					}
					// TODO: Implement round timer and map change once more maps are added
					break;
				}
			}
		}

		public static List<RicochetPlayer> GetPlayers( bool spectatorsonly = false )
		{
			List<RicochetPlayer> players = new();
			foreach ( Client cl in Client.All )
			{
				var ply = cl.Pawn as RicochetPlayer;
				if ( ply.IsValid() )
				{
					if ( spectatorsonly && ply.IsSpectator )
					{
						players.Add( ply );
						continue;
					}
					players.Add( ply );
				}
			}
			return players;
		}

		public static void SpawnSpectators()
		{
			foreach ( RicochetPlayer ply in GetPlayers( true ) )
			{
				ply.SetSpectator();
			}
		}

		public override void ClientJoined( Client client )
		{
			base.ClientJoined( client );
			var player = new RicochetPlayer();
			client.Pawn = player;
			TotalClients++;
			player.Respawn();
			CheckRoundState();
			if ( CurrentState == RoundState.Waiting )
			{
				player.SetSpectator();
			}
		}

		public override void ClientDisconnect( Client client, NetworkDisconnectionReason reason )
		{
			var ply = client.Pawn as RicochetPlayer;
			TotalTeams[ply.Team - 1]--;
			TotalClients--;
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
	}
}
