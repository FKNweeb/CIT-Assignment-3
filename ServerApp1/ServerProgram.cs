using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

var port = 5000;

var server = new TcpListener(IPAddress.Loopback, port); // IPv4 127.0.0.1 IPv6 ::1
server.Start();

Console.WriteLine($"Server started on port {port}");

while (true)
{
    var client = server.AcceptTcpClient();
    Console.WriteLine("Client connected!!!");

    try
    {
        var stream = client.GetStream();

        var buffer = new byte[1024];

        var readCount = stream.Read(buffer);
        var msg = Encoding.UTF8.GetString(buffer, 0, readCount);
        Console.WriteLine(msg);
        var response = new Response();
        if (msg == "{}")
        {
            response.Status = "missing method";
        }

        else

        {
            var request = JsonSerializer.Deserialize<Request>(msg, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            if (request.Method == "xxxx")
            {
                response.Status = "illegal method";
            }
        }

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        Console.WriteLine(json);

        buffer = Encoding.UTF8.GetBytes(json);
        stream.Write(buffer);





    }
    catch { }
}