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

var requestTarget = request.Split(' ')[1];
var path = requestTarget.Split('/');
byte[] response;

if (path[1] == "echo")
{
    var textStart = request.IndexOf("/echo/");
    var textEnd = request.IndexOf("HTTP/1.1");
    var text = request.Substring(textStart + 6, textEnd - textStart - 7);
    response = HttpResponse.Ok(text);
    socket.Send(response);
}
else
{
    response = requestTarget == "/" ? HttpResponse.Ok() : HttpResponse.NotFound();
}

socket.Send(response);


internal class HttpResponse
{
    internal static byte[] Ok(string? body = null)
    {
        var basicResponse = $"HTTP/1.1 200 OK\r\n";
        if (body is null)
            return Encoding.UTF8.GetBytes(basicResponse + "\r\n");
        string response = "HTTP/1.1 200 OK\r\n" + "Content-Type: text/plain\r\n" +
                          $"Content-Length: {body.Length}\r\n" + "\r\n" + $"{body}";
        Console.WriteLine(response);

        return Encoding.UTF8.GetBytes(response);
    }

    internal static byte[] NotFound() {
        return Encoding.UTF8.GetBytes("HTTP/1.1 404 Not Found\r\n\r\n");
    }
}