# CSharpServer

[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Release](https://img.shields.io/github/release/chronoxor/CSharpServer.svg?sort=semver)](https://github.com/chronoxor/CSharpServer/releases)
[![Windows (Visual Studio)](https://github.com/chronoxor/CSharpServer/actions/workflows/build-windows-vs.yml/badge.svg)](https://github.com/chronoxor/CSharpServer/actions/workflows/build-windows-vs.yml)

**This project is not supported anymore! Please considering migrating to [NetCoreServer](https://github.com/chronoxor/NetCoreServer)**

Ultra fast and low latency asynchronous socket server & client C# library with
support TCP, SSL, UDP protocols and [10K connections problem](https://en.wikipedia.org/wiki/C10k_problem)
solution.

[CSharpServer documentation](https://chronoxor.github.io/CSharpServer)<br/>
[CSharpServer downloads](https://github.com/chronoxor/CSharpServer/releases)<br/>

# Contents
  * [Features](#features)
  * [Requirements](#requirements)
  * [How to build?](#how-to-build)
  * [Examples](#examples)
    * [Example: Asio timer](#example-asio-timer)
    * [Example: TCP chat server](#example-tcp-chat-server)
    * [Example: TCP chat client](#example-tcp-chat-client)
    * [Example: SSL chat server](#example-ssl-chat-server)
    * [Example: SSL chat client](#example-ssl-chat-client)
    * [Example: UDP echo server](#example-udp-echo-server)
    * [Example: UDP echo client](#example-udp-echo-client)
    * [Example: UDP multicast server](#example-udp-multicast-server)
    * [Example: UDP multicast client](#example-udp-multicast-client)
  * [Performance](#performance)
    * [Benchmark: Round-Trip](#benchmark-round-trip)
      * [TCP echo server](#tcp-echo-server)
      * [SSL echo server](#ssl-echo-server)
      * [UDP echo server](#udp-echo-server)
    * [Benchmark: Multicast](#benchmark-multicast)
      * [TCP multicast server](#tcp-multicast-server)
      * [SSL multicast server](#ssl-multicast-server)
      * [UDP multicast server](#udp-multicast-server)
  * [OpenSSL certificates](#openssl-certificates)
    * [Production](#production)
    * [Development](#development)
    * [Certificate Authority](#certificate-authority)
    * [SSL Server certificate](#ssl-server-certificate)
    * [SSL Client certificate](#ssl-client-certificate)
    * [Diffie-Hellman key exchange](#diffie-hellman-key-exchange)

# Features
* [Asynchronous communication](https://think-async.com)
* Supported CPU scalability designs: IO service per thread, thread pool
* Supported transport protocols: [TCP](#example-tcp-chat-server), [SSL](#example-ssl-chat-server),
  [UDP](#example-udp-echo-server), [UDP multicast](#example-udp-multicast-server)

# Requirements
* Windows
* [7-Zip](https://www.7-zip.org)
* [cmake](https://www.cmake.org)
* [git](https://git-scm.com)
* [gil](https://github.com/chronoxor/gil.git)
* [python3](https://www.python.org)
* [Visual Studio](https://www.visualstudio.com)

# How to build?

### Install [gil (git links) tool](https://github.com/chronoxor/gil)
```shell
pip3 install gil
```

### Setup repository
```shell
git clone https://github.com/chronoxor/CSharpServer.git
cd CSharpServer
gil update
```

### Generate CMake projects
Run CMake script to generate C++ projects:
```shell
cd modules/CppServer/build/VisualStudio
01-generate.bat
```

### Windows (Visual Studio)
Open and build [CSharpServer.sln](https://github.com/chronoxor/CSharpServer/blob/master/CSharpServer.sln) or run the build script:
```shell
cd build
vs.bat
```

The build script will create "release" directory with zip files:
* CSharpServer.zip - C# Server assembly
* Benchmarks.zip - C# Server benchmarks
* Examples.zip - C# Server examples

# Examples

## Example: Asio timer
Here comes the example of Asio timer. It can be used to wait for some action
in future with providing absolute time or relative time span. Asio timer can
be used in synchronous or asynchronous modes.
```c#
using System;
using System.Threading;
using CSharpServer;

namespace AsioTimer
{
    class AsioTimer : CSharpServer.Timer
    {
        public AsioTimer(Service service) : base(service) {}

        protected override void OnTimer(bool canceled)
        {
            Console.WriteLine("Asio timer " + (canceled ? "canceled" : "expired"));
        }

        protected override void OnError(int error, string category, string message)
        {
            Console.WriteLine($"Asio timer caught an error with code {error} and category '{category}': {message}");
        }
    }

    class Program
    {
        static void Main()
        {
            // Create a new service
            var service = new Service();

            // Start the service
            Console.Write("Service starting...");
            service.Start();
            Console.WriteLine("Done!");

            // Create a new Asio timer
            var timer = new AsioTimer(service);

            // Setup and synchronously wait for the timer
            timer.Setup(DateTime.UtcNow.AddSeconds(1));
            timer.WaitSync();

            // Setup and asynchronously wait for the timer
            timer.Setup(TimeSpan.FromSeconds(1));
            timer.WaitAsync();

            // Wait for a while...
            Thread.Sleep(2000);

            // Setup and asynchronously wait for the timer
            timer.Setup(TimeSpan.FromSeconds(1));
            timer.WaitAsync();

            // Wait for a while...
            Thread.Sleep(500);

            // Cancel the timer
            timer.Cancel();

            // Wait for a while...
            Thread.Sleep(500);

            // Stop the service
            Console.Write("Service stopping...");
            service.Stop();
            Console.WriteLine("Done!");
        }
    }
}
```

Output of the above example is the following:
```
Service starting...Done!
Asio timer expired
Asio timer canceled
Service stopping...Done!
```

## Example: TCP chat server
Here comes the example of the TCP chat server. It handles multiple TCP client
sessions and multicast received message from any session to all ones. Also it
is possible to send admin message directly from the server.

```c#
using System;
using System.Text;
using CSharpServer;

namespace TcpChatServer
{
    class ChatSession : TcpSession
    {
        public ChatSession(TcpServer server) : base(server) {}

        protected override void OnConnected()
        {
            Console.WriteLine($"Chat TCP session with Id {Id} connected!");

            // Send invite message
            string message = "Hello from TCP chat! Please send a message or '!' to disconnect the client!";
            SendAsync(message);
        }

        protected override void OnDisconnected()
        {
            Console.WriteLine($"Chat TCP session with Id {Id} disconnected!");
        }

        protected override void OnReceived(byte[] buffer, long size)
        {
            string message = Encoding.UTF8.GetString(buffer, 0, (int)size);
            Console.WriteLine("Incoming: " + message);

            // Multicast message to all connected sessions
            Server.Multicast(message);

            // If the buffer starts with '!' the disconnect the current session
            if (message == "!")
                DisconnectAsync();
        }

        protected override void OnError(int error, string category, string message)
        {
            Console.WriteLine($"Chat TCP session caught an error with code {error} and category '{category}': {message}");
        }
    }

    class ChatServer : TcpServer
    {
        public ChatServer(Service service, int port, InternetProtocol protocol) : base(service, port, protocol) {}

        protected override TcpSession CreateSession() { return new ChatSession(this); }

        protected override void OnError(int error, string category, string message)
        {
            Console.WriteLine($"Chat TCP server caught an error with code {error} and category '{category}': {message}");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            // TCP server port
            int port = 1111;
            if (args.Length > 0)
                port = int.Parse(args[0]);

            Console.WriteLine($"TCP server port: {port}");

            // Create a new service
            var service = new Service();

            // Start the service
            Console.Write("Service starting...");
            service.Start();
            Console.WriteLine("Done!");

            // Create a new TCP chat server
            var server = new ChatServer(service, port, InternetProtocol.IPv4);

            // Start the server
            Console.Write("Server starting...");
            server.Start();
            Console.WriteLine("Done!");

            Console.WriteLine("Press Enter to stop the server or '!' to restart the server...");

            // Perform text input
            for (;;)
            {
                string line = Console.ReadLine();
                if (line == string.Empty)
                    break;

                // Restart the server
                if (line == "!")
                {
                    Console.Write("Server restarting...");
                    server.Restart();
                    Console.WriteLine("Done!");
                    continue;
                }

                // Multicast admin message to all sessions
                line = "(admin) " + line;
                server.Multicast(line);
            }

            // Stop the server
            Console.Write("Server stopping...");
            server.Stop();
            Console.WriteLine("Done!");

            // Stop the service
            Console.Write("Service stopping...");
            service.Stop();
            Console.WriteLine("Done!");
        }
    }
}
```

## Example: TCP chat client
Here comes the example of the TCP chat client. It connects to the TCP chat
server and allows to send message to it and receive new messages.

```c#
using System;
using System.Text;
using System.Threading;
using CSharpServer;

namespace TcpChatClient
{
    class ChatClient : TcpClient
    {
        public ChatClient(Service service, string address, int port) : base(service, address, port) {}

        public void DisconnectAndStop()
        {
            _stop = true;
            DisconnectAsync();
            while (IsConnected)
                Thread.Yield();
        }

        protected override void OnConnected()
        {
            Console.WriteLine($"Chat TCP client connected a new session with Id {Id}");
        }

        protected override void OnDisconnected()
        {
            Console.WriteLine($"Chat TCP client disconnected a session with Id {Id}");

            // Wait for a while...
            Thread.Sleep(1000);

            // Try to connect again
            if (!_stop)
                ConnectAsync();
        }

        protected override void OnReceived(byte[] buffer, long size)
        {
            Console.WriteLine(Encoding.UTF8.GetString(buffer, 0, (int)size));
        }

        protected override void OnError(int error, string category, string message)
        {
            Console.WriteLine($"Chat TCP client caught an error with code {error} and category '{category}': {message}");
        }

        private bool _stop;
    }

    class Program
    {
        static void Main(string[] args)
        {
            // TCP server address
            string address = "127.0.0.1";
            if (args.Length > 0)
                address = args[0];

            // TCP server port
            int port = 1111;
            if (args.Length > 1)
                port = int.Parse(args[1]);

            Console.WriteLine($"TCP server address: {address}");
            Console.WriteLine($"TCP server port: {port}");

            // Create a new service
            var service = new Service();

            // Start the service
            Console.Write("Service starting...");
            service.Start();
            Console.WriteLine("Done!");

            // Create a new TCP chat client
            var client = new ChatClient(service, address, port);

            // Connect the client
            Console.Write("Client connecting...");
            client.ConnectAsync();
            Console.WriteLine("Done!");

            Console.WriteLine("Press Enter to stop the client or '!' to reconnect the client...");

            // Perform text input
            for (;;)
            {
                string line = Console.ReadLine();
                if (line == string.Empty)
                    break;

                // Disconnect the client
                if (line == "!")
                {
                    Console.Write("Client disconnecting...");
                    client.DisconnectAsync();
                    Console.WriteLine("Done!");
                    continue;
                }

                // Send the entered text to the chat server
                client.SendAsync(line);
            }

            // Disconnect the client
            Console.Write("Client disconnecting...");
            client.DisconnectAndStop();
            Console.WriteLine("Done!");

            // Stop the service
            Console.Write("Service stopping...");
            service.Stop();
            Console.WriteLine("Done!");
        }
    }
}
```

## Example: SSL chat server
Here comes the example of the SSL chat server. It handles multiple SSL client
sessions and multicast received message from any session to all ones. Also it
is possible to send admin message directly from the server.

This example is very similar to the TCP one except the code that prepares SSL
context and handshake handler.

```c#
using System;
using System.Text;
using CSharpServer;

namespace SslChatServer
{
    class ChatSession : SslSession
    {
        public ChatSession(SslServer server) : base(server) {}

        protected override void OnConnected()
        {
            Console.WriteLine($"Chat SSL session with Id {Id} connected!");
        }

        protected override void OnHandshaked()
        {
            Console.WriteLine($"Chat SSL session with Id {Id} handshaked!");

            // Send invite message
            string message = "Hello from SSL chat! Please send a message or '!' to disconnect the client!";
            SendAsync(message);
        }

        protected override void OnDisconnected()
        {
            Console.WriteLine($"Chat SSL session with Id {Id} disconnected!");
        }

        protected override void OnReceived(byte[] buffer, long size)
        {
            string message = Encoding.UTF8.GetString(buffer, 0, (int)size);
            Console.WriteLine("Incoming: " + message);

            // Multicast message to all connected sessions
            Server.Multicast(message);

            // If the buffer starts with '!' the disconnect the current session
            if (message == "!")
                DisconnectAsync();
        }

        protected override void OnError(int error, string category, string message)
        {
            Console.WriteLine($"Chat SSL session caught an error with code {error} and category '{category}': {message}");
        }
    }

    class ChatServer : SslServer
    {
        public ChatServer(Service service, SslContext context, int port, InternetProtocol protocol) : base(service, context, port, protocol) {}

        protected override SslSession CreateSession() { return new ChatSession(this); }

        protected override void OnError(int error, string category, string message)
        {
            Console.WriteLine($"Chat SSL server caught an error with code {error} and category '{category}': {message}");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            // SSL server port
            int port = 2222;
            if (args.Length > 0)
                port = int.Parse(args[0]);

            Console.WriteLine($"SSL server port: {port}");

            // Create a new service
            var service = new Service();

            // Start the service
            Console.Write("Service starting...");
            service.Start();
            Console.WriteLine("Done!");

            // Create and prepare a new SSL server context
            var context = new SslContext(SslMethod.TLSV12);
            context.SetPassword("qwerty");
            context.UseCertificateChainFile("server.pem");
            context.UsePrivateKeyFile("server.pem", SslFileFormat.PEM);
            context.UseTmpDHFile("dh4096.pem");

            // Create a new SSL chat server
            var server = new ChatServer(service, context, port, InternetProtocol.IPv4);

            // Start the server
            Console.Write("Server starting...");
            server.Start();
            Console.WriteLine("Done!");

            Console.WriteLine("Press Enter to stop the server or '!' to restart the server...");

            // Perform text input
            for (;;)
            {
                string line = Console.ReadLine();
                if (line == string.Empty)
                    break;

                // Restart the server
                if (line == "!")
                {
                    Console.Write("Server restarting...");
                    server.Restart();
                    Console.WriteLine("Done!");
                    continue;
                }

                // Multicast admin message to all sessions
                line = "(admin) " + line;
                server.Multicast(line);
            }

            // Stop the server
            Console.Write("Server stopping...");
            server.Stop();
            Console.WriteLine("Done!");

            // Stop the service
            Console.Write("Service stopping...");
            service.Stop();
            Console.WriteLine("Done!");
        }
    }
}
```

## Example: SSL chat client
Here comes the example of the SSL chat client. It connects to the SSL chat
server and allows to send message to it and receive new messages.

This example is very similar to the TCP one except the code that prepares SSL
context and handshake handler.

```c#
using System;
using System.Text;
using System.Threading;
using CSharpServer;

namespace SslChatClient
{
    class ChatClient : SslClient
    {
        public ChatClient(Service service, SslContext context, string address, int port) : base(service, context, address, port) {}

        public void DisconnectAndStop()
        {
            _stop = true;
            DisconnectAsync();
            while (IsConnected)
                Thread.Yield();
        }

        protected override void OnConnected()
        {
            Console.WriteLine($"Chat SSL client connected a new session with Id {Id}");
        }

        protected override void OnHandshaked()
        {
            Console.WriteLine($"Chat SSL client handshaked a new session with Id {Id}");
        }

        protected override void OnDisconnected()
        {
            Console.WriteLine($"Chat SSL client disconnected a session with Id {Id}");

            // Wait for a while...
            Thread.Sleep(1000);

            // Try to connect again
            if (!_stop)
                ConnectAsync();
        }

        protected override void OnReceived(byte[] buffer, long size)
        {
            Console.WriteLine(Encoding.UTF8.GetString(buffer, 0, (int)size));
        }

        protected override void OnError(int error, string category, string message)
        {
            Console.WriteLine($"Chat SSL client caught an error with code {error} and category '{category}': {message}");
        }

        private bool _stop;
    }

    class Program
    {
        static void Main(string[] args)
        {
            // SSL server address
            string address = "127.0.0.1";
            if (args.Length > 0)
                address = args[0];

            // SSL server port
            int port = 2222;
            if (args.Length > 1)
                port = int.Parse(args[1]);

            Console.WriteLine($"SSL server address: {address}");
            Console.WriteLine($"SSL server port: {port}");

            // Create a new service
            var service = new Service();

            // Start the service
            Console.Write("Service starting...");
            service.Start();
            Console.WriteLine("Done!");

            // Create and prepare a new SSL client context
            var context = new SslContext(SslMethod.TLSV12);
            context.SetDefaultVerifyPaths();
            context.SetRootCerts();
            context.SetVerifyMode(SslVerifyMode.VerifyPeer | SslVerifyMode.VerifyFailIfNoPeerCert);
            context.LoadVerifyFile("ca.pem");

            // Create a new SSL chat client
            var client = new ChatClient(service, context, address, port);

            // Connect the client
            Console.Write("Client connecting...");
            client.ConnectAsync();
            Console.WriteLine("Done!");

            Console.WriteLine("Press Enter to stop the client or '!' to reconnect the client...");

            // Perform text input
            for (;;)
            {
                string line = Console.ReadLine();
                if (line == string.Empty)
                    break;

                // Disconnect the client
                if (line == "!")
                {
                    Console.Write("Client disconnecting...");
                    client.DisconnectAsync();
                    Console.WriteLine("Done!");
                    continue;
                }

                // Send the entered text to the chat server
                client.SendAsync(line);
            }

            // Disconnect the client
            Console.Write("Client disconnecting...");
            client.DisconnectAndStop();
            Console.WriteLine("Done!");

            // Stop the service
            Console.Write("Service stopping...");
            service.Stop();
            Console.WriteLine("Done!");
        }
    }
}
```

## Example: UDP echo server
Here comes the example of the UDP echo server. It receives a datagram mesage
from any UDP client and resend it back without any changes.

```c#
using System;
using System.Text;
using CSharpServer;

namespace UdpEchoServer
{
    class EchoServer : UdpServer
    {
        public EchoServer(Service service, int port, InternetProtocol protocol) : base(service, port, protocol) {}

        protected override void OnStarted()
        {
            // Start receive datagrams
            ReceiveAsync();
        }

        protected override void OnReceived(UdpEndpoint endpoint, byte[] buffer, long size)
        {
            Console.WriteLine("Incoming: " + Encoding.UTF8.GetString(buffer, 0, (int)size));

            // Echo the message back to the sender
            SendAsync(endpoint, buffer, 0, size);
        }

        protected override void OnSent(UdpEndpoint endpoint, long sent)
        {
            // Continue receive datagrams
            ReceiveAsync();
        }

        protected override void OnError(int error, string category, string message)
        {
            Console.WriteLine($"Echo UDP server caught an error with code {error} and category '{category}': {message}");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            // UDP server port
            int port = 3333;
            if (args.Length > 0)
                port = int.Parse(args[0]);

            Console.WriteLine($"UDP server port: {port}");

            // Create a new service
            var service = new Service();

            // Start the service
            Console.Write("Service starting...");
            service.Start();
            Console.WriteLine("Done!");

            // Create a new UDP echo server
            var server = new EchoServer(service, port, InternetProtocol.IPv4);

            // Start the server
            Console.Write("Server starting...");
            server.Start();
            Console.WriteLine("Done!");

            Console.WriteLine("Press Enter to stop the server or '!' to restart the server...");

            // Perform text input
            for (;;)
            {
                string line = Console.ReadLine();
                if (line == string.Empty)
                    break;

                // Restart the server
                if (line == "!")
                {
                    Console.Write("Server restarting...");
                    server.Restart();
                    Console.WriteLine("Done!");
                }
            }

            // Stop the server
            Console.Write("Server stopping...");
            server.Stop();
            Console.WriteLine("Done!");

            // Stop the service
            Console.Write("Service stopping...");
            service.Stop();
            Console.WriteLine("Done!");
        }
    }
}
```

## Example: UDP echo client
Here comes the example of the UDP echo client. It sends user datagram message
to UDP server and listen for response.

```c#
using System;
using System.Text;
using System.Threading;
using CSharpServer;

namespace UdpEchoClient
{
    class EchoClient : UdpClient
    {
        public EchoClient(Service service, string address, int port) : base(service, address, port) {}

        public void DisconnectAndStop()
        {
            _stop = true;
            DisconnectAsync();
            while (IsConnected)
                Thread.Yield();
        }

        protected override void OnConnected()
        {
            Console.WriteLine($"Echo UDP client connected a new session with Id {Id}");

            // Start receive datagrams
            ReceiveAsync();
        }

        protected override void OnDisconnected()
        {
            Console.WriteLine($"Echo UDP client disconnected a session with Id {Id}");

            // Wait for a while...
            Thread.Sleep(1000);

            // Try to connect again
            if (!_stop)
                ConnectAsync();
        }

        protected override void OnReceived(UdpEndpoint endpoint, byte[] buffer, long size)
        {
            Console.WriteLine("Incoming: " + Encoding.UTF8.GetString(buffer, 0, (int)size));

            // Continue receive datagrams
            ReceiveAsync();
        }

        protected override void OnError(int error, string category, string message)
        {
            Console.WriteLine($"Echo UDP client caught an error with code {error} and category '{category}': {message}");
        }

        private bool _stop;
    }

    class Program
    {
        static void Main(string[] args)
        {
            // UDP server address
            string address = "127.0.0.1";
            if (args.Length > 0)
                address = args[0];

            // UDP server port
            int port = 3333;
            if (args.Length > 1)
                port = int.Parse(args[1]);

            Console.WriteLine($"UDP server address: {address}");
            Console.WriteLine($"UDP server port: {port}");

            // Create a new service
            var service = new Service();

            // Start the service
            Console.Write("Service starting...");
            service.Start();
            Console.WriteLine("Done!");

            // Create a new TCP chat client
            var client = new EchoClient(service, address, port);

            // Connect the client
            Console.Write("Client connecting...");
            client.ConnectAsync();
            Console.WriteLine("Done!");

            Console.WriteLine("Press Enter to stop the client or '!' to reconnect the client...");

            // Perform text input
            for (;;)
            {
                string line = Console.ReadLine();
                if (line == string.Empty)
                    break;

                // Disconnect the client
                if (line == "!")
                {
                    Console.Write("Client disconnecting...");
                    client.DisconnectAsync();
                    Console.WriteLine("Done!");
                    continue;
                }

                // Send the entered text to the chat server
                client.SendSync(line);
            }

            // Disconnect the client
            Console.Write("Client disconnecting...");
            client.DisconnectAndStop();
            Console.WriteLine("Done!");

            // Stop the service
            Console.Write("Service stopping...");
            service.Stop();
            Console.WriteLine("Done!");
        }
    }
}
```

## Example: UDP multicast server
Here comes the example of the UDP multicast server. It use multicast IP address
to multicast datagram messages to all client that joined corresponding UDP
multicast group.

```c#
using System;
using System.Text;
using CSharpServer;

namespace UdpMulticastServer
{
    class MulticastServer : UdpServer
    {
        public MulticastServer(Service service, int port, InternetProtocol protocol) : base(service, port, protocol) {}

        protected override void OnError(int error, string category, string message)
        {
            Console.WriteLine($"Multicast UDP server caught an error with code {error} and category '{category}': {message}");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            // UDP multicast address
            string multicastAddress = "239.255.0.1";
            if (args.Length > 0)
                multicastAddress = args[0];

            // UDP multicast port
            int multicastPort = 3334;
            if (args.Length > 1)
                multicastPort = int.Parse(args[1]);

            Console.WriteLine($"UDP multicast address: {multicastAddress}");
            Console.WriteLine($"UDP multicast port: {multicastPort}");

            // Create a new service
            var service = new Service();

            // Start the service
            Console.Write("Service starting...");
            service.Start();
            Console.WriteLine("Done!");

            // Create a new UDP multicast server
            var server = new MulticastServer(service, 0, InternetProtocol.IPv4);

            // Start the multicast server
            Console.Write("Server starting...");
            server.Start(multicastAddress, multicastPort);
            Console.WriteLine("Done!");

            Console.WriteLine("Press Enter to stop the server or '!' to restart the server...");

            // Perform text input
            for (;;)
            {
                string line = Console.ReadLine();
                if (line == string.Empty)
                    break;

                // Restart the server
                if (line == "!")
                {
                    Console.Write("Server restarting...");
                    server.Restart();
                    Console.WriteLine("Done!");
                    continue;
                }

                // Multicast admin message to all sessions
                line = "(admin) " + line;
                server.MulticastSync(line);
            }

            // Stop the server
            Console.Write("Server stopping...");
            server.Stop();
            Console.WriteLine("Done!");

            // Stop the service
            Console.Write("Service stopping...");
            service.Stop();
            Console.WriteLine("Done!");
        }
    }
}
```

## Example: UDP multicast client
Here comes the example of the UDP multicast client. It use multicast IP address
and joins UDP multicast group in order to receive multicasted datagram messages
from UDP server.

```c#
using System;
using System.Text;
using System.Threading;
using CSharpServer;

namespace UdpMulticastClient
{
    class MulticastClient : UdpClient
    {
        public string Multicast;

        public MulticastClient(Service service, string address, int port) : base(service, address, port) {}

        public void DisconnectAndStop()
        {
            _stop = true;
            DisconnectAsync();
            while (IsConnected)
                Thread.Yield();
        }

        protected override void OnConnected()
        {
            Console.WriteLine($"Multicast UDP client connected a new session with Id {Id}");

            // Join UDP multicast group
            JoinMulticastGroupAsync(Multicast);

            // Start receive datagrams
            ReceiveAsync();
        }

        protected override void OnDisconnected()
        {
            Console.WriteLine($"Multicast UDP client disconnected a session with Id {Id}");

            // Wait for a while...
            Thread.Sleep(1000);

            // Try to connect again
            if (!_stop)
                ConnectAsync();
        }

        protected override void OnReceived(UdpEndpoint endpoint, byte[] buffer, long size)
        {
            Console.WriteLine("Incoming: " + Encoding.UTF8.GetString(buffer, 0, (int)size));

            // Continue receive datagrams
            ReceiveAsync();
        }

        protected override void OnError(int error, string category, string message)
        {
            Console.WriteLine($"Multicast UDP client caught an error with code {error} and category '{category}': {message}");
        }

        private bool _stop;
    }

    class Program
    {
        static void Main(string[] args)
        {
            // UDP listen address
            string listenAddress = "0.0.0.0";
            if (args.Length > 0)
                listenAddress = args[0];

            // UDP multicast address
            string multicastAddress = "239.255.0.1";
            if (args.Length > 1)
                multicastAddress = args[1];

            // UDP multicast port
            int multicastPort = 3334;
            if (args.Length > 2)
                multicastPort = int.Parse(args[2]);

            Console.WriteLine($"UDP listen address: {listenAddress}");
            Console.WriteLine($"UDP multicast address: {multicastAddress}");
            Console.WriteLine($"UDP multicast port: {multicastPort}");

            // Create a new service
            var service = new Service();

            // Start the service
            Console.Write("Service starting...");
            service.Start();
            Console.WriteLine("Done!");

            // Create a new TCP chat client
            var client = new MulticastClient(service, listenAddress, multicastPort);
            client.SetupMulticast(true);
            client.Multicast = multicastAddress;

            // Connect the client
            Console.Write("Client connecting...");
            client.ConnectAsync();
            Console.WriteLine("Done!");

            Console.WriteLine("Press Enter to stop the client or '!' to reconnect the client...");

            // Perform text input
            for (;;)
            {
                string line = Console.ReadLine();
                if (line == string.Empty)
                    break;

                // Disconnect the client
                if (line == "!")
                {
                    Console.Write("Client disconnecting...");
                    client.DisconnectAsync();
                    Console.WriteLine("Done!");
                    continue;
                }
            }

            // Disconnect the client
            Console.Write("Client disconnecting...");
            client.DisconnectAndStop();
            Console.WriteLine("Done!");

            // Stop the service
            Console.Write("Service stopping...");
            service.Stop();
            Console.WriteLine("Done!");
        }
    }
}
```

# Performance

Here comes several communication scenarios with timing measurements.

Benchmark environment is the following:
```
CPU architecutre: Intel(R) Core(TM) i7-4790K CPU @ 4.00GHz
CPU logical cores: 8
CPU physical cores: 4
CPU clock speed: 3.998 GHz
CPU Hyper-Threading: enabled
RAM total: 31.962 GiB
RAM free: 21.623 GiB

OS version: Microsoft Windows 8 Enterprise Edition (build 9200), 64-bit
OS bits: 64-bit
Process bits: 64-bit
Process configuaraion: release
```

## Benchmark: Round-Trip

![Round-trip](https://github.com/chronoxor/CSharpServer/raw/master/images/round-trip.png)

This scenario sends lots of messages from several clients to a server.
The server responses to each message and resend the similar response to
the client. The benchmark measures total round-trip time to send all
messages and receive all responses, messages & data throughput, count
of errors.

### TCP echo server

* [TcpEchoServer](https://github.com/chronoxor/CSharpServer/blob/master/performance/TcpEchoServer/Program.cs)
* [TcpEchoClient](https://github.com/chronoxor/CSharpServer/blob/master/performance/TcpEchoClient/Program.cs) --clients 1 --threads 1

```
Server address: 127.0.0.1
Server port: 1111
Working threads: 1
Working clients: 1
Working messages: 1000
Message size: 32
Seconds to benchmarking: 10

Errors: 0

Total time: 10.001 s
Total data: 1.493 GiB
Total messages: 49715465
Data throughput: 151.709 MiB/s
Message latency: 201 ns
Message throughput: 4970671 msg/s
```

* [TcpEchoServer](https://github.com/chronoxor/CSharpServer/blob/master/performance/TcpEchoServer/Program.cs)
* [TcpEchoClient](https://github.com/chronoxor/CSharpServer/blob/master/performance/TcpEchoClient/Program.cs) --clients 100 --threads 4

```
Server address: 127.0.0.1
Server port: 1111
Working threads: 4
Working clients: 100
Working messages: 1000
Message size: 32
Seconds to benchmarking: 10

Errors: 0

Total time: 10.011 s
Total data: 1.055 GiB
Total messages: 35386331
Data throughput: 107.882 MiB/s
Message latency: 282 ns
Message throughput: 3534415 msg/s
```

### SSL echo server

* [SslEchoServer](https://github.com/chronoxor/CSharpServer/blob/master/performance/SslEchoServer/Program.cs)
* [SslEchoClient](https://github.com/chronoxor/CSharpServer/blob/master/performance/SslEchoClient/Program.cs) --clients 1 --threads 1

```
Server address: 127.0.0.1
Server port: 2222
Working threads: 1
Working clients: 1
Working messages: 1000
Message size: 32
Seconds to benchmarking: 10

Errors: 0

Total time: 10.013 s
Total data: 151.424 MiB
Total messages: 4961559
Data throughput: 15.124 MiB/s
Message latency: 2.018 mcs
Message throughput: 495500 msg/s
```

* [SslEchoServer](https://github.com/chronoxor/CSharpServer/blob/master/performance/SslEchoServer/Program.cs)
* [SslEchoClient](https://github.com/chronoxor/CSharpServer/blob/master/performance/SslEchoClient/Program.cs) --clients 100 --threads 4

```
Server address: 127.0.0.1
Server port: 2222
Working threads: 4
Working clients: 100
Working messages: 1000
Message size: 32
Seconds to benchmarking: 10

Errors: 0

Total time: 10.305 s
Total data: 411.159 MiB
Total messages: 13472751
Data throughput: 39.917 MiB/s
Message latency: 764 ns
Message throughput: 1307300 msg/s
```

### UDP echo server

* [UdpEchoServer](https://github.com/chronoxor/CSharpServer/blob/master/performance/UdpEchoServer/Program.cs)
* [UdpEchoClient](https://github.com/chronoxor/CSharpServer/blob/master/performance/UdpEchoClient/Program.cs) --clients 1 --threads 1

```
Server address: 127.0.0.1
Server port: 3333
Working threads: 1
Working clients: 1
Working messages: 1000
Message size: 32
Seconds to benchmarking: 10

Errors: 0

Total time: 208.608 ms
Total data: 220.736 KiB
Total messages: 7063
Data throughput: 1.034 MiB/s
Message latency: 29.535 mcs
Message throughput: 33857 msg/s
```

* [UdpEchoServer](https://github.com/chronoxor/CSharpServer/blob/master/performance/UdpEchoServer/Program.cs)
* [UdpEchoClient](https://github.com/chronoxor/CSharpServer/blob/master/performance/UdpEchoClient/Program.cs) --clients 100 --threads 4

```
Server address: 127.0.0.1
Server port: 3333
Working threads: 4
Working clients: 100
Working messages: 1000
Message size: 32
Seconds to benchmarking: 10

Errors: 0

Total time: 10.008 s
Total data: 41.094 MiB
Total messages: 1346505
Data throughput: 4.108 MiB/s
Message latency: 7.432 mcs
Message throughput: 134540 msg/s
```

## Benchmark: Multicast

![Multicast](https://github.com/chronoxor/CSharpServer/raw/master/images/multicast.png)

In this scenario server multicasts messages to all connected clients.
The benchmark counts total messages received by all clients for all
the working time and measures messages & data throughput, count
of errors.

### TCP multicast server

* [TcpMulticastServer](https://github.com/chronoxor/CSharpServer/blob/master/performance/TcpMulticastServer/Program.cs)
* [TcpMulticastClient](https://github.com/chronoxor/CSharpServer/blob/master/performance/TcpMulticastClient/Program.cs) --clients 1 --threads 1

```
Server address: 127.0.0.1
Server port: 1111
Working threads: 1
Working clients: 1
Message size: 32

Errors: 0

Total time: 10.003 s
Total data: 947.864 MiB
Total messages: 31058956
Data throughput: 94.764 MiB/s
Message latency: 322 ns
Message throughput: 3104662 msg/s
```

* [TcpMulticastServer](https://github.com/chronoxor/CSharpServer/blob/master/performance/TcpMulticastServer/Program.cs)
* [TcpMulticastClient](https://github.com/chronoxor/CSharpServer/blob/master/performance/TcpMulticastClient/Program.cs) --clients 100 --threads 4

```
Server address: 127.0.0.1
Server port: 1111
Working threads: 4
Working clients: 100
Message size: 32

Errors: 0

Total time: 10.008 s
Total data: 330.068 MiB
Total messages: 10815633
Data throughput: 32.1001 MiB/s
Message latency: 925 ns
Message throughput: 1080613 msg/s
```

### SSL multicast server

* [SslMulticastServer](https://github.com/chronoxor/CSharpServer/blob/master/performance/SslMulticastServer/Program.cs)
* [SslMulticastClient](https://github.com/chronoxor/CSharpServer/blob/master/performance/SslMulticastClient/Program.cs) --clients 1 --threads 1

```
Server address: 127.0.0.1
Server port: 2222
Working threads: 1
Working clients: 1
Message size: 32

Errors: 0

Total time: 10.012 s
Total data: 974.081 MiB
Total messages: 31918634
Data throughput: 97.291 MiB/s
Message latency: 313 ns
Message throughput: 3187826 msg/s
```

* [SslMulticastServer](https://github.com/chronoxor/CSharpServer/blob/master/performance/SslMulticastServer/Program.cs)
* [SslMulticastClient](https://github.com/chronoxor/CSharpServer/blob/master/performance/SslMulticastClient/Program.cs) --clients 100 --threads 4

```
Server address: 127.0.0.1
Server port: 2222
Working threads: 4
Working clients: 100
Message size: 32

Errors: 0

Total time: 10.401 s
Total data: 1.121 GiB
Total messages: 37543146
Data throughput: 110.149 MiB/s
Message latency: 277 ns
Message throughput: 3609250 msg/s
```

### UDP multicast server

* [UdpMulticastServer](https://github.com/chronoxor/CSharpServer/blob/master/performance/UdpMulticastServer/Program.cs)
* [UdpMulticastClient](https://github.com/chronoxor/CSharpServer/blob/master/performance/UdpMulticastClient/Program.cs) --clients 1 --threads 1

```
Server address: 239.255.0.1
Server port: 3333
Working threads: 1
Working clients: 1
Message size: 32

Errors: 0

Total time: 10.002 s
Total data: 15.1000 MiB
Total messages: 523548
Data throughput: 1.611 MiB/s
Message latency: 19.106 mcs
Message throughput: 52339 msg/s
```

* [UdpMulticastServer](https://github.com/chronoxor/CSharpServer/blob/master/performance/UdpMulticastServer/Program.cs)
* [UdpMulticastClient](https://github.com/chronoxor/CSharpServer/blob/master/performance/UdpMulticastClient/Program.cs) --clients 100 --threads 4

```
Server address: 239.255.0.1
Server port: 3333
Working threads: 4
Working clients: 100
Message size: 32

Errors: 0

Total time: 10.013 s
Total data: 57.876 MiB
Total messages: 1895816
Data throughput: 5.796 MiB/s
Message latency: 5.281 mcs
Message throughput: 189335 msg/s
```

# OpenSSL certificates
In order to create OpenSSL based server and client you should prepare a set of
SSL certificates.

## Production
Depending on your project, you may need to purchase a traditional SSL
certificate signed by a Certificate Authority. If you, for instance,
want some else's web browser to talk to your WebSocket project, you'll
need a traditional SSL certificate.

## Development
The commands below entered in the order they are listed will generate a
self-signed certificate for development or testing purposes.

If you want to save some time, download and run [Generate-Certs](https://github.com/asheroto/NetCoreServer-Generate-Certs)
and it will generate the certs for you in less than a minute.
Supports Windows + Linux.

If you'd rather enter in the commands to generate the certificates manually,
here is the list in order below.

## Certificate Authority

* Create CA private key
```shell
openssl genrsa -passout pass:qwerty -out ca-secret.key 4096
```

* Remove passphrase
```shell
openssl rsa -passin pass:qwerty -in ca-secret.key -out ca.key
```

* Create CA self-signed certificate
```shell
openssl req -new -x509 -days 3650 -subj '/C=BY/ST=Belarus/L=Minsk/O=Example root CA/OU=Example CA unit/CN=example.com' -key ca.key -out ca.crt
```

* Convert CA self-signed certificate to PFX
```shell
openssl pkcs12 -export -passout pass:qwerty -inkey ca.key -in ca.crt -out ca.pfx
```

* Convert CA self-signed certificate to PEM
```shell
openssl pkcs12 -passin pass:qwerty -passout pass:qwerty -in ca.pfx -out ca.pem
```

## SSL Server certificate

* Create private key for the server
```shell
openssl genrsa -passout pass:qwerty -out server-secret.key 4096
```

* Remove passphrase
```shell
openssl rsa -passin pass:qwerty -in server-secret.key -out server.key
```

* Create CSR for the server
```shell
openssl req -new -subj '/C=BY/ST=Belarus/L=Minsk/O=Example server/OU=Example server unit/CN=server.example.com' -key server.key -out server.csr
```

* Create certificate for the server
```shell
openssl x509 -req -days 3650 -in server.csr -CA ca.crt -CAkey ca.key -set_serial 01 -out server.crt
```

* Convert the server certificate to PFX
```shell
openssl pkcs12 -export -passout pass:qwerty -inkey server.key -in server.crt -out server.pfx
```

* Convert the server certificate to PEM
```shell
openssl pkcs12 -passin pass:qwerty -passout pass:qwerty -in server.pfx -out server.pem
```

## SSL Client certificate

* Create private key for the client
```shell
openssl genrsa -passout pass:qwerty -out client-secret.key 4096
```

* Remove passphrase
```shell
openssl rsa -passin pass:qwerty -in client-secret.key -out client.key
```

* Create CSR for the client
```shell
openssl req -new -subj '/C=BY/ST=Belarus/L=Minsk/O=Example client/OU=Example client unit/CN=client.example.com' -key client.key -out client.csr
```

* Create the client certificate
```shell
openssl x509 -req -days 3650 -in client.csr -CA ca.crt -CAkey ca.key -set_serial 01 -out client.crt
```

* Convert the client certificate to PFX
```shell
openssl pkcs12 -export -passout pass:qwerty -inkey client.key -in client.crt -out client.pfx
```

* Convert the client certificate to PEM
```shell
openssl pkcs12 -passin pass:qwerty -passout pass:qwerty -in client.pfx -out client.pem
```

## Diffie-Hellman key exchange

* Create DH parameters
```shell
openssl dhparam -out dh4096.pem 4096
```
