# FPS Sample Source Code
This document gives a brief overview of main areas of the FPS Sample source code.

## Game class
The Game class (Game.cs) is a MonoBehavior that defines the main loop of the game. 
All gamecode is run from Game.Update method (with a few exceptions).
The game class is one of first classes to be instantiated and is always present no matter what state the game is in.
The Game class instantiates various systems like Audio and Input and has other app level utility functionality.

The game class can be requested to update a specific _GameLoop_.
Gameloops implements various mode the game can be in.
They handle initialization and shutdown of required systems and updates systems in correct order. 
Active gameloop can be changed at runtime.

Currently implemented gameloop types are Server, Client and Preview

## ServerGameLoop
ServerGameLoop (ServerGameLoop.cs) handles connection to a list of clients and update all server systems.

## ClientGameLoop
ClientGameLoop (ClientGameLoop.cs) handles a single connection to a server and updates all client systems.

## PreviewGameLoop   
PreviewGameLoop (PreviewGameLoop.cs) is mutations of Server and Client game loops slapped together.
It uses a mix of server and client systems and is used as a way to preview game without building bundles and starting up server and client.
This is the gameloop that is used when user presses play in editor with a game scene loaded.  

## Modules
For organizational purposes some gamecode is grouped into what we call modules. 
Modules contains components and the systems that update them and one or more module class that what is instantiated from the gameloop. 
Modules can have specific server, client or preview implementations.
Modules are found in Assets/Scripts/Game/Modules

### ReplicatedEntity Module
Handles replication of gameobjects and ECS entities.   

To replicate a gameobject it needs to be a prefab and have the ReplicatedEntity component attached. 
This will ensure prefab is registered in ReplicatedEntityRegistry (when build tool "Update Registry" is run). 
Server and client have different ReplicatedEntityRegistry allowing for different prefabs to be spawned on server and client each representing the same entity (but they both need same set of serializable components)   
Components that has data that should be replicated needs to implement the INetworkSerializable interface and be attacheds to same gameobject as ReplicatedEntity component.

To replicate an ECS entity you need to create a scriptable object deriving from ReplicatedEntityFactory. 
This class is also registered in ReplicatedEntityRegistry and has abstracts methods that needs to be implemented to create entity and create classes that serializes/deserializes component    

### Player Module
Player module handles server broadcasting of player information, sampling of user commands on client and local handling of controlled characters.  

Server creates a *Player* entity for each connected player. 
Player entity contains data that server sends to all clients (e.g. playername and what character the player is controlling) and data that is sent only the client the player represents (e.g. what UI elements to open)       

Each client also have a *LocalPlayer* instantiated that has local presentation components (e.g camera control and UI).
Components specific to the type of entity being controlled can be added to LocalPlayer for custom handling.
E.g. the *LocalPlayerCharacterControl* from the CharacterModule is attached to LocalPlayer to handling player controlling of character.  

### Character Module
Character module handles character abilities, animation and UI.
A character is define by prefab with either Character or Character1P MonoBehavior attached.
Character is used on server and on client for 3P characters whereas Character1P is used for 1P view of character.
Characters are setup using the *CharacterTypeDefinition* scriptable object. 
It has references to Server and Client versions of the 3P character prefab, and reference to the 1P prefab character that is instantiated in 1P view.

All character behaviour is handled by *Abilities* (in Character/Behaviours/Abilities).
What abilities are active is controlled by the *AbilityController*.
In current implementation characters always have the movement ability active and allow for only one other ability to be active (e.g. sprint, melee, shoot)   

HeroTypeAsset (HeroTypeAsset.cs) is used to setup a game hero. (note: in comments and naming the word character is often used for hero)
The HeroTypeAsset defines the stats of the hero and what character, weapons, abilities should be used.

### Item Module
Handles character items. 
Items are replicated but currently only client versions of prefabs have any logic or presentation.
When items are updated they read state of relevant abilities and uses that to trigger effects.   

### Effect Module
Effect module is responsible for showing short clientside effects (gfx and sound). 
Effects are "fire and forget" and cannot be canceled. 
Currently supports two kinds of effects: *SpatialEffect* and *HitscanEffect*.
Effects are created in a static pools at startup and reused.  
Each effect is setup using a scriptable object (e.g. SpatialEffectTypeDefinition) that defines what prefab should be used for the effect and how large effect pool should be.
Effect are requested by gamecode by creating entities with request component (e.g SpatialEffectRequest) that are then picked up by effect systems and effct is triggered.

## Grenade Module
Grenade module handles creation and updating of grenades. Grenades is updated on server and interpolated on client.

### HitCollision Module
HitCollision module handles hit collision of all objects that can receive damage in the game. 
Server stores position and rotation of moving colliders so they can be "rolled back" to a given tick before collision tests are performed. 
This is used for server side lag compensation. 
HitCollision for characters can be setup in a seperate prefab so it can be shared betweem server and client prefab 

*RaySphereQueryReciever* can be used to queue collision queries that first does a raytest against environment and then sphere cast against hit collision. 

When hit collision is hit the *HitCollisionOwner* component can be used to add damage events.
These events are not used by the HitCollision module, but it is up to some other system (e.g. Character or DestructableProp) to read and clear list and apply damage to whatever health presentation they have. 

Module also contains *SplashDamageSystem* that is an fire and forget way of giving splash damage to an area

### Projectile Module
Handles simulation of projectiles and visualization of projectiles on clients.  
Server creates *Projectiles* that are replicated to clients. 
When client receives projectile it creates a corresponding *ClientProjectile* that contains visualization of projectile.  
Projectiles can also predicatively be created on client. 
When a projectile is received from server that matches a predicted, the predicted is deleted and the associated clientprojectile is linked to the projectile from the server.
If no match can be found the predicted is deleted together with the clientprojectile.   

### Ragdoll
Handles updating state of all active ragdolls. Simulation done by Unity physics system.

### SpectatorCam
Handles a replicated player controllable camera. This has been used to test controlling non-character units and is currently in a pretty hacky state.  



