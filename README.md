# fps.sample

This is a fully functional, first person multiplayer shooter game made in Unity and with full source and assets.
It is in active development by a small team from Unity Technologies. The goals are to test and showcase new
features in unity and to be of use for teams who can bootstrap on top of this, extract useful bits or simply
learn from and get inspired by what is in the project.

## Status and prerequisites

Current status at a glance:
```
Required Unity: 18.3 beta 7
Supported platforms: Windows and Linux (server only)
```

## Getting started

The following guide should take you to the point where
you can hit play in the editor and run around the levels and also build a
standalone version of the game and connect a few clients with a server.

### Getting the project

There are 2 ways you can get the project. You can download a release.
Or you can clone the project. Note, that 

> *IMPORTANT*: 
> This project uses Git Large files. Downloading a zip file using the green button on Github
> WILL NOT WORK. You MUST clone the project or download a release from the releases tab.

### Getting the right version of Unity

Once you have downloaded a release or cloned the repository, it is recommended you install
the exact version of Unity that the project was last updated to. Currently that means

> *You need Unity 2018.3b7*

### Opening the project for the first time

The first time you open the project you need patience! Once the editor is ready,

> open the project tools window: fps.sample | Windows | Project tools

Keep this window docked as you will use it a lot. From here you can open the levels, build assetbundles
and build standalone players. Because this is a multiplayer game you will need to work
with standalone players a lot.

From the Project tools window hit

> open Level_00 

You should now be able to press

> Play in the editor

and enter playmode in 'preview' state. This means you can run around and test
the leve, but no multiplayer is enabled.

Back in the editor, press

> "All \[force\]" 

in the Project Tools window. This will build
the levels and other assets into assetbundles. The first time this will also take a significant
amount of time as all shaders have to be compiled.

Finally press

> "Build game"

from the Project Tools window. This builds the standalone player.
Again, first time is slow.

After all this is done, find the "Quick start" section in the Project Tools window. Select "Multiplayer" mode
and "Level_00" as level, 1 client, Headless server Checked, Unused editor and then press the green
"Start" button. This should launch two processes: one is a standalone,
headless server, the other is a client that will attempt to connect to the
server.

If all of this works, by all means celebrate!

## More information

Check out the `Documentation` folder for more information.

## License

This project is distributed under the Unity Companion License. The full text
is found in here: [LICENSE.md](LICENSE.md). 
