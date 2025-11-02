using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Projekat1
{
    public static class Logger
    {
        private static readonly object lockObj = new object(); // Objekat za sinhronizaciju
        private static readonly string logFilePath = "server.log.txt"; // Putanja do log fajla
        public static void Log(string message)
        {
            string entry = FormatEntry(message);

            lock (lockObj) // Deo koda koji se blokira kada jedna nit upisuje u zajednicki fajl
            {
                try
                {
                    Console.WriteLine(entry);
                    File.AppendAllText(logFilePath, entry + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[Logger error] {ex.GetType().Name}: {ex.Message}"); // U slucaju greske logera ne puca cela aplikacija
                }
            }
        }
        public static void LogRequest(System.Net.HttpListenerContext context)
        {
            if (context == null)
                return;

            string method = context.Request?.HttpMethod ?? "UNKNOWN";
            string url = context.Request?.Url?.ToString() ?? "unknown-url";
            string remote = context.Request?.RemoteEndPoint?.ToString() ?? "unknown-client";
            Log($"REQUEST => {method} {url} from {remote}");
        }
        public static void LogResponse(System.Net.HttpListenerContext context, int statusCode, long responseLength)
        {
            string url = context.Request?.Url?.ToString() ?? "unknown-url";
            Log($"RESPONSE => {statusCode} {responseLength} bytes for {url}");
        }
        public static void LogError(Exception ex, string context = null)
        {
            if (ex == null)
                return;

            string ctx = string.IsNullOrEmpty(context) ? "" : $" ({context})";
            Log($"ERROR    => {ex.GetType().Name}{ctx}: {ex.Message}");
        }
        private static string FormatEntry(string message)
        {
            return $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [T{Thread.CurrentThread.ManagedThreadId}] {message}";
        }
    }
}
