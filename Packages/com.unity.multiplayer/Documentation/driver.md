# Overview

The driver layer is the lowest level of interface available for the user. 
its purpose is to work as a driver for the medium chosen and help manage 
connections in the connection layer. it has a one to many relationship with the 
connection layer.


# Scenarios

> **IMPORTANT** 
>   Currently the `EndPoint` used is the `System.Net.EndPoint` this might change in the future to become a class inside
    the `Unity.Multiplayer` namespace.


> **IMPORTANT** 
>    All networkdriver need to have their `Update` method called in order to process any events.
>    this should be done once per frame.

> **NOTE** 
>    All custom drivers should inherit from `INetworkDriver`.

> **NOTE** 
>    All drivers depend on `NetworkParams`

### Initialize the Driver  

```c#
using Experimental.Multiplayer;

using UdpCNetworkDriver = Experimental.Multiplayer.BasicNetworkDriver<Experimental.Multiplayer.IPv4UDPSocket>;

public static class Runner
{
    public static void Run()
    {
        using (var stream = new BitStream(64, Allocator.Persistent))
        {
            var driver = new UdpCNetworkDriver();
            driver.Initialize(new NetworkParams(stream));
            driver.Update();
        }
    }
}
```

### Destroy the Driver  

```c#
using Experimental.Multiplayer;

using UdpCNetworkDriver = Experimental.Multiplayer.BasicNetworkDriver<Experimental.Multiplayer.IPv4UDPSocket>;

public static class Runner
{
    public static void Run()
    {
        using (var stream = new BitStream(64, Allocator.Persistent))
        {
            var driver = new UdpCNetworkDriver();
            driver.Initialize(new NetworkParams(stream));
            driver.Update();

            driver.Destroy();
        }
    }
}
```

### Bind the Driver to an `EndPoint`

```c#
using Experimental.Multiplayer;

using UdpCNetworkDriver = Experimental.Multiplayer.BasicNetworkDriver<Experimental.Multiplayer.IPv4UDPSocket>;

public static class Runner
{
    public static void Run()
    {
        using (var stream = new BitStream(64, Allocator.Persistent))
        {
            var driver = new UdpCNetworkDriver();
            driver.Initialize(new NetworkParams(stream));
            
            // bind to any ip and any port.
            driver.Bind(new IPEndPoint(IPAddress.Any, 0));

            driver.Update();
            driver.Destroy();
        }
    }
}
```

### Listen on the Driver for incomming connections.

```c#
using Experimental.Multiplayer;

using UdpCNetworkDriver = Experimental.Multiplayer.BasicNetworkDriver<Experimental.Multiplayer.IPv4UDPSocket>;

public static class Runner
{
    public static void Run()
    {
        using (var stream = new BitStream(64, Allocator.Persistent))
        {
            var driver = new UdpCNetworkDriver();
            driver.Initialize(new NetworkParams(stream));
            
            // make sure we bind before we listen
            driver.Bind(new IPEndPoint(IPAddress.Any, 0));
            driver.Listen();

            driver.Update();
            driver.Destroy();
        }
    }
}
```

### Accept incomming connections on the Driver.

```c#
using system;
using Experimental.Multiplayer;

using UdpCNetworkDriver = Experimental.Multiplayer.BasicNetworkDriver<Experimental.Multiplayer.IPv4UDPSocket>;

public static class Runner
{
    public static void Run()
    {
        using (var stream = new BitStream(64, Allocator.Persistent))
        {
            var driver = new UdpCNetworkDriver();
            driver.Initialize(new NetworkParams(stream));
            
            // make sure we bind before we listen
            driver.Bind(new IPEndPoint(IPAddress.Any, 0));
            driver.Listen();

            int connectionId = NetworkParams.Constants.InvalidConnectionId;
            while ((connectionId = driver.Accept()) != NetworkParams.Constants.InvalidConnectionId)
            {
                Console.WriteLine("new connection accepted - connectionid = " + connectionId);
                driver.Update();
            }

            driver.Destroy();
        }
    }
}
```

### Connect to a remote `EndPoint` on the Driver

```c#
using system;
using Experimental.Multiplayer;

using UdpCNetworkDriver = Experimental.Multiplayer.BasicNetworkDriver<Experimental.Multiplayer.IPv4UDPSocket>;

public static class Runner
{
    public static void Run()
    {
        using (var stream = new BitStream(64, Allocator.Persistent))
        {
            var driver = new UdpCNetworkDriver();
            driver.Initialize(new NetworkParams(stream));

            var endpoint = new IPEndPoint(IPAddress.Loopback, 1337);
            int connectionId = driver.Connect(endpoint);

            driver.Update();
            driver.Destroy();
        }
    }
}
```

### Disconnect from a remote `EndPoint` on the Driver

```c#
using system;
using Experimental.Multiplayer;

using UdpCNetworkDriver = Experimental.Multiplayer.BasicNetworkDriver<Experimental.Multiplayer.IPv4UDPSocket>;

public static class Runner
{
    public static void Run()
    {
        using (var stream = new BitStream(64, Allocator.Persistent))
        {
            var driver = new UdpCNetworkDriver();
            driver.Initialize(new NetworkParams(stream));

            var endpoint = new IPEndPoint(IPAddress.Loopback, 1337);
            int connectionId = driver.Connect(endpoint);

            driver.Disconnect(connectionId);

            driver.Update();
            driver.Destroy();
        }
    }
}
```

### Send data to a remote `EndPoint` on the Driver

```c#
using system;
using Experimental.Multiplayer;

using UdpCNetworkDriver = Experimental.Multiplayer.BasicNetworkDriver<Experimental.Multiplayer.IPv4UDPSocket>;

public static class Runner
{
    public static void Run()
    {
        using (var stream = new BitStream(64, Allocator.Persistent))
        {
            var driver = new UdpCNetworkDriver();
            driver.Initialize(new NetworkParams(stream));

            var endpoint = new IPEndPoint(IPAddress.Loopback, 1337);
            int connectionId = driver.Connect(endpoint);

            int data = 1337;
            stream.Write(data);
            driver.Send(connectionId, stream);

            driver.Disconnect(connectionId);

            driver.Update();
            driver.Destroy();
        }
    }
}

```

### Handle events from any `EndPoint` on the Driver.

```c#
using system;
using Experimental.Multiplayer;

using UdpCNetworkDriver = Experimental.Multiplayer.BasicNetworkDriver<Experimental.Multiplayer.IPv4UDPSocket>;

public static class Runner
{
    public static void Run()
    {
        using (var stream = new BitStream(64, Allocator.Persistent))
        {
            var driver = new UdpCNetworkDriver();
            driver.Initialize(new NetworkParams(stream));

            var endpoint = new IPEndPoint(IPAddress.Loopback, 1337);
            int connectionId = driver.Connect(endpoint);

            int fromConnectionId;
            BitSlice slice;
            int receivedEvent = UdpCNetworkDriver.NetworkEvent.Empty;
            do
            {
                receivedEvent = driver.PopEvent(out fromConnectionId, out slice);
                if (receivedEvent == UdpCNetworkDriver.NetworkEvent.data)
                {
                    // handle incomming data
                    // slice will now be filled with the received data.
                }
                if (receivedEvent == UdpCNetworkDriver.NetworkEvent.Connect)
                {
                    // handle connections
                }
                if (receivedEvent == UdpCNetworkDriver.NetworkEvent.Disconnect)
                {
                    // handle disconnections
                }
            } while (receivedEvent != UdpCNetworkDriver.NetworkEvent.Empty)

            driver.Disconnect(connectionId);

            driver.Update();
            driver.Destroy();
        }
    }
}
```

### Handle events from a specific `EndPoint` on the Driver.

```c#
using System;
using Experimental.Multiplayer;

using UdpCNetworkDriver = Experimental.Multiplayer.BasicNetworkDriver<Experimental.Multiplayer.IPv4UDPSocket>;

public static class Runner
{
    public static void Run()
    {
        using (var stream = new BitStream(64, Allocator.Persistent))
        {
            var driver = new Driver();
            driver.Initialize(new NetworkParams(stream));

            var endpoint = new IPEndPoint(IPAddress.Loopback, 1337);
            int connectionId = driver.Connect(endpoint);

            BitSlice slice;

            int receivedEvent = UdpCNetworkDriver.NetworkEvent.Empty;
            do
            {
                receivedEvent = driver.PopEventForConnection(connectionId, out slice);
                if (receivedEvent == UdpCNetworkDriver.NetworkEvent.Data)
                {
                    // Handle Incomming Data
                    // slice will now be filled with the received data.
                }
                if (receivedEvent == UdpCNetworkDriver.NetworkEvent.Connect)
                {
                    // Handle Connections
                }
                if (receivedEvent == UdpCNetworkDriver.NetworkEvent.Disconnect)
                {
                    // Handle Disconnections
                }
            } while (receivedEvent != UdpCNetworkDriver.NetworkEvent.Empty)

            driver.Disconnect(connectionId);

            driver.Update();
            driver.Destroy();
        }
    }
}
```