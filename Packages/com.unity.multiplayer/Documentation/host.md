# Overview

The connection layer encapsulates both the connection and any kind of reliability your packets might need.
A NetworkHost is a abstraction over the NetworkDriver to allow you to simply setup a listening host on a specified `EndPoint` of your choise.

# Scenarios

> **IMPORTANT** 
>   Currently the `EndPoint` used is the `System.Net.EndPoint` this might change in the future to become a class inside
    the `Unity.Multiplayer` namespace.

### Create a `NetworkHost`

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

            var host = new NetworkHost(driver);

            driver.Update();
            // ... any host calls should be made after the driver.Update();
            driver.Destroy();
        }
    }
}
```

### Listen on a `NetworkHost`

>  **NOTE**
>   All below examples assume you have created and initialized a `NetworkDriver` called driver.

```c#
using Experimental.Multiplayer;

using UdpCNetworkDriver = Experimental.Multiplayer.BasicNetworkDriver<Experimental.Multiplayer.IPv4UDPSocket>;

public static class Runner
{
    public static void Run()
    {
        using (var stream = new BitStream(64, Allocator.Persistent))
        {
            var host = new NetworkHost(driver);
            var endpoint = new IPEndPoint(IPAddress.Loopback, 1337);
            host.Listen(endpoint);
        }
    }
}
```

### Accept `NetworkConnections` on a `NetworkHost`

>  **NOTE**
>   All below examples assume you have created and initialized a `NetworkDriver` called driver.

```c#
using Experimental.Multiplayer;

using UdpCNetworkDriver = Experimental.Multiplayer.BasicNetworkDriver<Experimental.Multiplayer.IPv4UDPSocket>;

public static class Runner
{
    public static void Run()
    {
        using (var stream = new BitStream(64, Allocator.Persistent))
        {
            var host = new NetworkHost(driver);
            var endpoint = new IPEndPoint(IPAddress.Loopback, 1337);
            host.Listen(endpoint);

            INetworkConnection connection;
            while ((connection = host.Accept()) != null)
            {
                Console.WriteLine("new connection accepted - connectionid = " + connection.Id);
                driver.Update();
            }
        }
    }
}
```
