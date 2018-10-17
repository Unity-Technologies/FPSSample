Sample.fps Modules
=======

For organizational purposes some gamecode is grouped into what we call modules. 
Modules contains components and the systems that update them and one or more module class that what is instantiated from the gameloop. 
Modules can have specific server, client or preview implementations.

##ReplicatedEntity
Handles replication of gameobjects and ECS entities.   

To replicate a gameobject it needs to be a prefab and have the ReplicatedEntity component attached. 
This will ensure prefab is registered in ReplicatedEntityRegistry (when build tool "Update Registry" is run). 
Server and client have different ReplicatedEntityRegistry allowing for different prefabs to be spawned on server and client each representing the same entity (but they both need same set of serializable components)   
Components that has data that should be replicated needs to implement the INetworkSerializable interface and be attacheds to same gameobject as ReplicatedEntity component.

To replicate an entity you need to create a scriptable object deriving from ReplicatedEntityFactory. 
This class is also registered in ReplicatedEntityRegistry and has abstracts methods that needs to be implemented to create entity and create classes that serializes/deserializes component    

##Player
Player module handles server broadcasting of player information, sampling of user commands on client and local handling of controlled characters.  

Server creates a *Player* entity for each connected player. 
Player entity contains data that server sends to all clients (e.g. playername and what character the player is controlling) and data that is sent only the client the player represents (e.g. what UI elements to open)       

Each client also have a *LocalPlayer* instantiated that has local presentation components (e.g camera control and UI).
Components specific to the type of entity being controlled can be added to LocalPlayer for custom handling.
E.g. the *LocalPlayerCharacterControl* from the CharacterModule is attached to LocalPlayer to handling player controlling of character.  

##Character Module
Character module handles character abilities, animation and UI.
All character behaviour is handled by *Abilities*. Currently characters always have the movement ability active and allow for only one other ability to be active (e.g. sprint, melee, shoot)   
Each character is defined by 3 prefabs. Client version (3P), Server version (3P) and a 1P version.  
The first two represents the client and server version of the same replicated entity. 
The 1P version is created on clients when controlling a character.  

##Item 
Handles character items. 
Items are replicated but currently only client versions of prefabs have any logic or presentation.  

##Effect Module
Effect module is responsible for showing short clientside effects (gfx and sound). 
Effects are "fire and forget" and cannot be canceled. 
Currently supports two kinds of effects: *SpatialEffect* and *HitscanEffect*.
Effects are created in a static pools at startup and reused.  
Each effect is setup using a scriptable object (e.g. SpatialEffectTypeDefinition) that defines what prefab should be used for the effect and how large effect pool should be.
Effect are requested by gamecode by creating components (e.g SpatialEffectRequest) that are then picked up by effect systems and effct is triggered.

##Grenade
Grenade module handles creation and updating of grenades. Grenades is updated on server and interpolated on client.

#HitCollision
HitCollision module handles hit collision of all objects that can receive damage in the game. 
Server stores position and rotation of moving colliders so they can be "rolled back" to a given tick before collision tests are performed. 
This is used for server side lag compensation. 
HitCollision for characters can be setup in a seperate prefab so it can be shared betweem server and client prefab 

*RaySphereQueryReciever* can be used to queue collision queries that first does a raytest against environment and then sphere cast against hit collision. 

When hit collision is hit the *HitCollisionOwner* component can be used to add damage events.
These events are not used by the HitCollision module, but it is up to some other system (e.g. Character or DestructableProp) to read and clear list and apply damage to whatever health presentation they have. 

Module also contains *SplashDamageSystem* that is an fire and forget way of giving splash damage to an area

##Projectile
Handles simulation of projectiles and visualization of projectiles on clients.  
Server creates *Projectiles* that are replicated to clients. 
When client receives projectile it creates a corresponding *ClientProjectile* that contains visualization of projectile.  
Projectiles can also predicatively be created on client. 
When a projectile is received from server that matches a predicted, the predicted is deleted and the associated clientprojectile is linked to the projectile from the server.
If no match can be found the predicted is deleted together with the clientprojectile.   

##Ragdoll
Handles updating state of all active ragdolls. Simulation done by Unity physics system.

##SpectatorCam
Handles a replicated player controllable camera. This has been used to test controlling non-character units and is currently in a pretty hacky state.  



