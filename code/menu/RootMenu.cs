using Sandbox;
using Sandbox.Menu;
using Sandbox.UI;

namespace Ricochet.Menu;

public class RootMenu : RootPanel, IGameMenuPanel
{
	public static RootMenu Instance;
	
	public RootMenu()
	{
		StyleSheet.Load( "menu/menu.scss" );
		Instance = this;
		AddChild<Menu>();
	}

	public override void Tick()
	{
		SetClass( "ingame", Game.InGame );
		SetClass( "notingame", !Game.InGame );
	}
}
