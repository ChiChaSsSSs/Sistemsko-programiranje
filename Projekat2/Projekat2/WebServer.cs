using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Projekat1
{
    public class WebServer
    {
        private readonly HttpListener listener;
        private static readonly HttpClient client = new HttpClient();
        private readonly string prefix;
        private readonly Dictionary<string, (byte[] Data, string ContentType)> cache = new Dictionary<string, (byte[], string)>();
        private readonly object cacheLock = new object();
        private readonly object dataLock = new object();
        private List<SpaceXLaunch>? allLaunches;
        private bool dataLoaded = false;
        public WebServer(string p)
        {
            prefix = p;
            listener = new HttpListener();
            listener.Prefixes.Add(prefix);

            Thread initThread = new Thread(() =>
            {
                try
                {
                    string apiData = FetchSpaceXData();
                    var list = JsonSerializer.Deserialize<List<SpaceXLaunch>>(apiData);
                    lock (dataLock)
                    {
                        allLaunches = list;
                        dataLoaded = true;
                    }
                    Logger.Log("Initial SpaceX data loaded.");
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "InitializeData thread");
                }
            });

            initThread.Start();
        }
        public void Start()
        {
            listener.Start();
            Logger.Log($"Server started at {prefix}");
            Console.WriteLine($"Server start at {prefix}");

            ThreadPool.QueueUserWorkItem(_ =>
            {
                while (listener.IsListening)
                {
                    try
                    {
                        var context = listener.GetContext();
                        Logger.LogRequest(context);
                        ThreadPool.QueueUserWorkItem(ProcessRequest, context);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "while processing request");
                    }
                }
            });
        }
        private void ProcessRequest(object? contextObj)
        {
            var context = (HttpListenerContext)contextObj!;
            Logger.LogRequest(context);

            try
            {
                List<SpaceXLaunch>? launches;
                lock (dataLock)
                {
                    if (!dataLoaded || allLaunches == null)
                    {
                        context.Response.StatusCode = 500;
                        byte[] error = Encoding.UTF8.GetBytes("{ \"error\": \"Launch data not initialized yet\" }");
                        SendResponse(context, error, "application/json; charset=utf-8");
                        return;
                    }
                    launches = allLaunches;
                }

                string urlKey = context.Request.RawUrl ?? "";
                byte[] responseBytes;
                string contentType;

                lock (cacheLock)
                {
                    if (cache.TryGetValue(urlKey, out var cachedEntry))
                    {
                        Logger.Log($"CACHE HIT => {urlKey}");
                        SendResponse(context, cachedEntry.Data, cachedEntry.ContentType);
                        return;
                    }
                }

                string path = context.Request.Url!.AbsolutePath.ToLower();
                Logger.Log($"ROUTE => {path}");

                if (path == "/")
                {
                    string responseText = "SpaceX Flight Search Server is running!";
                    responseBytes = Encoding.UTF8.GetBytes(responseText);
                    contentType = "text/plain; charset=utf-8";
                }

                else if (path == "/search")
                {
                    var query = context.Request.QueryString; // Vraca kolekciju parametara upita (key-value)

                    if (query.Count == 0)
                    {
                        context.Response.StatusCode = 400;
                        byte[] badRequest = Encoding.UTF8.GetBytes("{ \"error\": \"You must provide at least one search parameter!\" }");
                        SendResponse(context, badRequest);
                        return;
                    }

                    var filter = new SpaceXLaunch();

                    foreach (string key in query.AllKeys ?? Array.Empty<string>())
                    {
                        string value = query[key]?.Trim() ?? "";
                        switch (key!.ToLower())
                        {
                            case "rocket":
                                filter.Rocket = value;
                                break;
                            case "success":
                                if (bool.TryParse(value, out bool b))
                                    filter.Success = b;
                                break;
                            case "name":
                                filter.Name = value;
                                break;
                        }
                    }

                    var filtered = launches?.Where(l => filter.Matches(l)).ToList();

                    string jsonResult = JsonSerializer.Serialize(filtered);
                    responseBytes = Encoding.UTF8.GetBytes(jsonResult);
                    contentType = "application/json; charset=utf-8";
                }

                else
                {
                    context.Response.StatusCode = 404;
                    byte[] notFound = Encoding.UTF8.GetBytes("404 Not Found");
                    SendResponse(context, notFound);
                    return;
                }

                lock (cacheLock)
                {
                    cache[urlKey] = (responseBytes, contentType);
                }

                Logger.Log($"CACHE MISS => {urlKey}");

                SendResponse(context, responseBytes, contentType);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "ProcessRequest");
                context.Response.StatusCode = 500;
                context.Response.OutputStream.Close();
            }
        }
        private void SendResponse(HttpListenerContext context, byte[] data, string contentType = "text/plain; charset=utf-8")
        {
            context.Response.ContentLength64 = data.Length;
            context.Response.ContentType = contentType;

            context.Response.OutputStream.Write(data, 0, data.Length);
            context.Response.OutputStream.Close();

            Logger.LogResponse(context, 200, data.Length);
        }
        private string FetchSpaceXData()
        {
            try
            {
                string apiUrl = "https://api.spacexdata.com/v5/launches/past";
                using (var client = new HttpClient())
                {
                    HttpResponseMessage response = client.GetAsync(apiUrl).GetAwaiter().GetResult();
                    response.EnsureSuccessStatusCode();
                    string jsonData = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    return jsonData;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "FetchSpaceXData");
                return "{ \"error\": \"Failed to fetch data from SpaceX API\" }";
            }
        }
        public void Stop()
        {
            listener.Stop();
            listener.Close();
            Logger.Log("Server stopped.");
            Console.WriteLine("Server stopped.");
        }
    }
}
