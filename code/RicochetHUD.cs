using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System.Collections.Generic;
using System.Linq;

namespace Ricochet
{
	public partial class RicochetHUD : HudEntity<RootPanel>
	{
		public RicochetHUD()
		{
			if ( IsClient )
			{
				RootPanel.AddChild<ChatBox>();
				RootPanel.AddChild<DiscHUD>();
				RootPanel.AddChild<Crosshair>();
				RootPanel.AddChild<RicochetKillFeed>();
				RootPanel.AddChild<RicochetScoreboard<RicochetScoreboardEntry>>();
			}
		}
	}
	
	public class DiscHUD : Panel
	{
		private Image[] DiscImages = new Image[RicochetPlayer.MaxDiscs];

		public DiscHUD()
		{
			StyleSheet.Load( "RicochetHUD.scss" );
			Style.Left = Screen.Width / 2 - 246; // ( 64 (image width) + 100 (amount of margin per image) ) * 3 (amount of images) * 50% (half of screen width) = 246
			for ( int i = 0; i < RicochetPlayer.MaxDiscs; i++ )
			{
				DiscImages[i] = Add.Image( "", "image" );
			}
		}

		[Event.Tick.Client]
		public void UpdateDiscImages()
		{
			RicochetPlayer ply = Local.Pawn as RicochetPlayer;
			for ( int i = 0; i < RicochetPlayer.MaxDiscs; i++ )
			{
				if ( ply.NumDiscs < i + 1 )
				{
					SetDiscImage( i, "discgrey" );
				}
				else if ( ply.HasPowerup( Powerup.Triple ) )
				{
					SetDiscImage( i, "triple" );
				}
				else if ( ply.HasPowerup( Powerup.Fast ) )
				{
					SetDiscImage( i, "fast" );
				}
				else if ( ply.HasPowerup( Powerup.Freeze ) )
				{
					SetDiscImage( i, "freeze" );
				}
				else if ( ply.HasPowerup( Powerup.Hard ) )
				{
					SetDiscImage( i, "hard" );
				}
				else if ( ply.Team == 0 )
				{
					if ( ply.NumDiscs == RicochetPlayer.MaxDiscs )
						SetDiscImage( i, "discred2" );
					else
						SetDiscImage( i, "discred" );
				}
				else
				{
					if ( ply.NumDiscs == RicochetPlayer.MaxDiscs )
						SetDiscImage( i, "discblue2" );
					else
						SetDiscImage( i, "discblue" );
				}
			}
		}

		public void SetDiscImage( int num, string name )
		{
			DiscImages[num].SetTexture( "/ui/hud/" + name + ".png" );
		}
	}

	public class Crosshair : Panel
	{
		public Crosshair()
		{
			StyleSheet.Load( "RicochetHUD.scss" );
		}
	}

	public class RicochetKillFeed : Panel
	{
		public static RicochetKillFeed Current;

		public RicochetKillFeed()
		{
			Current = this;
			StyleSheet.Load( "RicochetHUD.scss" );
		}

		public Panel AddEntry( long lsteamid, string left, long rsteamid, string right, string method )
		{
			var e = Current.AddChild<RicochetKillFeedEntry>();
			e.Left.Text = left;
			e.Left.SetClass( "me", lsteamid == Local.Client?.PlayerId );
			e.Method.SetTexture( method );
			e.Right.Text = right;
			e.Right.SetClass( "me", rsteamid == Local.Client?.PlayerId );
			return e;
		}
	}

	public class RicochetKillFeedEntry : KillFeedEntry
	{
		public new Label Left { get; internal set; }
		public new Label Right { get; internal set; }
		public new Image Method { get; internal set; }

		public RicochetKillFeedEntry()
		{
			Left = Add.Label( "", "left" );
			Method = Add.Image( "", "image" );
			Right = Add.Label( "", "right" );
		}
	}

	public class RicochetScoreboard<T> : Panel where T : RicochetScoreboardEntry, new()
	{
		public Panel Canvas { get; protected set; }
		Dictionary<Client, T> Rows = new();

		public RicochetScoreboard()
		{
			StyleSheet.Load( "RicochetScoreboard.scss" );
			AddClass( "ricochetscoreboard" );
			Canvas = Add.Panel( "canvas" );
			Panel Header = Canvas.Add.Panel( "header" );
			Header.Add.Label( "", "name" );
			Header.Add.Label( "POINTS", "pointstext" );
			Header.Add.Label( "LATENCY", "latencytext" );
			Header.Add.Label( "VOICE", "voicetext" );
		}

		public override void Tick()
		{
			base.Tick();

			SetClass( "open", Input.Down( InputButton.Score ) );

			if ( !IsVisible )
				return;

			foreach ( var client in Client.All.Except( Rows.Keys ) )
			{
				var entry = AddClient( client );
				Rows[client] = entry;
			}

			foreach ( var client in Rows.Keys.Except( Client.All ) )
			{
				if ( Rows.TryGetValue( client, out var row ) )
				{
					row?.Delete();
					Rows.Remove( client );
				}
			}
		}

		protected T AddClient( Client entry )
		{
			var p = Canvas.AddChild<T>();
			p.Client = entry;
			return p;
		}
	}

	public class RicochetScoreboardEntry : Panel
	{
		public Client Client;
		public Label PlayerName;
		public Label Kills;
		public Label Voice;
		public Label Ping;

		public RicochetScoreboardEntry()
		{
			AddClass( "entry" );
			PlayerName = Add.Label( "PlayerName", "name" );
			Kills = Add.Label( "", "kills" );
			Ping = Add.Label( "", "ping" );
			Voice = Add.Label( "", "voice" );
		}

		RealTimeSince TimeSinceUpdate = 0;

		public override void Tick()
		{
			base.Tick();

			if ( !IsVisible )
				return;

			if ( !Client.IsValid() )
				return;

			if ( TimeSinceUpdate < 0.1f )
				return;

			TimeSinceUpdate = 0;
			UpdateData();
		}

		public void UpdateData()
		{
			PlayerName.Text = Client.Name;
			Kills.Text = Client.GetInt( "kills" ).ToString();
			Voice.Text = Client.VoiceLevel.ToString();
			Ping.Text = Client.Ping.ToString();
			SetClass( "me", Client == Local.Client );

			if ( Client != Local.Client )
			{
				Color color = ( Client.Pawn as RicochetPlayer ).TeamColor;
				PlayerName.Style.FontColor = color;
				Kills.Style.FontColor = color;
				Ping.Style.FontColor = color;
				if ( Client.VoiceLevel > 0 )
				{
					Voice.Style.FontColor = color;
				}
			}
		}
	}
}
