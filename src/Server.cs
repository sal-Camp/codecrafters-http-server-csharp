using System.Net;
using System.Net.Sockets;
using System.Text;

// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.WriteLine("Logs from your program will appear here!");

TcpListener server = new TcpListener(IPAddress.Any, 4221);
server.Start();

string? directoryPath = null;

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--directory" && i+1 < args.Length)
        directoryPath = args[i + 1];
}

while (true)
{
    var socket = server.AcceptSocket();
    var clientThread = new Thread(() => HandleRequest(socket, directoryPath));
    clientThread.Start();
}

static void HandleRequest(Socket socket, string? directoryPath = null)
{
    var buffer = new byte[1024];
    socket.Receive(buffer);

    var request = new HttpRequest(buffer);
    byte[] response;

    if (request.Method == MethodType.Post)
    {
        if (request.Body is not null)
        {
            Console.WriteLine($"Writing to file @ {directoryPath}" + request.FilePath);
            File.WriteAllText($"{directoryPath}"  + request.FilePath, request.Body);
            var written = File.ReadAllText($"{directoryPath}" + request.FilePath);
            Console.WriteLine($"Written: {written}");
        }
        response = HttpResponse.Created();
    } else if (request.Path.Contains("echo")) {
        string text = request.Path[(6)..]; // get everything after '/echo/'
        response = HttpResponse.Ok(text);
    } else if (request.Path.Contains("/user-agent")) {
        response = HttpResponse.Ok(request.Headers.UserAgent);
    } else if (request.Path.Contains("/files") && directoryPath != null) {
        response = File.Exists($"{directoryPath}" + "/" + request.FilePath) ? HttpResponse.OkWithFileContent(File.ReadAllText($"{directoryPath}" + "/" + request.FilePath)) : HttpResponse.NotFound();
    } else {
        response = request.Path == "/" ? HttpResponse.Ok() : HttpResponse.NotFound();
    }

    socket.Send(response); // send response to client
    socket.Close();
}

internal class HttpRequest
{
    public MethodType Method { get; }
    public string Path { get; }
    public string? FilePath { get; }
    public string? Body { get; }
    public HttpHeaders Headers { get; }
    public HttpRequest(byte[] buffer)
    {
        var filteredBuffer = buffer.Where(b => b != 0).ToArray();
        var requestString = Encoding.UTF8.GetString(filteredBuffer);

        Method = requestString.Split("/")[0].Contains("GET") ? MethodType.Get : MethodType.Post;
        Path = requestString.Split("\r\n")[0].Split(' ')[1];
        if (Path.Contains("/files"))
        {
            FilePath = Path[(7)..]; // everything after '/files/'
        }

        int bodyIndex = requestString.IndexOf("\r\n\r\n", StringComparison.Ordinal) + 4;
        Body = requestString[bodyIndex..].Trim('\0');
        Console.WriteLine($"Body: {Body}");
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

    internal static byte[] Ok(string? body = null)
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

    internal static byte[] OkWithFileContent(string? body = null)
    {
        var contentType = $"Content-Type: application/octet-stream\r\n";
        if (body is null)
            return Encoding.UTF8.GetBytes(BasicResponse + "\r\n" + contentType + "\r\n");

        var response = BasicResponse + contentType + $"Content-Length: {body.Length}\r\n\r\n" + $"{body}";

        Console.WriteLine(response);
        return Encoding.UTF8.GetBytes(response);
    }

    internal static byte[] Created()
    {
        var createdResponse = Encoding.UTF8.GetBytes("HTTP/1.1 201 Created\r\n");
        Console.WriteLine($"{Encoding.UTF8.GetString(createdResponse)}");
        return createdResponse;
    }
}

internal enum MethodType
{
    Get,
    Post
}