using Sandbox.UI;

namespace Ricochet
{
	public partial class RicochetHUD : Sandbox.HudEntity<RootPanel>
	{
		public RicochetHUD()
		{
			if ( IsClient )
			{
				RootPanel.SetTemplate( "/RicochetHUD.html" );
			}
		}
	}

}
