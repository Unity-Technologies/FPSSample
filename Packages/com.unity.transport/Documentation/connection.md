# Overview

The connection layer encapsulates both the connection and any kind of reliability your packets might need.
A connection can at most have 1 NetworkDriver driving its update loop.

One fast path for parsing packets might be supplying the driver with a specific `parse` function. This function would be run directly after Driver.Update is called.


# Scenarios

> **IMPORTANT** 
>   Currently the `EndPoint` used is the `System.Net.EndPoint` this might change in the future to become a class inside
    the `Unity.Multiplayer` namespace.

### Create a connection using the `NetworkDriver`

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

            var connection = new NetworkConnection(driver);

            // `-1` indicates an empty disconnected connection.
            Assert.That(connection.Id == -1);

            driver.Update();
            // ... any connection calls should be made after the driver.Update();
            driver.Destroy();
        }
    }
}
```

### Create a connection using the `NetworkDriver` and Connect to a remote `EndPoint`

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

            var connection = new NetworkConnection(driver);

            // `-1` indicates an empty disconnected connection.
            Assert.That(connection.Id == -1);

            // create a endpoint to localhost:1337
            var endpoint = new IPEndPoint(IPAddress.Loopback, 1337);
            var id = connection.Connect(endpoint);
            Assert.That(id != -1);

            driver.Update();
            // ... any connection calls should be made after the driver.Update();
            // so you should check your connection.PollEvent() here for a `NetworkDriver.Connect` event;
            driver.Destroy();
        }
    }
}
```

### Close a NetworkConnection

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

            var connection = new NetworkConnection(driver);

            // `-1` indicates an empty disconnected connection.
            Assert.That(connection.Id == -1);

            // create a endpoint to localhost:1337
            var endpoint = new IPEndPoint(IPAddress.Loopback, 1337);
            var id = connection.Connect(endpoint);
            Assert.That(id != -1);

            driver.Update();
            // ... any connection calls should be made after the driver.Update();
            // so you should check your connection.PollEvent() here for a `NetworkDriver.Connect` event;

            connection.Close();
            driver.Destroy();
        }
    }
}
```

### `NetworkConnection` can call `PopEvent` to get incomming messages or connection events.

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

            var connection = new NetworkConnection(driver);

            // `-1` indicates an empty disconnected connection.
            Assert.That(connection.Id == -1);

            // create a endpoint to localhost:1337
            var endpoint = new IPEndPoint(IPAddress.Loopback, 1337);
            var id = connection.Connect(endpoint);
            Assert.That(id != -1);

            driver.Update();

            BitSlice slice;
            int receivedEvent = UdpCNetworkDriver.NetworkEvent.Empty;
            do
            {
                receivedEvent = connection.PopEvent(out fromConnectionId, out slice);
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

            connection.Disconnect(connectionId);
            driver.Destroy();
        }
    }
}
```

### Send data to a remote `EndPoint` on the Connection

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

            var connection = new NetworkConnection(driver);

            // `-1` indicates an empty disconnected connection.
            Assert.That(connection.Id == -1);

            // create a endpoint to localhost:1337
            var endpoint = new IPEndPoint(IPAddress.Loopback, 1337);
            var id = connection.Connect(endpoint);
            Assert.That(id != -1);

            int data = 1337;
            stream.Write(data);
            connection.Send(stream);

            connection.Disconnect();

            driver.Update();
            driver.Destroy();
        }
    }
}

```