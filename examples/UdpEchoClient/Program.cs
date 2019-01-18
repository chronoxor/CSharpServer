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
            Disconnect();
            while (IsConnected)
                Thread.Yield();
        }

        protected override void OnConnected()
        {
            Console.WriteLine($"Echo UDP client connected a new session with Id {Id}");

            // Start receive datagrams
            Receive();
        }

        protected override void OnDisconnected()
        {
            Console.WriteLine($"Echo UDP client disconnected a session with Id {Id}");

            // Wait for a while...
            Thread.Sleep(1000);

            // Try to connect again
            if (!_stop)
                Connect();
        }

        protected override void OnReceived(UdpEndpoint endpoint, byte[] buffer, long size)
        {
            Console.WriteLine("Incoming: " + Encoding.UTF8.GetString(buffer, 0, (int)size));

            // Continue receive datagrams
            Receive();
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
            client.Connect();
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
                    client.Disconnect();
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
