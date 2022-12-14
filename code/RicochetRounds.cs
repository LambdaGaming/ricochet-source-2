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
		public RicochetPlayer PlayerOne { get; set; }
		public RicochetPlayer PlayerTwo { get; set; }

		[ConVar.Server( "rc_minplayers", Help = "Minimum amount of players required to start an arena match." )]
		public static int MinPlayers { get; set; } = 2;

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
			IClient plyone = plylist[rand.Next( plylist.Count )];
			PlayerOne = plyone.Pawn as RicochetPlayer;
			PlayerOne.Team = 0;
			PlayerOne.Respawn();
			plylist.Remove( plyone ); // Remove the first selected player so they can't be chosen for player 2

			IClient plytwo = plylist[rand.Next( plylist.Count )];
			PlayerTwo = plytwo.Pawn as RicochetPlayer;
			PlayerTwo.Team = 1;
			PlayerTwo.Respawn();
			plylist.Remove( plytwo );

			foreach ( IClient cl in plylist )
			{
				( cl.Pawn as RicochetPlayer ).SetSpectator();
			}

			_ = RoundCountdown();
			TotalRounds++;
		}

		public override void EndRound()
		{
			if ( TotalRounds >= MaxRounds )
			{
				// TODO: Implement map change once more maps are available
			}
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
