using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

class ProxyServer
{
    public static async Task Main()
    {
        var listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:8080/");
        listener.Start();
        Console.WriteLine("Proxy server running on http://localhost:8080/");

        while (true)
        {
            var context = await listener.GetContextAsync();
            _ = HandleRequest(context);
        }
    }

    private static async Task HandleRequest(HttpListenerContext context)
    {
        var request = context.Request;
        var url = request.Url.ToString();
        Console.WriteLine($"Request for: {url}");

        var requestMethod = request.HttpMethod;
        var requestStream = request.InputStream;

        HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
        webRequest.Method = requestMethod;

        using (var reqStream = request.GetInputStream())
        {
            if (reqStream != null)
            {
                using (var reqStreamCopy = new MemoryStream())
                {
                    await reqStream.CopyToAsync(reqStreamCopy);
                    reqStreamCopy.Position = 0;
                    await reqStreamCopy.CopyToAsync(webRequest.GetRequestStream());
                }
            }
        }

        using (var webResponse = (HttpWebResponse)await webRequest.GetResponseAsync())
        {
            context.Response.StatusCode = (int)webResponse.StatusCode;
            using (var stream = webResponse.GetResponseStream())
            {
                await stream.CopyToAsync(context.Response.OutputStream);
            }
        }

        context.Response.Close();
    }
}
