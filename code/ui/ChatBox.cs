using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;

namespace Ricochet;

public partial class ChatBox : Panel
{
	static ChatBox Current;

	public Panel Canvas { get; protected set; }
	public TextEntry Input { get; protected set; }

	public ChatBox()
	{
		Current = this;
		StyleSheet.Load( "/ui/ChatBox.scss" );
		Canvas = Add.Panel( "chat_canvas" );
		Input = Add.TextEntry( "" );
		Input.AddEventListener( "onsubmit", () => Submit() );
		Input.AddEventListener( "onblur", () => Close() );
		Input.AcceptsFocus = true;
		Input.AllowEmojiReplace = true;
	}

	void Open()
	{
		AddClass( "open" );
		Input.Focus();
	}

	void Close()
	{
		RemoveClass( "open" );
		Input.Blur();
	}

	public override void Tick()
	{
		base.Tick();
		if ( Sandbox.Input.Pressed( "chat" ) )
		{
			Open();
		}
	}

	void Submit()
	{
		Close();

		var msg = Input.Text.Trim();
		Input.Text = "";

		if ( string.IsNullOrWhiteSpace( msg ) ) return;

		Say( msg );
	}

	public void AddEntry( string name, string message )
	{
		var e = Canvas.AddChild<ChatEntry>();
		e.Message.Text = message;
		e.NameLabel.Text = string.IsNullOrEmpty( name ) ? "Unknown Player" : $"{name}:";
	}

	[ConCmd.Client( "chat_add_ricochet", CanBeCalledFromServer = true )]
	public static void AddChatEntry( string name, string message )
	{
		Current?.AddEntry( name, message );
		Sound.FromScreen( "talk" );
		if ( !Game.IsListenServer )
		{
			Log.Info( $"{name}: {message}" );
		}
	}

	[ConCmd.Client( "chat_addinfo_ricochet", CanBeCalledFromServer = true )]
	public static void AddInformation( string message )
	{
		Current?.AddEntry( null, message );
	}

	[ConCmd.Server( "say_ricochet" )]
	public static void Say( string message )
	{
		if ( message.Contains( '\n' ) || message.Contains( '\r' ) ) return;
		Log.Info( $"{ConsoleSystem.Caller}: {message}" );
		AddChatEntry( To.Everyone, ConsoleSystem.Caller?.Name ?? "Server", message );
	}
}
