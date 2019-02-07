using System;
using System.Collections.Generic;
using System.Threading;
using CSharpServer;
using NDesk.Options;

namespace UdpEchoClient
{
    class EchoClient : UdpClient
    {
        public bool Connected { get; set; }

        public EchoClient(Service service, string address, int port, int messages) : base(service, address, port)
        {
            _messages = messages;
        }

        protected override void OnConnected()
        {
            Connected = true;

            // Start receive datagrams
            ReceiveAsync();

            SendMessage();
        }

        protected override void OnReceived(UdpEndpoint endpoint, byte[] buffer, long size)
        {
            Program.TimestampStop = DateTime.UtcNow;
            Program.TotalBytes += size;
            ++Program.TotalMessages;

            // Continue receive datagrams
            ReceiveAsync();

            SendMessage();
        }

        protected override void OnError(int error, string category, string message)
        {
            Console.WriteLine($"Client caught an error with code {error} and category '{category}': {message}");
            ++Program.TotalErrors;
        }

        private void SendMessage()
        {
            if (_messages-- > 0)
                Send(Program.MessageToSend);
            else
                DisconnectAsync();
        }

        private int _messages;
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
            int port = 3333;
            int threads = Environment.ProcessorCount;
            int clients = 100;
            int messages = 1000000;
            int size = 32;

            var options = new OptionSet()
            {
                { "h|?|help",   v => help = v != null },
                { "a|address=", v => address = v },
                { "p|port=", v => port = int.Parse(v) },
                { "t|threads=", v => threads = int.Parse(v) },
                { "c|clients=", v => clients = int.Parse(v) },
                { "m|messages=", v => messages = int.Parse(v) },
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
            Console.WriteLine($"Messages to send: {messages}");
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

            // Create echo clients
            var echoClients = new List<EchoClient>();
            for (int i = 0; i < clients; ++i)
            {
                var client = new EchoClient(service, address, port, messages / clients);
                echoClients.Add(client);
            }

            TimestampStart = DateTime.UtcNow;

            // Connect clients
            Console.Write("Clients connecting...");
            foreach (var client in echoClients)
                client.ConnectAsync();
            Console.WriteLine("Done!");
            foreach (var client in echoClients)
            {
                while (!client.Connected)
                    Thread.Yield();
            }
            Console.WriteLine("All clients connected!");

            // Wait for processing all messages
            Console.Write("Processing...");
            foreach (var client in echoClients)
            {
                while (client.IsConnected)
                    Thread.Sleep(100);
            }
            Console.WriteLine("Done!");

            // Stop the service
            Console.Write("Service stopping...");
            service.Stop();
            Console.WriteLine("Done!");

            Console.WriteLine();

            Console.WriteLine($"Errors: {TotalErrors}");

            Console.WriteLine();

            Console.WriteLine($"Round-trip time: {Service.GenerateTimePeriod((TimestampStop - TimestampStart).TotalMilliseconds)}");
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
