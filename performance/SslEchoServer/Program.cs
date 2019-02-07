using System;
using CSharpServer;
using NDesk.Options;

namespace SslEchoServer
{
    class EchoSession : SslSession
    {
        public EchoSession(SslServer server) : base(server) {}

        protected override void OnReceived(byte[] buffer, long size)
        {
            // Resend the message back to the client
            SendAsync(buffer, 0, size);
        }

        protected override void OnError(int error, string category, string message)
        {
            Console.WriteLine($"Session caught an error with code {error} and category '{category}': {message}");
        }
    }

    class EchoServer : SslServer
    {
        public EchoServer(Service service, SslContext context, InternetProtocol protocol, int port) : base(service, context, protocol, port) {}

        protected override SslSession CreateSession() { return new EchoSession(this); }

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
            int port = 2222;
            int threads = Environment.ProcessorCount;

            var options = new OptionSet()
            {
                { "h|?|help",   v => help = v != null },
                { "p|port=", v => port = int.Parse(v) },
                { "t|threads=", v => threads = int.Parse(v) }
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

            Console.WriteLine();

            // Create a new service
            var service = new Service(threads);

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

            // Create a new echo server
            var server = new EchoServer(service, context, InternetProtocol.IPv4, port);
            // server.SetupNoDelay(true);
            server.SetupReuseAddress(true);
            server.SetupReusePort(true);

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
