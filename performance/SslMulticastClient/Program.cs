using System;
using System.Collections.Generic;
using System.Threading;
using CSharpServer;
using NDesk.Options;

namespace SslMulticastClient
{
    class MulticastClient : SslClient
    {
        public bool Handshaked { get; set; }

        public MulticastClient(Service service, SslContext context, string address, int port) : base(service, context, address, port)
        {
        }

        protected override void OnHandshaked()
        {
            Handshaked = true;
        }

        protected override void OnReceived(byte[] buffer)
        {
            Program.TotalBytes += buffer.Length;
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
        public static DateTime TimestampStart;
        public static DateTime TimestampStop;
        public static long TotalErrors;
        public static long TotalBytes;
        public static long TotalMessages;

        static void Main(string[] args)
        {
            bool help = false;
            string address = "127.0.0.1";
            int port = 2222;
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

            // Prepare a message to send
            MessageToSend = new byte[size];

            // Create a new service
            var service = new Service(threads);

            // Start the service
            Console.Write("Service starting...");
            service.Start();
            Console.WriteLine("Done!");

            // Create and prepare a new SSL client context
            var context = new SslContext(SslMethod.TLSV12);
            context.SetVerifyMode(SslVerifyMode.VerifyPeer);
            context.LoadVerifyFile("ca.pem");

            // Create multicast clients
            var multicastClients = new List<MulticastClient>();
            for (int i = 0; i < clients; ++i)
            {
                var client = new MulticastClient(service, context, address, port);
                // client.SetupNoDelay(true);
                multicastClients.Add(client);
            }

            TimestampStart = DateTime.UtcNow;

            // Connect clients
            Console.Write("Clients connecting...");
            foreach (var client in multicastClients)
                client.Connect();
            Console.WriteLine("Done!");
            foreach (var client in multicastClients)
            {
                while (!client.Handshaked)
                    Thread.Yield();
            }
            Console.WriteLine("All clients connected!");

            // Sleep for 10 seconds...
            Console.Write("Processing...");
            Thread.Sleep(10000);
            Console.WriteLine("Done!");

            // Disconnect clients
            Console.Write("Clients disconnecting...");
            foreach (var client in multicastClients)
                client.Disconnect();
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
