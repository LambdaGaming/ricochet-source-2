using Sandbox;
using System;
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

	public enum RoundType {
		Deathmatch,
		TeamDeathmatch,
		Arena
	}

	public abstract class BaseRound
	{
		public RoundState CurrentState { get; set; } = RoundState.Waiting;

		public abstract void StartRound();
		public abstract void EndRound();

		public async Task RoundCountdown()
		{
			CurrentState = RoundState.Countdown;
			for ( int i = 0; i < 3; i++ )
			{
				Sound.FromScreen( "one" );
				await Task.Delay( 1000 );
			}
			Sound.FromScreen( "die" );
			CurrentState = RoundState.Active;
		}
	}

	public class ArenaRound : BaseRound
	{
		public static int TotalRounds { get; set; } = 0;
		public static List<RicochetPlayer> LastWinners = new();
		public static List<RicochetPlayer> CurrentPlayers = new();

		[ConVar.Server( "rc_playersperteam", Help = "Amount of players that should be on each team during an arena round." )]
		public static int PlayersPerTeam { get; set; } = 1;

		[ConVar.Server( "rc_rounds", Help = "Max rounds to play before the map changes." )]
		public static int MaxRounds { get; set; } = 3;

		private async Task RestartRound()
		{
			await Task.Delay( 3000 );
			StartRound();
		}

		public override void StartRound()
		{
			Random rand = new();
			List<IClient> plylist = new( Game.Clients );
			foreach ( RicochetPlayer ply in LastWinners )
			{
				// Make sure a winning player didn't leave
				if ( !ply.IsValid() )
					LastWinners.Remove( ply );
			}

			if ( LastWinners.Count > 0 )
			{
				foreach ( RicochetPlayer ply in LastWinners )
				{
					// Spawn winning team first
					ply.Team = 0;
					ply.Respawn();
					plylist.Remove( ply.Client );
					CurrentPlayers.Add( ply );
				}
			}
			else
			{
				for ( int i = 0; i < PlayersPerTeam; i++ )
				{
					// Spawn random team 1
					RicochetPlayer ply = plylist[rand.Next( plylist.Count )].Pawn as RicochetPlayer;
					ply.Team = 0;
					ply.Respawn();
					plylist.Remove( ply.Client );
					CurrentPlayers.Add( ply );
				}
			}

			for ( int i = 0; i < PlayersPerTeam; i++ )
			{
				// Spawn random team 2
				RicochetPlayer ply = plylist[rand.Next( plylist.Count )].Pawn as RicochetPlayer;
				ply.Team = 1;
				ply.Respawn();
				plylist.Remove( ply.Client );
				CurrentPlayers.Add( ply );
			}

			foreach ( IClient cl in plylist )
			{
				// Spawn remaining players as spectators
				( cl.Pawn as RicochetPlayer ).SetSpectator();
			}
			_ = RoundCountdown();
			TotalRounds++;
		}

		public override void EndRound()
		{
			int aliveTeam = CurrentPlayers[0].Team;
			foreach ( RicochetPlayer ply in CurrentPlayers )
			{
				// Don't end round if at least 2 players of opposing teams are still alive
				if ( aliveTeam != ply.Team )
					return;
			}

			if ( Game.IsServer && TotalRounds >= MaxRounds )
			{
				Random rand = new();
				string[] maps = { "lambdagaming.rc_deathmatch", "lambdagaming.rc_deathmatch_2", "lambdagaming.rc_arena" };
				Game.ChangeLevel( maps[rand.Next( maps.Length )] );
			}
			CurrentPlayers.Clear();
			CurrentState = RoundState.End;
			_ = RestartRound();
		}
	}

	public class DeathmatchRound : BaseRound
	{
		public static bool IsTDM { get; set; } = false;

		public DeathmatchRound( bool tdm = false )
		{
			IsTDM = tdm;
			StartRound();
		}

		public override void StartRound()
		{
			CurrentState = RoundState.Active;
		}

		public override void EndRound()
		{
			CurrentState = RoundState.End;
		}
	}
}
