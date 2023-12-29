using System.Net;
using System.Net.Sockets;
using System.Text;

// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.WriteLine("Logs from your program will appear here!");

TcpListener server = new TcpListener(IPAddress.Any, 4221);
server.Start();
var socket = server.AcceptSocket(); // wait for client
var buffer = new byte[1024];
socket.Receive(buffer);
var request = new HttpRequest(buffer);
byte[] response;

if (request.Path.Contains("echo"))
{
    var text = request.Path[(6)..];
    response = HttpResponse.OK(text);
} else if (request.Path.Contains("/user-agent"))
{
    response = HttpResponse.OK(request.Headers.UserAgent);
}
else
{
    response = request.Path == "/" ? HttpResponse.OK() : HttpResponse.NotFound();
}

socket.Send(response);

internal class HttpRequest
{
    public string Method { get; }
    public string Path { get; }
    public HttpHeaders Headers { get; }
    public HttpRequest(byte[] buffer)
    {
        var requestString = Encoding.UTF8.GetString(buffer);

        Method = requestString.Split("/")[0];
        Path = requestString.Split("\r\n")[0].Split(' ')[1];
        Headers = new HttpHeaders(requestString);
    }

    internal class HttpHeaders
    {
        public string UserAgent { get; }
        public HttpHeaders(string requestString)
        {
            var uaIndex = requestString.IndexOf("User-Agent", StringComparison.Ordinal);
            UserAgent = requestString[(uaIndex + "User-Agent: ".Length)..].Split("\r")[0];
        }
    }
}

internal static class HttpResponse
{
    private const string BasicResponse = "HTTP/1.1 200 OK\r\n";

    internal static byte[] OK(string? body = null)
    {
        if (body is null)
            return Encoding.UTF8.GetBytes(BasicResponse + "\r\n");
        var response = BasicResponse + "Content-Type: text/plain\r\n" +
                       $"Content-Length: {body.Length}\r\n\r\n" + $"{body}";

        Console.WriteLine(response);

        return Encoding.UTF8.GetBytes(response);
    }

    internal static byte[] NotFound()
    {
        return Encoding.UTF8.GetBytes("HTTP/1.1 404 Not Found\r\n\r\n");
    }
}