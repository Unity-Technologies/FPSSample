# Changelog

This file contains a summary of the changes that went into each release.

## [0.3.1] - 2019-03-11

- Fixed changelog not being properly updated (missing entries under 0.3.0 heading)
- Updated to Unity 2018.3.8f1
- Updated to latest Matchmaker example code

## [0.3.0] - 2019-02-28

- Updated HDRP to version 4.6
- `net.stats 4` now show breakdown of incoming update packets
- Updated to Unity 2018.3f2
- Fix for too agressive framenting of network packets (would fragment before packet was full)
- Improved compression of Schemas (sent out the clients the first time a new entity type is spawned)
- Fixed network tests that were broken at last release
- Added `server.disconnecttimeout` to allow tuning disconnect time
- New type of headless client, "thinclient", to enable stresstesting of server. Start e.g. using `-batchmode -nographics +thinclient +debugmove 1 +connect localhost +thinclient.requested 16` to get 16 client connections.
- Changed `client.updatesendrate` (number of updates from server per second) to `client.updateinterval` (time in simticks between updates from server). Old default was 20 updates/sec and new default is 3; with tickrate of 60 default is thus unchanged.
- The oddly named `owner` to `server` in ServerConnection
- Tweaks to Linux build steps to align with needs of Multiplay (naming etc.)
- Game now looks for `-port` and `-query_port` for port numbers for game resp server query port.
- Lots of optimizations to delta compression and snapshot generation on server
- Redid all the old particles in with cool new Visual Effect Graph
- Fix for crashes in netcode when client had very long stalls
- Converted all of the UI to use Text Mesh Pro
- Server Query Protocol now defaults to the port offset (+10) used by Multiplay
- Fix and from (thanks: carlself) removing hang when joining an 'old' server
- Fix for some elements of menu sometimes becoming unresponsive
- UNITY_DISABLE_AUTOMATIC_SYSTEM_BOOTSTRAP no longer set in project settings. This means ECS systems automatically starts and runs in edit mode (Unless [DisableAutoCreation] attribute is set, which it is for most of our game systems)
- HitCollision no longer uses Unity collision system for queries against moving colliders. For lag compensation server needs to do queries using different historical data and moving Unity physics colliders multiple times per frame is very inefficient. Now queries are handled using custom collision primitives and burst jobs. The structure of the collider and history data is still WIP.
- Unified how ECS components are serialized. Serialized components should now implement either IReplicatedComponent, IPredictedComponent or IInterpolatedComponent and will automatically be serialized appropriately (if attached to entity with replicatedentity component). IMPORTANT: It is currently also required to define a static methods _CreateSerializerFactory_ that returns a serializer factory for the component type. This is something we are working on getting rid of.
- Removed support for replicated monobehavior. All serialization is now performed by ECS component.
- Config var debugmove 2  now also makes character shoot secondary weapon
- UserCommand buttons now uses single int property together with enum to defined pressed buttons. 
- Removed sprint button from UserCommand. This is now handled by button Ability1.
- Removed DebugPrimitive module from project. Not used and needed cleanup.
- Added VFXSystem that manages VFX assets. Currently has interface for starting spatial and hitscan effects. 
- SpatialEffect and HitscanEffect now uses VFX system. Pools of effect prefabs are no longer created as effects can be triggered without gameobject instantiation (Yay!)
- SpatialEffect and HitscanEffect registries removed.
- Character properties that should be replicated to all clients have been moved to to new ECS component CharacterRepAll
- Replicated MonoBehavior components converted to ECS components. Including GameEntityType, ReplicatedEntity, RagdollState, SpectatorCam, DamageHistory, HealthState, UserCommand, HitCollisionOwner , TeleporterPresentation, Moveable.
- GameMode, PlayerState and CapturePoint are now replicated through an ECS component (they need to stay MonoBehavior as they contain strings properties and we don’t have good solution for storing strings on data components atm)
- All abilities now have a request phase that is executed before movement update. This allows for multiple different movement abilities (before Ability_Movement was always active)
- Renamed DefaultBehaviorController to AbilityCollection. AbilityCollection now has a general way to handle all abilities (before there where hardcoded handling of e.g. movement and death). All abilities are registered in one list and what abilities are active are determined by their requests (acquired in the request phase) and rules for what other abilities an ability can run together with and what it can interrupt (This is setup on AbilityCollection) 
- Setup of buttons that should trigger an ability is now setup on each individual ability scriptable object.
- Item registry removed (Items are not currently being replicated - they will probably be when weapon switching is implemented. All abilities are owned and replicated by AbilityCollection)
- Added new module called Presentation. This is responsible for creating and attaching presentation geometry and logic to replicated entities. Entities can have different presentation depending on platform (e.g. Server, Client) and other properties. Only grenades use this atm. 
- Grenades are now replicated as pure ECS entity. Client presentation handled by Presentation module.
- Fixed error spam from BuildWindow after deleting scene. Build window now regenerates level list when it finds invalid level info.
- Registries are now automatically generated before bundled are build (Manual generation can be triggered from _FPS Sample->Registries->Prepare Registries_). Manual fix-up of registries no longer needed after deleting registered objects.
- ReplicatedEntityFactories are now stored in bundles. The goal is to generalize creation of entities (from prefabs or ReplicatedEntityFactory). Note: ReplicatedEntityFactory is currently used to create pure entities, but a prefab like workflow might be used in the future.   
- WeakAssetReference are now blitable (struct and guid saved as ints) and can be stored on pure ECS components.


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
