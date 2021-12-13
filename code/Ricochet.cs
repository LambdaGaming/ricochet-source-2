using Sandbox;
using Sandbox.UI;
using System;
using System.Collections.Generic;

namespace Ricochet
{
	public partial class Ricochet : Game
	{
		public static int TeamCount { get; set; } = 2;
		public static int[] TotalTeams { get; set; } = new int[TeamCount];
		public static BaseRound CurrentRound { get; set; }

		[ServerVar( "rc_roundtype", Help = "Type of round. 0 for deathmatch, 1 for team deathmatch, and 2 for arena. Requires server reload after changing." )]
		public static RoundType InitialRoundType { get; set; } = RoundType.Deathmatch;

		// TODO: Add option to select initial round type from the game creation menu once Facepunch adds support for that

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

			switch ( InitialRoundType )
			{
				case RoundType.Deathmatch:
				{
					CurrentRound = new DeathmatchRound();
					break;
				}
				case RoundType.TeamDeathmatch:
				{
					CurrentRound = new DeathmatchRound( true );
					break;
				}
				case RoundType.Arena:
				{
					CurrentRound = new ArenaRound();
					break;
				}
			}
		}

		private void CheckRoundState( Client cl )
		{
			if ( CurrentRound is ArenaRound )
			{
				if ( CurrentRound.CurrentState == RoundState.Waiting )
				{
					if ( Client.All.Count >= ArenaRound.MinPlayers )
					{
						CurrentRound.StartRound();
						return;
					}
					ChatBox.AddInformation( To.Everyone, $"Waiting for {ArenaRound.MinPlayers - Client.All.Count} more players..." );
				}
				else
				{
					( cl.Pawn as RicochetPlayer ).SetSpectator();
					ChatBox.AddInformation( To.Single( cl ), "An arena game is currently active. You are now a spectator." );
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
				ply.RemoveSpectator();
			}
		}

		public override void MoveToSpawnpoint( Entity pawn )
		{
			if ( CurrentRound is ArenaRound )
			{
				Random rand = new();
				var ply = pawn as RicochetPlayer;
				string color = ply.Team == 0 ? "red" : "blue";
				Entity spawnpoint = FindByName( $"spawn_{color}{rand.Next( 1, 5 )}" );
				
				if ( spawnpoint == null )
                {
					Log.Warning( $"Couldn't find spawnpoint for {pawn}!" );
					return;
				}

				pawn.Transform = spawnpoint.Transform;
				return;
			}
			base.MoveToSpawnpoint( pawn );
		}

		public override void ClientJoined( Client client )
		{
			base.ClientJoined( client );
			var player = new RicochetPlayer();
			client.Pawn = player;
			player.Respawn();
			CheckRoundState( client );
		}

		public override void ClientDisconnect( Client client, NetworkDisconnectionReason reason )
		{
			var ply = client.Pawn as RicochetPlayer;
			TotalTeams[ply.Team]--;
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
