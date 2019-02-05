using System;
using System.Text;
using CSharpServer;

namespace UdpMulticastServer
{
    class MulticastServer : UdpServer
    {
        public MulticastServer(Service service, InternetProtocol protocol, int port) : base(service, protocol, port) {}

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
            var server = new MulticastServer(service, InternetProtocol.IPv4, 0);

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
