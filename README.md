# Ricochet: Source 2
 WIP project that aims to port Ricochet into S&Box. The plan is to make the game identical to the original, or at least as close as possible given the API and tools available. I started this project mainly to experiment with S&Box before I move onto more complex projects, so any changes that greatly differ from the original game are not planned. Credit goes to Valve for the original game and its resources.

# Round Types
- Deathmatch - Normal deathmatch, each player is assigned a separate team. Round starts as soon as the first player joins.
- Team Deathmatch - Same as deathmatch but players are split into red and blue teams.
- Arena - 1v1 mode. Two players are selected at random at the start of the round, the rest are spectators. The round ends when one of the players die.

# Server Commands
- rc_roundtype - Type of round. 0 for deathmatch, 1 for team deathmatch, and 2 for arena. Requires server reload after changing.
- rc_minplayers - Minimum amount of players required to start an arena match.
- rc_rounds - Max rounds to play before the map changes. (Currently doesn't do anything since there's only one map right now.)

# Issues & Pull Requests
 If you would like to contribute to this repository by creating an issue or pull request, please refer to the [contributing guidelines.](https://lambdagaming.github.io/contributing.html)

# Tools & Resources Used
- [Blender](https://www.blender.org/)
- [SourceIO](https://github.com/REDxEYE/SourceIO)
- [GIMP](https://www.gimp.org/)
- [bigjpg.com](https://bigjpg.com/)
- [Ricochet source code](https://github.com/ValveSoftware/halflife/tree/master/ricochet)
