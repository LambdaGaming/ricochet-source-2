using Sandbox;
using Sandbox.UI;

namespace Ricochet.Menu;

[UseTemplate( "/menu/Menu.html" )]
public class Menu : Panel
{
	SoundHandle MusicHandle;
	SoundHandle ClickSoundHandle;

	public void ClickSound()
	{
		ClickSoundHandle = Audio.Play( "click" );
	}

	public void Resume( Panel p )
	{
		Game.Menu.HideMenu();
	}

	public void Disconnect( Panel p )
	{
		Game.Menu.LeaveServer( "Client disconnect" );
	}

	public void CreateGame( Panel p )
	{
		//Parent.AddChild<CreateGame>();
	}

	public void FindGame( Panel p )
	{
		//Parent.AddChild<FindGame>();
	}

	public void Options( Panel p )
	{
		//Parent.AddChild<Options>();
	}

	public void Quit( Panel p )
	{
		Game.Menu.Close();
	}
}
