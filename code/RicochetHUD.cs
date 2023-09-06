using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System.Collections.Generic;
using System.Linq;

namespace Ricochet;

public partial class RicochetHUD : HudEntity<RootPanel>
{
	public RicochetHUD()
	{
		if ( Game.IsClient )
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
		AddClass( "dischud" );
		Panel Canvas = Add.Panel( "canvas" );
		for ( int i = 0; i < RicochetPlayer.MaxDiscs; i++ )
		{
			DiscImages[i] = Canvas.Add.Image( "", "image" );
		}
	}

	[GameEvent.Tick.Client]
	public void UpdateDiscImages()
	{
		RicochetPlayer ply = Game.LocalPawn as RicochetPlayer;
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
		Add.Image( "/ui/hud/icons/crosshairs.png" );
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
		e.Left.SetClass( "me", lsteamid == Game.LocalClient?.SteamId );
		e.Method.SetTexture( method );
		e.Right.Text = right;
		e.Right.SetClass( "me", rsteamid == Game.LocalClient?.SteamId );
		return e;
	}
}

public class RicochetKillFeedEntry : Panel
{
	public Label Left { get; internal set; }
	public Label Right { get; internal set; }
	public Image Method { get; internal set; }
	public RealTimeSince TimeSinceBorn = 0;

	public RicochetKillFeedEntry()
	{
		Left = Add.Label( "", "left" );
		Method = Add.Image( "", "image" );
		Right = Add.Label( "", "right" );
	}

	public override void Tick()
	{
		base.Tick();
		if ( TimeSinceBorn > 6 )
		{
			Delete();
		}
	}
}

public class RicochetScoreboard<T> : Panel where T : RicochetScoreboardEntry, new()
{
	Panel Canvas { get; set; }
	Dictionary<IClient, T> Rows = new();

	public RicochetScoreboard()
	{
		StyleSheet.Load( "RicochetScoreboard.scss" );
		AddClass( "ricochetscoreboard" );
		Canvas = Add.Panel( "canvas" );
		Panel Header = Canvas.Add.Panel( "header" );
		if ( Ricochet.CurrentRound is DeathmatchRound )
		{
			Button spectator = Header.Add.Button( "TOGGLE SPECTATOR MODE", () => ConsoleSystem.Run( "toggle_spectator" ) );
		}
		Header.Add.Label( "", "name" );
		Header.Add.Label( "POINTS", "pointstext" );
		Header.Add.Label( "LATENCY", "latencytext" );
		Header.Add.Label( "VOICE", "voicetext" );
	}

	public override void Tick()
	{
		base.Tick();

		SetClass( "open", Input.Down( "score" ) );

		if ( !IsVisible )
			return;

		foreach ( var client in Game.Clients.Except( Rows.Keys ) )
		{
			var entry = AddClient( client );
			Rows[client] = entry;
		}

		foreach ( var client in Rows.Keys.Except( Game.Clients ) )
		{
			if ( Rows.TryGetValue( client, out var row ) )
			{
				row?.Delete();
				Rows.Remove( client );
			}
		}
	}

	protected T AddClient( IClient entry )
	{
		var p = Canvas.AddChild<T>();
		p.Client = entry;
		return p;
	}
}

public class RicochetScoreboardEntry : Panel
{
	public IClient Client;
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
		Voice.Text = Client.Voice.CurrentLevel.ToString();
		Ping.Text = Client.Ping.ToString();
		SetClass( "me", Client == Game.LocalClient );
		
		var ply = Client.Pawn as RicochetPlayer;
		Color color = ply.IsSpectator ? Color.White : ply.TeamColor;
		if ( Client != Game.LocalClient )
		{
			PlayerName.Style.FontColor = color;
			Kills.Style.FontColor = color;
			Ping.Style.FontColor = color;
			if ( Client.Voice.CurrentLevel > 0 )
			{
				Voice.Style.FontColor = color;
			}
		}
		else
		{
			Style.BackgroundColor = color.WithAlpha( 0.05f );
		}
	}
}
