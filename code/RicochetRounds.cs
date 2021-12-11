using Sandbox;
using System;
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

		[ServerVar( "rc_minplayers", Help = "Minimum amount of players required to start." )]
		public static int MinPlayers { get; set; } = 2;

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

		[ServerVar( "rc_maxrounds", Help = "Max rounds to play before the map changes." )]
		public static int MaxRounds { get; set; } = 3;

		public override void StartRound()
		{
			Random rand = new();
			PlayerOne = ( RicochetPlayer ) Client.All[rand.Next( Client.All.Count )].Pawn;
			PlayerTwo = ( RicochetPlayer ) Client.All[rand.Next( Client.All.Count )].Pawn;
			PlayerOne.Team = 1;
			PlayerTwo.Team = 2;
			PlayerOne.Respawn();
			PlayerTwo.Respawn();
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
			PlayerOne.SetSpectator();
			PlayerTwo.SetSpectator();
		}
	}

	public class DeathmatchRound : BaseRound
	{
		[ServerVar( "rc_tdm", Help = "Enable or disable teams." )]
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
