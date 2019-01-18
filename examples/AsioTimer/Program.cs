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
