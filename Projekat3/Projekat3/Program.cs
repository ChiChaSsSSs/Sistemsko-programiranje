namespace Projekat1
{
    public class Program
    {
        static void Main(string[] args)
        {
            string url = "http://localhost:8080/";
            
            var server = new WebServer(url);
            server.Start();

            Console.WriteLine("Go to http://localhost:8080/ to see the server response.");
            Console.WriteLine("Press Enter to stop the server...");

            Console.ReadLine();
            server.Stop();
        }
    }
}
