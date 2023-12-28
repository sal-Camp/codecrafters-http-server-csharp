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

var request = Encoding.UTF8.GetString(buffer);
var parsedRequest = new HttpRequest(request);

var response = HttpResponse.Ok(parsedRequest);
socket.Send(response);
internal class HttpResponse
{
    internal static byte[] Ok(HttpRequest parsedRequest) {
        return Encoding.UTF8.GetBytes(
            "HTTP/1.1 200 OK\r\n\r\n" +
            "Content-Type: text/plain\r\n\r\n" +
            "Content-Length: " + $"{parsedRequest.PathContent.Length}" + "\r\n\r\n" +
            "\r\n\r\n" +
            $"{parsedRequest.PathContent}"
            );
    }

    internal static byte[] NotFound() {
        return Encoding.UTF8.GetBytes("HTTP/1.1 404 Not Found\r\n\r\n");
    }
}

internal class HttpRequest
{
    internal HttpRequest(string request)
    {
        Request = request;
        ParseRequest();
    }

    private string Request;
    public string RequestType;
    public string HttpVersion;
    public string Path;
    public string PathContent;

    internal void ParseRequest()
    {
        RequestType = Request.Split(' ')[0];
        Path = Request.Split(' ')[1];
        HttpVersion = Request.Split(' ')[2];
        PathContent = Path[(Path.IndexOf('/', Path.IndexOf('/') + 1) + 1)..];

    }
}