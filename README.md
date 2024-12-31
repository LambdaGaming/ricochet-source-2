# Ricochet: Source 2
 This is a port of the Valve game Ricochet, with additional features such as VR support and minor QoL improvements. This was a project I started to get familiar with the development process of s&box. I didn't really have any plans to finish it at first, but that's what ended up happening. There are also a couple things missing, including the original playermodel and certain particle effects.  
 
 The project was abandoned in late 2023 after Facepunch announced that they would be removing the entity and client/server systems in favor of more modern systems, similar to what Unity uses. I wasn't happy with this change since I was already so familiar with the Source ecosystem, and I didn't have the motivation to make the code work with these new systems. On top of that, Valve forced the developers of Team Fortress: Source 2 (which I was not affiliated with) to take down their project around the same time as Facepunch's announcement, so that didn't help things either.  
 
 As you might expect, the game is completely unplayable in current versions of s&box. In fact, the game hasn't worked properly since around mid 2023. After that, Facepunch stopped fixing bugs related to systems that they would end up gutting by the end of the year. It *might* be possible to get it running on the January 2023 beta branch with a few tweaks, but I won't be providing any support for that. This repo only exists for reference purposes at this point.

# Round Types
- Deathmatch - Normal deathmatch, each player is assigned a separate team. Round starts as soon as the first player joins.
- Team Deathmatch - Same as deathmatch but players are split into red and blue teams.
- Arena - Organized team battles. Two teams of players are randomly selected at the start of the round, and the rest are spectators. If there was a previous match with a winner, the winning team will play again. The round ends when all players on one team die.

# Server Commands
- rc_roundtype - Type of round. 0 for deathmatch, 1 for team deathmatch, and 2 for arena. Requires server reload after changing.
- rc_playersperteam - Amount of players that should be on each team during an arena round.
- rc_rounds - Max arena rounds to play before the map changes.
- rc_allowvr - Allow VR players to join the game

# VR Mode
VR is now supported, but it is currently experimental. The game should be fully playable but do not expect a refined experience that is equal to or better than playing normally. There are a few things to make note of before playing in VR:
- This game involves quickly jumping between platforms and falling into a void, so if you are afraid of heights or get easily motion sick, you probably shouldn't play this in VR.
- This game was designed to be played standing or seated. Room-scale works but is not fully supported, so walking away from the center of your play area might cause weird things to happen.
- Left trigger fires the hard disc, and right trigger fires the regular disc. Your hands will be the models of these discs so you can tell the difference.
- Discs are launched from the position of the player's hand, instead of the center of their body like normal. This means that VR players have a slight advantage since they can fire above and below the bouncers that normally deflect discs.
- VR players can play with normal players, but servers have the option to not allow VR players to join due to the advantage mentioned above.

# Tools & Resources Used
## Code
Code was edited with Visual Studio 2022. I used the [Ricochet source code](https://github.com/ValveSoftware/halflife/tree/master/ricochet) as a reference for many things such as the entities and certain parts of the HUD. 

## Maps & Models
Models were ported using [Blender](https://www.blender.org/) and [SourceIO](https://github.com/REDxEYE/SourceIO). The best way of porting the maps that I could find was to use [Godot 3](https://godotengine.org/download/3.x/windows/) with a plugin called [GodotGoldSrcBSP](https://github.com/DataPlusProgram/GodotGoldSrcBSP). I imported the original maps into Godot using that plugin and then exported the scene as glTF. From there, I imported the glTF file into Blender, removed some unnecessary objects, and exported it as FBX at 20% scale. I then imported the FBX into hammer and manually added textures and entities.

## Textures
Textures were extracted with [GCFScape](https://valvedev.info/tools/gcfscape/) and converted into their proper format with [GIMP.](https://www.gimp.org/) Certain textures were also AI upscaled using [bigjpg.com.](https://bigjpg.com/)
