# NetworkDriver

The driver layer is the lowest level of interface available for the user. 
its purpose is to work as a driver for the medium chosen and help manage 
connections in the connection layer. it has a one to many relationship with the 
connection layer.


> **IMPORTANT** 
>   Currently the `EndPoint` used is the `System.Net.EndPoint` this might change in the future to become a class inside
    the `Unity.Multiplayer` namespace.


> **IMPORTANT** 
>    All networkdriver need to have their `ScheduleUpdate` method called in order to process any events.
>    this should be done once per frame.

> **NOTE** 
>    All custom drivers should inherit from `INetworkDriver`.

> **NOTE** 
>    All drivers depend on `NetworkParams`

For more detailed up to date information about the function calls please take a look at the script reference.