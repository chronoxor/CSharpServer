using System;
using System.Text;
using CSharpServer;

namespace SslChatServer
{
    class ChatSession : SslSession
    {
        public ChatSession(SslServer server) : base(server) { }

        protected override void OnConnected()
        {
            Console.WriteLine($"Chat SSL session with Id {Id} connected!");

            // Send invite message
            string message = "Hello from SSL chat! Please send a message or '!' to disconnect the client!";
            Send(message);
        }

        protected override void OnDisconnected()
        {
            Console.WriteLine($"Chat SSL session with Id {Id} disconnected!");
        }

        protected override void OnReceived(byte[] buffer)
        {
            string message = Encoding.UTF8.GetString(buffer);
            Console.WriteLine("Incoming: " + message);

            // Multicast message to all connected sessions
            Server.Multicast(message);

            // If the buffer starts with '!' the disconnect the current session
            if (message == "!")
                Disconnect();
        }

        protected override void OnError(int error, string category, string message)
        {
            Console.WriteLine($"Chat SSL session caught an error with code {error} and category '{category}': {message}");
        }
    }

    class ChatServer : SslServer
    {
        public ChatServer(Service service, SslContext context, InternetProtocol protocol, int port) : base(service, context, protocol, port) {}

        protected override SslSession CreateSession()
        {
            return new ChatSession(this);
        }

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
            var server = new ChatServer(service, context, InternetProtocol.IPv4, port);

            // Start the server
            Console.Write("Server starting...");
            server.Start();
            Console.WriteLine("Done!");

            Console.WriteLine("Press Enter to stop the server or '!' to restart the server...");

            // Perform text input
            for (;;)
            {
                string line = Console.ReadLine();
                if (line == String.Empty)
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
