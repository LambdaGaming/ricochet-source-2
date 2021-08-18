using Sandbox.UI;
using Sandbox.UI.Construct;

namespace Ricochet
{
	public partial class RicochetHUD : Sandbox.HudEntity<RootPanel>
	{
		public RicochetHUD()
		{
			if ( IsClient )
			{
				// TODO: Death notifications and scoreboard
				RootPanel.StyleSheet.Load( "RicochetHUD.scss" );
				RootPanel.AddChild<ChatBox>();
				RootPanel.AddChild<DiscHUD>();
				RootPanel.AddChild<Crosshair>();
			}
		}
	}
	
	public class DiscHUD : Panel
	{
		public DiscHUD()
		{
			StyleSheet.Load( "RicochetHUD.scss" );
			for ( int i = 0; i < 3; i++ )
			{
				var img = Add.Panel( "image" );
				img.Style.Background = new PanelBackground()
				{
					Texture = Sandbox.Texture.Load( "/gfx/hud/640_freeze.tga" )
				};
			}
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
