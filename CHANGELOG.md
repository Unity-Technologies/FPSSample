# Changelog

This file contains a summary of the changes that went into each release.

## [0.2.0] - 2018-11-29
- Removed “Update Registries” button from Project Tools. Prefabs and scriptable objects that should be referenced by a registry now each have custom inspector that is used to register them.
- Removed support for client and server specific versions of the same replicated prefab. 
- Characters now all use the same replicated character prefab (character.prefab). Different non-replicated presentation entities (with CharPresentation component) are created on server and client depending on hero type and view mode (1P or 3P). Characters require at least one CharPresentation that is used to update the animation state. Item presentation (skeleton, mesh and effects) are also setup as character presentation.
- Character behaviours (or abilities - naming is quite a mess atm) are now instantiated as a separate replicated entity (DefaultCharBehaviourController). This creates various sub behaviors, but all replication is handled by DefaultCharBehaviourController.
- Items are now created as replicated entities. They only contains two behaviours that are attached (in a currently hacky way) to the characters DefaultCharBehaviourController.
- All character behaviour data are now IComponentData attached to “pure” ECS entities.
- ReplicatedEntity property predictingPlayerId is now replicated. This is used to define what client should predict a given replicated entity. Clients add ServerEntity component to replicated entities it receives where predictingPlayerId is its on playerid (before this was done by client using controlled entity reference)
- Various editor tools have been moved to separate tools folder
- Updated Entities package to 0.0.12-preview.18 and package is no longer internalized.
- Melee can now be triggered from sprint
- Added simple emote framework. Emotes can be triggered by buttons J and K. Still work in progress
- Removed all the old UNet stuff
- Made `server.maxclients` actually work.
- Added a serverlist to the main menu. List uses Unity Server Query Protocol (USQP) to check servers for game mode, number of players etc.
- Switched to Unity2018.3b12 allowing enabling late sync for a good speedup on some configs.
- Added GDRP compliance button (only works if built with a valid project id)
- Fixed bugs where a failed join would leave client in bad state (unable to connect again)
- Fix for `disconnect` console command not working. Added leave game option to ingame menu.
- Some efforts to make movement more calm in 3P.
- 3P movement: Re-works squash node, especially on direction change
- 3P movement: Softens transition speeds and adds central animation in move blend space
- 3P movement: Adds delay to to animation state (vs. game state) when transitioning from loco to standing
- 3P movement: Turns down the characters acceleration (capsule)
- FPS Projection of weapons is now done in a shader graph shader.

## [0.1.1] - 2018-10-22
- Fix for headless build not running on some machines (Mominon)
- Changed boot behaviour. Now always read boot.cfg (previously named game.cfg) unless -noboot passed.
- Added documentation about small tools in editor
- Fix for exec command giving scary sounding warnings
- Fix for project using Perforce by default
- Tweak to animation for slightly smoother 3rd person
- Fixes to SourceCode doc (Badger0101)
- Changed Tick error message to just be info
- Type fixes (jfmc)
- Updated matchmaker code 
- Added information about contribution
- Improved documentation about animation
- Fix for linux version of headless server not working with redict of in-/output

## [0.1] - 2018-10-22
- First public release, Unite L.A. 2018
