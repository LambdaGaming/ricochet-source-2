using Sandbox.UI;

namespace Ricochet
{
	public partial class RicochetHUD : Sandbox.HudEntity<RootPanel>
	{
		public RicochetHUD()
		{
			if ( IsClient )
			{
				// TODO: Scoreboard
				RootPanel.StyleSheet.Load( "RicochetHUD.scss" );
				RootPanel.AddChild<ChatBox>();
				RootPanel.AddChild<DiscHUD>();
				RootPanel.AddChild<Crosshair>();
				RootPanel.AddChild<KillFeed>();
			}
		}
	}
	
	public class DiscHUD : Panel
	{
		public DiscHUD()
		{
			// TODO: Disc images that change based on powerup and discs left
			StyleSheet.Load( "RicochetHUD.scss" );
		}
	}

	public class Crosshair : Panel
	{
		public Crosshair()
		{
			StyleSheet.Load( "RicochetHUD.scss" );
		}
	}
}
