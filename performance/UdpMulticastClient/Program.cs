using System;
using System.Collections.Generic;
using System.Threading;
using CSharpServer;
using NDesk.Options;

namespace UdpMulticastClient
{
    class MulticastClient : UdpClient
    {
        public string Multicast { get; set; }

        public MulticastClient(Service service, string address, string multicast, int port) : base(service, address, port)
        {
            Multicast = multicast;
        }

        protected override void OnConnected()
        {
            // Join UDP multicast group
            JoinMulticastGroup(Multicast);

            // Start receive datagrams
            ReceiveAsync();
        }

        protected override void OnReceived(UdpEndpoint endpoint, byte[] buffer, long size)
        {
            Program.TotalBytes += size;

            // Continue receive datagrams
            ReceiveAsync();
        }

        protected override void OnError(int error, string category, string message)
        {
            Console.WriteLine($"Client caught an error with code {error} and category '{category}': {message}");
            ++Program.TotalErrors;
        }
    }

    class Program
    {
        public static byte[] MessageToSend;
        public static DateTime TimestampStart = DateTime.UtcNow;
        public static DateTime TimestampStop = DateTime.UtcNow;
        public static long TotalErrors;
        public static long TotalBytes;
        public static long TotalMessages;

        static void Main(string[] args)
        {
            bool help = false;
            string address = "239.255.0.1";
            int port = 3333;
            int threads = Environment.ProcessorCount;
            int clients = 100;
            int size = 32;

            var options = new OptionSet()
            {
                { "h|?|help",   v => help = v != null },
                { "a|address=", v => address = v },
                { "p|port=", v => port = int.Parse(v) },
                { "t|threads=", v => threads = int.Parse(v) },
                { "c|clients=", v => clients = int.Parse(v) },
                { "s|size=", v => size = int.Parse(v) }
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

            Console.WriteLine($"Server address: {address}");
            Console.WriteLine($"Server port: {port}");
            Console.WriteLine($"Working threads: {threads}");
            Console.WriteLine($"Working clients: {clients}");
            Console.WriteLine($"Message size: {size}");

            Console.WriteLine();

            // Prepare a message to send
            MessageToSend = new byte[size];

            // Create a new service
            var service = new Service(threads);

            // Start the service
            Console.Write("Service starting...");
            service.Start();
            Console.WriteLine("Done!");

            // Create multicast clients
            var multicastClients = new List<MulticastClient>();
            for (int i = 0; i < clients; ++i)
            {
                var client = new MulticastClient(service, "0.0.0.0", address, port);
                client.SetupMulticast(true);
                multicastClients.Add(client);
            }

            TimestampStart = DateTime.UtcNow;

            // Connect clients
            Console.Write("Clients connecting...");
            foreach (var client in multicastClients)
                client.ConnectAsync();
            Console.WriteLine("Done!");
            foreach (var client in multicastClients)
                while (!client.IsConnected)
                    Thread.Yield();
            Console.WriteLine("All clients connected!");

            // Sleep for 10 seconds...
            Console.Write("Processing...");
            Thread.Sleep(10000);
            Console.WriteLine("Done!");

            // Disconnect clients
            Console.Write("Clients disconnecting...");
            foreach (var client in multicastClients)
                client.DisconnectAsync();
            Console.WriteLine("Done!");
            foreach (var client in multicastClients)
                while (client.IsConnected)
                    Thread.Yield();
            Console.WriteLine("All clients disconnected!");

            TimestampStop = DateTime.UtcNow;

            // Stop the service
            Console.Write("Service stopping...");
            service.Stop();
            Console.WriteLine("Done!");

            Console.WriteLine();

            Console.WriteLine($"Errors: {TotalErrors}");

            Console.WriteLine();

            TotalMessages = TotalBytes / size;

            Console.WriteLine($"Multicast time: {Service.GenerateTimePeriod((TimestampStop - TimestampStart).TotalMilliseconds)}");
            Console.WriteLine($"Total data: {Service.GenerateDataSize(TotalBytes)}");
            Console.WriteLine($"Total messages: {TotalMessages}");
            Console.WriteLine($"Data throughput: {Service.GenerateDataSize((long)(TotalBytes / (TimestampStop - TimestampStart).TotalSeconds))}/s");
            if (TotalMessages > 0)
            {
                Console.WriteLine($"Message latency: {Service.GenerateTimePeriod((TimestampStop - TimestampStart).TotalMilliseconds / TotalMessages)}");
                Console.WriteLine($"Message throughput: {(long)(TotalMessages / (TimestampStop - TimestampStart).TotalSeconds)} msg/s");
            }
        }
    }
}
