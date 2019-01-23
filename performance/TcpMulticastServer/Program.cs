using System;
using System.Threading;
using System.Threading.Tasks;
using CSharpServer;
using NDesk.Options;

namespace TcpMulticastServer
{
    class MulticastSession : TcpSession
    {
        public MulticastSession(TcpServer server) : base(server) {}

        protected override bool OnSending(long size)
        {
            // Limit session send buffer to 1 megabyte
            return BytesPending + size <= 1 * 1024 * 1024;
        }

        protected override void OnError(int error, string category, string message)
        {
            Console.WriteLine($"Session caught an error with code {error} and category '{category}': {message}");
        }
    }

    class MulticastServer : TcpServer
    {
        public MulticastServer(Service service, InternetProtocol protocol, int port) : base(service, protocol, port) {}

        protected override TcpSession CreateSession()
        {
            return new MulticastSession(this);
        }

        protected override void OnError(int error, string category, string message)
        {
            Console.WriteLine($"Server caught an error with code {error} and category '{category}': {message}");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            bool help = false;
            int port = 1111;
            int threads = Environment.ProcessorCount;
            int messagesRate = 1000000;
            int messageSize = 32;

            var options = new OptionSet()
            {
                { "h|?|help",   v => help = v != null },
                { "p|port=", v => port = int.Parse(v) },
                { "t|threads=", v => threads = int.Parse(v) },
                { "m|messages=", v => messagesRate = int.Parse(v) },
                { "s|size=", v => messageSize = int.Parse(v) }
            };

            try
            {
                options.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write("Command line error: ");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `--help' to get usage information.");
                return;
            }

            if (help)
            {
                Console.WriteLine("Usage:");
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            Console.WriteLine($"Server port: {port}");
            Console.WriteLine($"Working threads: {threads}");
            Console.WriteLine($"Messages rate: {messagesRate}");
            Console.WriteLine($"Message size: {messageSize}");

            // Create a new service
            var service = new Service(threads);

            // Start the service
            Console.Write("Service starting...");
            service.Start();
            Console.WriteLine("Done!");

            // Create a new echo server
            var server = new MulticastServer(service, InternetProtocol.IPv4, port);
            // server.SetupNoDelay(true);
            server.SetupReuseAddress(true);
            server.SetupReusePort(true);

            // Start the server
            Console.Write("Server starting...");
            server.Start();
            Console.WriteLine("Done!");

            // Start the multicasting thread
            bool multicasting = true;
            var multicaster = Task.Factory.StartNew(() =>
            {
                // Prepare message to multicast
                byte[] message = new byte[messageSize];

                // Multicasting loop
                while (multicasting)
                {
                    var start = DateTime.UtcNow;
                    for (int i = 0; i < messagesRate; ++i)
                        server.Multicast(message);
                    var end = DateTime.UtcNow;

                    // Sleep for remaining time or yield
                    var milliseconds = (int)(end - start).TotalMilliseconds;
                    if (milliseconds < 1000)
                        Thread.Sleep(milliseconds);
                    else
                        Thread.Yield();
                }
            });

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

            // Stop the multicasting thread
            multicasting = false;
            multicaster.Wait();

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
