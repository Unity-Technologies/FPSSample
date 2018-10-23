# Getting Started Guide

## The game

The FPS Sample is a multiplayer only game. There is no single player mode and
you cannot play the game without being connected to a server. It is a pretty
traditional shooter game with two different characters: The Terraformer and
The Robot. Each have their own weapon with a primary and secondary fire mode.
There are two game modes:

1. Team deathmatch

    Two teams fights for frags. Winner is team with most scores when time is up.

2. Assault mode

    One team is attacking, another is defending. Attackers win by capturing all 3 bases before time runs out. Defenders win by preventing that. Attackers can capture a base by having one or more players in the base. Defenders can take capture progress back by being in the base. Once a base is captured completely the battle proceeds to the next base.

There are two levels in the game. The primary level is built for Assault mode. It is called Level_01. The other level is mainly for testing purposes and is much smaller both in size and assets. It is called Level_00.

## Playing the game

If you have a full build of the game, it will launch into the menu and you can either
connect to a server or create a new game. If you create a new game, it will launch a
server on your machine. If your firewall settings permit, other people on your LAN
can now connect to your IP address and play.

Key | Function
--- | --- 
WASD | Player movement
Shift | Sprint
Mouse | Look
LeftClick | Primary fire
RightClick | Secondary fire
Space | Jump
V | Melee attack
H | When in captured base or spawn: change character
Enter | Open chat
Tab | Show scores
ESC | Menu
F1 | Open console
Alt+Enter | Toggle full screen

## Understanding the workflow: Standalone and AssetBundles

Working with a multiplayer game in Unity means you will be working a lot with the
standalone player. To make a client and a server that talks over a network connection there
has to be two processes. To make this workflow as frictionless as possible, we
use assetbundles for all the content (levels and characters etc.). 
The only thing that goes into the standalone player is the code and a single, very small, bootstrapper scene. 
Only if you have made changes to a level or a prefab do you have to rebuild the assetbundles. 
(And you can rebuild selectively -- to some degree.)

The Project Tools window is used to make this workflow function in practice. The most commonly used functions here are:

Button | Function
--- | --- 
__Open__ | Open all the scenes that make up a level
__Levels [force]__ | Build all the levels into bundles
__Assets [force]__ | Build all prefabs into bundles (players etc)
__All [force]__ | Build all levels and prefabs into bundles
__Build game__ | Build code and bootstrapper scene
__Run__ | Start game in _boot mode_. No level loaded, only bootstrapper.
__Open build folder__ | Open the folder containing the standalone player
__Update Registry__ | Updates the registry used when building prefab bundles. 

Updating the registry only needs to be done when new game elements (ect. Character, Replicated entity, Hero definition) 
are added to the project  


From the _boot mode_ a standalone player can enter other modes. Some examples:

This will enter preview mode on level_01. This is equivalent to pressing play in the editor when level_01 is open.
```
preview level_01
```

The following will enter client mode and connect to an ip address
```
connect 127.0.0.1
```

And finally, we can start a server by typing
```
serve level_00
```

These are the main modes. In practice one will most often use the __Quick Start__ section on the
Project Tools window to launch different combinations of clients and servers. The way this works is that _command line arguments_ are passed on to the standalone player. Any command line argument
that is prefixed with `+` will become an command on the console. At the very bottom of the
Project Tools window the actual command line arguments are shown. This is a good place to learn
how the quick start tool functions.

## The Console, Commands and Vars

To open the console at any time, press __F1__.

The console support __commands__ and __variables__. As an example the command `quit` will quit the game. The console has __tab__ completion. Here are some of the commonly used commands:


Command | Function
--- | --- 
`help` | Show list of all commands
`serve <levelname>` | Enter servermode with the named level
`connect <host>` | Enter client mode and connect to host
`preview <levelname>` | Enter preview mode for testing levelname
`nextchar` | Toggles character
`exec <file>` | Executes the commands in file as if they were typed on console
`respawn` | Force a respawn
`thirdperson` | Toggle thirdperson view (for debugging)
`runatserver <command>` | Executes the command on the server's console
`vars` | Show all vars

There are many more variables than commands. Here are a some of the most commonly used

Variable | Function
--- | --- 
`client.debug` | Use 1, 2, 3 to get lots of spam about client networking
`client.matchmaker` | Set this to the matchmaker `host:port` endpoint
`client.playername` | Your visible player name
`client.updaterate` | Max rate (bytes/sec) client wants from server (30000)
`client.updatesendrate` | Rate (packets/sec) client wants from server (20)
`config.fov`| Field of view
`config.mousesensitivity` | Mouse sensitivity
`game.assault.minplayers` | Players needed before starting assault round
`game.assault.roundlength` | Length of a game round in seconds
`game.dm.minplayers` | Players needed before starting deathmatch round
`game.dm.roundlength` | Length of a game round in seconds
`game.modename` | Can be either `deathmatch` or `assault`
`net.stats`| Set to > 0 to get network stats/graphs
`r.vsync` | Number of vblanks to sync to. 0 for no sync
`r.resolution` | Current resolution, e.g. `1920x1080`
`server.port` | The port to listen on (7912)
`server.quitwhenempty` | Set to 1 to make server quit when last player leaves
`server.recycleinterval` | If > 0 server will shut down after this seconds when no players
`server.sqp_port` | Server Query Protocol poert (7912)
`server.tickrate` | The tickrate of the server (and client)
`show.compactstats` | Set to 1 to show FPS and RTT in top left corner
`show.fps` | Show some fps stats if > 0
`sound.debug` | Show debug info about sounds

## Making builds

To make a full build of the game, there is some menu items under __FPS Sample__ > __BuildSystem__. Use __CreateBuildWindows64__ to make a full windows build, for example.

These menu items are directly available as functions that can be called from the commandline of Unity if you want to integrate with a build machine.

