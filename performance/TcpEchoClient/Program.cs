using System;
using System.Collections.Generic;
using System.Threading;
using CSharpServer;
using NDesk.Options;

namespace TcpEchoClient
{
    class EchoClient : TcpClient
    {
        public bool Connected { get; set; }

        public EchoClient(Service service, string address, int port, int messages) : base(service, address, port)
        {
            _messagesOutput = messages;
            _messagesInput = messages;
        }

        protected override void OnConnected()
        {
            Connected = true;
            SendMessage();
        }

        protected override void OnSent(long sent, long pending)
        {
            _sent += sent;
            if (_sent >= Program.MessageToSend.Length)
            {
                SendMessage();
                _sent -= Program.MessageToSend.Length;
            }
        }

        protected override void OnReceived(byte[] buffer, long size)
        {
            _received += size;
            while (_received >= Program.MessageToSend.Length)
            {
                ReceiveMessage();
                _received -= Program.MessageToSend.Length;
            }

            Program.TimestampStop = DateTime.UtcNow;
            Program.TotalBytes += size;
        }

        protected override void OnError(int error, string category, string message)
        {
            Console.WriteLine($"Client caught an error with code {error} and category '{category}': {message}");
            ++Program.TotalErrors;
        }

        private void SendMessage()
        {
            if (_messagesOutput-- > 0)
                SendAsync(Program.MessageToSend);
        }

        void ReceiveMessage()
        {
            if (--_messagesInput == 0)
                DisconnectAsync();
        }

        private int _messagesOutput;
        private int _messagesInput;
        private long _sent;
        private long _received;
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
            int port = 1111;
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
                // client.SetupNoDelay(true);
                echoClients.Add(client);
            }

            TimestampStart = DateTime.UtcNow;

            // Connect clients
            Console.Write("Clients connecting...");
            foreach (var client in echoClients)
                client.ConnectAsync();
            Console.WriteLine("Done!");
            foreach (var client in echoClients)
                while (!client.Connected)
                    Thread.Yield();
            Console.WriteLine("All clients connected!");

            // Wait for processing all messages
            Console.Write("Processing...");
            foreach (var client in echoClients)
                while (client.IsConnected)
                    Thread.Sleep(100);
            Console.WriteLine("Done!");

            // Stop the service
            Console.Write("Service stopping...");
            service.Stop();
            Console.WriteLine("Done!");

            Console.WriteLine();

            Console.WriteLine($"Errors: {TotalErrors}");

            Console.WriteLine();

            TotalMessages = TotalBytes / size;

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
