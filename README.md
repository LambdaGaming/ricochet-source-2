# Ricochet: Source 2
 Ricochet in s&box. The main game is complete and work has now started on making small improvements and fixing bugs. Credit goes to Valve for the original game and its resources.

# Round Types
- Deathmatch - Normal deathmatch, each player is assigned a separate team. Round starts as soon as the first player joins.
- Team Deathmatch - Same as deathmatch but players are split into red and blue teams.
- Arena - 1v1 mode. Two players are selected at random at the start of the round, the rest are spectators. The round ends when one of the players die.

# Server Commands
- rc_roundtype - Type of round. 0 for deathmatch, 1 for team deathmatch, and 2 for arena. Requires server reload after changing.
- rc_minplayers - Minimum amount of players required to start an arena match.
- rc_rounds - Max rounds to play before the map changes. (Currently doesn't do anything since there's only one map right now.)

# Official Maps
- [rc_arena](https://github.com/LambdaGaming/rc_arena)
- [rc_deathmatch](https://github.com/LambdaGaming/rc_deathmatch)
- rc_deathmatch2 (WIP)

# Issues & Pull Requests
If you would like to contribute to this repository by creating an issue or pull request, please refer to the [contributing guidelines.](https://lambdagaming.github.io/contributing.html) Please keep in mind that this is intended to be a port of the original game with minor improvements, and not a sequel with major changes. I will not accept suggestions or PRs that go against this. The experimental VR mode is the only exception to this.

# Tools & Resources Used
## Code
Code was edited with Visual Studio 2022. I used the [Ricochet source code](https://github.com/ValveSoftware/halflife/tree/master/ricochet) as a reference for many things such as the entities and certain parts of the HUD. 

## Maps & Models
Models were ported using [Blender](https://www.blender.org/) and [SourceIO](https://github.com/REDxEYE/SourceIO). The best way of porting the maps that I could find was to use [Godot 3](https://godotengine.org/download/3.x/windows/) with a plugin called [GodotGoldSrcBSP](https://github.com/DataPlusProgram/GodotGoldSrcBSP). I imported the original maps into Godot using that plugin and then exported the scene as glTF. From there, I imported the glTF file into Blender, removed some unnecessary objects, and exported it as FBX at 20% scale. I then imported the FBX into hammer and manually added textures and entities.

## Textures
Textures were extracted with [GCFScape](https://valvedev.info/tools/gcfscape/) and converted into their proper format with [GIMP.](https://www.gimp.org/) Certain textures were also AI upscaled using [bigjpg.com.](https://bigjpg.com/)
