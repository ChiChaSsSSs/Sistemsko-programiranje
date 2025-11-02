using Projekat3.Entities;
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.ML.Data;
using Microsoft.ML;
using Projekat3.SentimentAnalysis;

namespace Projekat1
{
    public class WebServer
    {
        private readonly HttpListener listener;
        private readonly string prefix;
        private readonly CancellationTokenSource cancellationToken = new();
        private readonly IObservable<HttpListenerContext> requestStream;
        private IDisposable? subscription;
        private readonly SentimentService sentimentService = new SentimentService();
        public WebServer(string p)
        {
            prefix = p;
            listener = new HttpListener();
            listener.Prefixes.Add(prefix);

            var stopSignal = Observable
               .FromEvent(
                   h => cancellationToken.Token.Register(h),
                   h => { }
               );

                requestStream = Observable
                    .Defer(() => Observable.FromAsync(listener.GetContextAsync))
                    .Repeat()
                    .TakeUntil(stopSignal)
                    .ObserveOn(TaskPoolScheduler.Default)
                        .SelectMany(context => Observable.FromAsync(async () => {
                            Logger.Log($"[START] {context.Request.Url} - Process on thread: {Thread.CurrentThread.ManagedThreadId}");
                            await ProcessRequest(context);
                            Logger.Log($"[END] {context.Request.Url} - Finished process on thread: {Thread.CurrentThread.ManagedThreadId}");
                            return context;
                        })
                    );
        }
        public void Start()
        {
            listener.Start();
            Logger.Log($"Server started at {prefix}");
            Console.WriteLine($"Server start at {prefix}");

            subscription = requestStream.Subscribe(
               _ => { },
               ex => Logger.Log($"Error while processing request: {ex.Message}"),
               () => Logger.Log("Web server stoped.")
            );
        }
        private async Task ProcessRequest(object? contextObj)
        {
            var context = (HttpListenerContext)contextObj!;
            Logger.LogRequest(context);

            try
            {
                var request = context.Request;

                string query = request.QueryString["q"]!;
                if (string.IsNullOrEmpty(query))
                {
                    byte[] badRequest = Encoding.UTF8.GetBytes("{ \"error\": \"You must provide search parameter!\" }");
                    SendResponse(context, badRequest, 400);
                    return;
                }

                using var http = new HttpClient();
                string apiUrl = $"https://www.googleapis.com/books/v1/volumes?q={query}";
                string json = await http.GetStringAsync(apiUrl);

                var books = JsonSerializer.Deserialize<BookList>(json);
                if (books?.Items == null || books.Items.Count == 0)
                {
                    byte[] badRequest = Encoding.UTF8.GetBytes("{ \"error\": \"There are no books!!\" }");
                    SendResponse(context, badRequest, 400);
                    return;
                }

                var results = new List<object>();
                var mlContext = new MLContext();

                foreach (var item in books.Items)
                {
                    var desc = item.volumeInfo?.Description ?? "No description";

                    var sentiment = sentimentService.Predict(desc);

                    results.Add(new
                    {
                        Title = item.volumeInfo?.Title,
                        Description = desc,
                        Sentiment = sentiment.Prediction ? "Positive" : "Negative",
                        Probability = sentiment.Probability
                    });
                }

                string output = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
                byte[] buffer = Encoding.UTF8.GetBytes(output);
                SendResponse(context, buffer, 200);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "ProcessRequest");
                context.Response.StatusCode = 500;
                context.Response.OutputStream.Close();
            }

        }
        private void SendResponse(HttpListenerContext context, byte[] data, int status, string contentType = "text/plain; charset=utf-8")
        {
            context.Response.ContentLength64 = data.Length;
            context.Response.ContentType = contentType;

            context.Response.OutputStream.Write(data, 0, data.Length);
            context.Response.OutputStream.Close();

            Logger.LogResponse(context, status, data.Length);
        }
        public void Stop()
        {
            cancellationToken.Cancel();
            listener.Stop();
            listener.Close();
            subscription?.Dispose();
            Logger.Log("Server stopped.");
            Console.WriteLine("Server stopped.");
        }
    }
}
