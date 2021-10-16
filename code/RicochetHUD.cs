using Sandbox;
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
				// TODO: Scoreboard
				RootPanel.AddChild<ChatBox>();
				RootPanel.AddChild<DiscHUD>();
				RootPanel.AddChild<Crosshair>();
				RootPanel.AddChild<KillFeed>();
			}
		}
	}
	
	public class DiscHUD : Panel
	{
		private Image Image1;
		private Image Image2;
		private Image Image3;

		public DiscHUD()
		{
			StyleSheet.Load( "RicochetHUD.scss" );
			Style.Left = Screen.Width / 2 - 246; // Image width + 100 * 3 images * 50% = 246
			Image1 = Add.Image( "", "image" );
			Image2 = Add.Image( "", "image" );
			Image3 = Add.Image( "", "image" );
			SetAllDiscImages( "/ui/hud/discblue2.png" );
		}

		[Event( "OnPowerupPickup" )]
		private void OnPowerupPickup( RicochetPlayer ply, Powerup powerup )
		{
			Log.Info( ply + " " + powerup );
		}

		public void SetDiscImage( int num, string name )
		{
			switch ( num )
			{
				case 1:
				{
					Image1.SetTexture( name );
					break;
				}
				case 2:
				{
					Image2.SetTexture( name );
					break;
				}
				case 3:
				{
					Image3.SetTexture( name );
					break;
				}
				default: break;
			}
		}

		public void SetAllDiscImages( string name )
		{
			Image1.Style.SetBackgroundImage( Texture.Load( name ) );
			Image2.Style.SetBackgroundImage( Texture.Load( name ) );
			Image3.Style.SetBackgroundImage( Texture.Load( name ) );
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
