# FPS Sample Source Code
This document gives a brief overview of main areas of the FPS Sample source code.

## Game class
The Game class (Game.cs) is a MonoBehavior that defines the main loop of the game. 
All gamecode is run from Game.Update method (with a few exceptions).
The game class is one of first classes to be instantiated and is always present no matter what state the game is in.
The Game class instantiates various systems like Audio, Input as well as containing other app level utility functionality.

The game class can be requested to update a specific _GameLoop_.
Gameloops implements various modes the game can be in.
They handle initialization and shutdown of required systems along with updating systems in the correct order. 
Active gameloop can be changed at runtime.

Currently implemented gameloop types are Server, Client and Preview

## ServerGameLoop
ServerGameLoop (ServerGameLoop.cs) handles connection to a list of clients and updates all the server systems.

## ClientGameLoop
ClientGameLoop (ClientGameLoop.cs) handles a single connection to a server and updates all client systems.

## PreviewGameLoop   
PreviewGameLoop (PreviewGameLoop.cs) is a mutation of Server and Client game loops slapped together.
It uses a mix of server and client systems as a way to preview the game without building bundles or starting up the server and client.
This becomes the gameloop when a user presses play in the editor with a game scene loaded.  

## Modules
For organizational purposes some gamecode is grouped into what we call modules. 
Modules contain components, along with the systems that update these components. The modules may also contain one or more additional specific classes that are instantiated by the gameloop. 
Modules can have a specific server, client or preview implementations.
Modules location: Assets/Scripts/Game/Modules

### ReplicatedEntity Module
Handles replication of gameobjects and ECS entities.   

To replicate a gameobject, it must be a prefab with the ReplicatedEntity component attached. 
This will ensure the prefab is registered within ReplicatedEntityRegistry (any time build tool runs: "Update Registry"). 
Server and client have different ReplicatedEntityRegistry allowing for different prefabs to be spawned on the server and client. Each representing the same entity while simultaneously utilizing the same set of serializable components. 
Components that have data to be replicated must implement the INetworkSerializable interface and be attached to the same gameobject as  ReplicatedEntity component.

To replicate an ECS entity you create a ScriptableObject deriving from ReplicatedEntityFactory. 
This class is also registered in ReplicatedEntityRegistry. It contains abstract methods that must be implemented in order to create  entities and classes with the serialize or deserialize component.    

### Player Module
Player module handles server broadcasting of player information. The sampling of user commands on the client and local handling of controlled characters.  

Server creates a *Player* entity for each connected player. 
Player entities contain data that the server sends to all connected clients (e.g. playername and what character the player is controlling). This data that is sent only to the client that the player represents (e.g. what UI elements to open)       

Each client acontains a *LocalPlayer* instantiation that has local presentation components (e.g camera control and UI).
Components specific to the type of entity that is being controlled can be added to the LocalPlayer for custom handling.
E.g. the *LocalPlayerCharacterControl* from the CharacterModule is attached to LocalPlayer to handling player controlling of character.  

### Character Module
The Character module is responsible for character abilities, animation and UI.
A character is defined by a prefab with either Character or Character1P MonoBehavior attached.
Character is used on the server and on the client for 3P characters. Whereas, Character1P is used for first person camera view.
Characters are setup using the *CharacterTypeDefinition* ScriptableObject. 
This ScriptableObject has references to the Server and Client versions of the 3P character prefab, and reference to the 1P prefab character that is instantiated in first person camera view.

All character behaviour is handled by *Abilities* (Located: Character/Behaviours/Abilities).
Abilities are actives and controlled by the *AbilityController*.
In the current implementation characters always have the movement ability active. Characters are allowed only one other ability to be active at a time (e.g. sprint, melee, shoot)   

HeroTypeAsset (HeroTypeAsset.cs) is used to setup the games hero. (note: in comments and naming the word character is often used for hero)
The HeroTypeAsset defines the stats of the hero and what character, weapons, abilities should be used.

### Item Module
Handles character items. 
Items are replicated but currently only the client versions of the prefab have any logic or visuals.
When items are updated they read the state of the relevant abilities and use those to trigger the associated effects.   

### Effect Module
Effect module controls clientside effects (gfx and sound). 
Effects are "fire and forget" this cannot be cancelled. 
This module currently supports two kinds of effects: *SpatialEffect* and *HitscanEffect*.
Effects are created within static pools at startup.  
Each effect is setup using a ScriptableObject (e.g. SpatialEffectTypeDefinition) that defines what prefab should be used for the effect as well as how large the effect pool should be.
Effects are requested by gamecode, it creates entities with the requested component (e.g SpatialEffectRequest). These are then added to the effect systems at which time the effect is then triggered.

## Grenade Module
Grenade module handles the creation and updating of grenades. Grenades are updated on the server and interpolated to the client.

### HitCollision Module
HitCollision module handles hit collision of all objects that are capable of receiving damage within the game. 
The server stores position and rotation of moving colliders so that they can be "rolled back". This allows rollingback the collider to any given tick before collision tests have been performed. 
This is used for server side lag compensation. 
HitCollision for characters can be setup in a seperate prefab so that it can be shared between the server and client prefabs. 

*RaySphereQueryReciever* can be used to queue collision queries. It first processes a raytest against the environment and then sphere casts against the hit collision. 

When hit collision is hit the *HitCollisionOwner* component is used for adding damage events.
These events are not used by the HitCollision module. Instead it is used by other systems (e.g. Character or DestructableProp) to read and clear the list and apply damage to whatever health value they have. 

This module also contains a *SplashDamageSystem* that is a fire and forget way of giving splash damage to an area.

### Projectile Module
This module processes simulation of projectiles and their associated visuals to the client.  
The server creates *Projectiles* and these are what is replicated to client side. 
When a client receives a projectile it creates a corresponding *ClientProjectile* with visuals of the projectile.  
Projectiles can also predicatively be created on the client. 
When a projectile is received from the server which matches a predicted, the predicted is deleted while the associated clientprojectile is linked to the projectile from the server.
If no match can be found the predicted is deleted together with the clientprojectile.   

### Ragdoll
Updates the state of all active ragdolls. Simulation processed by Unity physics system.

### SpectatorCam
Controls a replicated player camera, that can be controlled by the spectator. This can be used to test control of non-character units and is currently in a pretty hacky state.  

## ECS
All (almost) game-code is implemented using ECS systems, but in what we call _mixed-mode_.
As we depend on functionality that is not yet implemented in ECS we still base our code around
GameObjects and MonoBehaviors - but we try to spice things up with some entities and data components. 
We also opted for manually updating all systems as it gives us complete control over update order and is 
required when systems needed to be updated multiple times per frame (as in client side prediction).  
But expect more and more game code to go _pure_ ECS as we move along.