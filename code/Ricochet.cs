using Sandbox;
using Sandbox.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ricochet
{
	public partial class Ricochet : GameManager
	{
		public static int TeamCount { get; set; } = 2;
		public static int[] TotalTeams { get; set; } = new int[TeamCount];
		public static BaseRound CurrentRound { get; set; }

		[ConVar.Server( "rc_allowvr", Help = "Allow VR players to join the game." )]
		public static bool AllowVRPlayers { get; set; } = true;

		[ConVar.Server( "rc_roundtype", Help = "Type of round. 0 for deathmatch, 1 for team deathmatch, and 2 for arena. Requires server reload after changing." )]
		public static RoundType InitialRoundType { get; set; } = RoundType.Deathmatch;

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
			if ( Game.IsServer )
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

		private void CheckRoundState( IClient cl )
		{
			if ( CurrentRound is ArenaRound )
			{
				if ( CurrentRound.CurrentState == RoundState.Waiting )
				{
					if ( Game.Clients.Count >= ArenaRound.PlayersPerTeam * 2 )
					{
						CurrentRound.StartRound();
						return;
					}
					ChatBox.AddInformation( To.Everyone, $"Waiting for {( ArenaRound.PlayersPerTeam * 2 ) - Game.Clients.Count} more players..." );
				}
				else
				{
					ChatBox.AddInformation( To.Single( cl ), "An arena game is currently active. You are now a spectator." );
				}
				( cl.Pawn as RicochetPlayer ).SetSpectator();
				return;
			}
			( cl.Pawn as RicochetPlayer ).Respawn();
		}

		public static List<RicochetPlayer> GetPlayers( bool spectatorsonly = false )
		{
			List<RicochetPlayer> players = new();
			foreach ( IClient cl in Game.Clients )
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
				string color = ( pawn as RicochetPlayer ).Team == 0 ? "red" : "blue";
				IEnumerable<Entity> ents = FindAllByName( $"spawn_{color}" );
				Entity spawnpoint = ents.ElementAt( rand.Next( ents.Count() ) );
				
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

		public override void ClientJoined( IClient client )
		{
			base.ClientJoined( client );
			RicochetPlayer player = new();
			client.Pawn = player;
			CheckRoundState( client );
			if ( client.IsUsingVr && !AllowVRPlayers )
			{
				client.Kick();
			}
		}

		public override void ClientDisconnect( IClient client, NetworkDisconnectionReason reason )
		{
			var ply = client.Pawn as RicochetPlayer;
			TotalTeams[ply.Team]--;
			if ( client.IsUsingVr )
			{
				ply.DeleteVRHands();
			}
			base.ClientDisconnect( client, reason );
		}

		public override void OnKilled( IClient client, Entity pawn )
		{
			Game.AssertServer();
			var ply = pawn as RicochetPlayer;
			Log.Info( $"{client.Name} was killed by {ply.LastDeathReason}." );
			if ( pawn.LastAttacker != null )
			{
				if ( pawn.LastAttacker.Client != null )
				{
					OnKilledMessage( pawn.LastAttacker.Client.SteamId, pawn.LastAttacker.Client.Name, client.SteamId, client.Name, GetDeathImage( pawn ) );
				}
				else
				{
					OnKilledMessage( pawn.LastAttacker.NetworkIdent, pawn.LastAttacker.ToString(), client.SteamId, client.Name, GetDeathImage( pawn ) );
				}
			}
			else
			{
				OnKilledMessage( 0, "", client.SteamId, client.Name, GetDeathImage( pawn ) );
			}
			if ( client.IsUsingVr )
			{
				ply.DeleteVRHands();
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
