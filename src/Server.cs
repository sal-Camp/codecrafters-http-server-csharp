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

var response = Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\r\n\r\n");

socket.Send(response);