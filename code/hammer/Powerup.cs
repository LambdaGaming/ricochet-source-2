using Editor;
using Sandbox;
using System;
using System.Threading.Tasks;

namespace Ricochet;

[Library( "powerup" ), HammerEntity]
public partial class PowerupEnt : AnimatedEntity
{
	[Net] public Powerup CurrentPowerup { get; set; } = 0;
	public bool Hidden { get; set; } = false;
	private PointLightEntity PowerupLight;

	public override void Spawn()
	{
		base.Spawn();
		SetupPhysicsFromModel( PhysicsMotionType.Static, false );
		Tags.Add( "trigger" );
		EnableSolidCollisions = false;
		EnableTouch = true;
		SetRandomPowerup();
	}

	public override void StartTouch( Entity ent )
	{
		var ply = ent as Player;
		if ( Game.IsServer )
		{
			base.StartTouch( ent );
			if ( !Hidden )
			{
				if ( ply.IsValid() )
				{
					ply.AddPowerup( CurrentPowerup );
					ply.PlaySound( "powerup" );
					Hide();
				}
			}
		}
	}

	public void Hide()
	{
		RenderColor = Color.Transparent;
		_ = WaitForRespawn();
		Hidden = true;
		PowerupLight.Delete();
	}

	public void Unhide()
	{
		SetRandomPowerup();
		RenderColor = Color.White;
		PlaySound( "pspawn" );
		Hidden = false;
	}

	async Task WaitForRespawn()
	{
		await Task.DelaySeconds( 10 );
		Unhide();
	}

	private void SetRandomPowerup()
	{
		Random rand = new();
		Array powerups = Enum.GetValues( typeof( Powerup ) );
		Powerup powerup = ( Powerup ) powerups.GetValue( rand.Next( powerups.Length ) );
		CurrentPowerup = powerup;
		SetModel( GetPowerupModel() );
		PowerupLight = new PointLightEntity
		{
			Color = GetLightColor(),
			LightSize = 0.01f,
			Brightness = 0.1f,
			Position = Position,
			Parent = this
		};
	}

	private string GetPowerupModel()
	{
		return CurrentPowerup switch
		{
			Powerup.Fast => "models/pow_fast/pow_fast.vmdl",
			Powerup.Freeze => "models/pow_freeze/pow_freeze.vmdl",
			Powerup.Hard => "models/pow_hard/pow_hard.vmdl",
			Powerup.Triple => "models/pow_triple/pow_triple.vmdl",
			_ => "models/pow_fast/pow_fast.vmdl",
		};
	}

	private Color GetLightColor()
	{
		return CurrentPowerup switch
		{
			Powerup.Fast => Color.Green,
			Powerup.Freeze => Color.Cyan,
			Powerup.Hard => Color.Red,
			Powerup.Triple => Color.Magenta,
			_ => Color.Black,
		};
	}
}
