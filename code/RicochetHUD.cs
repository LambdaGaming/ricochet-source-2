using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;

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
				RootPanel.AddChild<Scoreboard<ScoreboardEntry>>();
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

		public Panel AddEntry( ulong lsteamid, string left, ulong rsteamid, string right, string method )
		{
			var e = Current.AddChild<RicochetKillFeedEntry>();
			e.Left.Text = left;
			e.Left.SetClass( "me", lsteamid == Local.Client?.SteamId );
			e.Method.SetTexture( method );
			e.Right.Text = right;
			e.Right.SetClass( "me", rsteamid == Local.Client?.SteamId );
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
}
