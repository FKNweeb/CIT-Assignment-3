
using System.Net.Sockets;
using System.Net;
using System.Text;
using System;
using System.Text.Json;
using System.Reflection;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Runtime.Intrinsics.X86;

public class Server
{
    private readonly int _port;
    private static List<Category> _categories = new List<Category>
        {
            new Category {Cid = 1, Name = "Beverages"},
            new Category {Cid = 2, Name = "Condiments"},
            new Category {Cid = 3, Name = "Confections"}
        };

    public Server(int port)
    {
        _port = port;
       
    }


    public void Run()
    {

        var server = new TcpListener(IPAddress.Loopback, _port); // IPv4 127.0.0.1 IPv6 ::1
        server.Start();

        Console.WriteLine($"Server started on port {_port}");

        while (true)
        {
            var client = server.AcceptTcpClient();
            Console.WriteLine("Client connected");
            Task.Run(()=>HandleClient(client));
           

        }


    }

    private void HandleClient(TcpClient client)
    {

        try
        {
            var stream = client.GetStream();
            string msg = ReadFromStream(stream);
            var request = FromJson(msg);

            Console.WriteLine("Message from client" + msg);

            string[] validMethods = ["create", "read", "update", "delete", "echo"];

      
            var response = new Response { };

            
            if(msg == "{}")
            {
                response.Status = "missing method missing date";
                var json = ToJson(response);
                WriteToStream(stream, json);
            }
            else
            {
                if (!validMethods.Contains(request.Method))
                {
                    response.Status = "illegal method";
                    var json = ToJson(response);
                    WriteToStream(stream, json);
                }
                else if (string.IsNullOrEmpty(request.Path) && !(request.Method == "echo"))
                {
                    response.Status = "missing resource";
                    var json = ToJson(response);
                    WriteToStream(stream, json);    
                }
                else if(string.IsNullOrEmpty(request.Date) || !IsValidDate(request.Date))
                {
                    response.Status = "missing date illegal date";
                    var json = ToJson(response);
                    WriteToStream(stream, json);
                }
                else if(RequiresBody(request.Method) && string.IsNullOrEmpty(request.Body))
                {
                    response.Status = "missing body";
                    var json = ToJson(response);
                    WriteToStream(stream, json);
                }
                else if (request.Method == "echo")
                {
                    response.Body = request.Body;
                    var json = ToJson(response);
                    WriteToStream(stream, json);
                }
                else if (RequiresBody(request.Method) && !IsJson(request.Body))
                {
                    response.Status = "illegal body";
                    var json = ToJson(response);
                    WriteToStream(stream, json);
                }
                else if (!IsPathValidForMethod(request))
                {
                    response.Status = "4 Bad Request";
                    var json = ToJson(response);
                    WriteToStream(stream, json);
                }

            }
        }
        catch (Exception)
        {
            
            throw;
        }
    }


    private bool RequiresBody(string method)
    {
        if (method == "update") { return true; }
        if (method == "create") { return true; }
        if (method == "echo") { return true; }
        return false;
    }

    private bool IdIsAllowed(string method)
    {
        if (method == "update") { return true; }
        if (method == "read") { return true; }
        if (method == "delete") { return true; }
        return false;
    }

    private bool IdRequired(string method)
    {
        if (method == "update") { return true; }
        if (method == "delete") { return true; }
        return false;
    }

 
    public bool IsPathValidForMethod(Request request)
    {
        if (CheckValidPath(request.Path))
        {
            string path = request.Path;
            Regex regex = new Regex(@"/(\d+)$");
            Match match = regex.Match(path);

            if (match.Success && (IdIsAllowed(request.Method) && IdRequired(request.Method)))
            {
                return true;
            }
        }
        return false;
    }
    public bool CheckValidPath(string path)
    {
        string pattern = "^api/categories(/[1-9]*)?$";
        Regex rg = new Regex(pattern);

        if (rg.IsMatch(path)) { return true; }
        return false;

    }

    public bool IsValidDate(string date)
    {
       if(long.TryParse(date, out long unixTime))
        {
            if (unixTime >= 0 && unixTime < 2177449200)
            {
                return true;
            }
        }
        return false;
    }
    private static string UnixTimestamp()
    {
        return DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
    }

    private string ReadFromStream(NetworkStream stream)
    {
        var buffer = new byte[1024];
        var readCount = stream.Read(buffer);
        return Encoding.UTF8.GetString(buffer, 0, readCount);
    }

    private void WriteToStream(NetworkStream stream, string msg)
    {
        var buffer = Encoding.UTF8.GetBytes(msg);
        stream.Write(buffer);
    }
  

    public static string ToJson(Response response)
    {
        return JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }

    public static Request? FromJson(string element)
    {
        return JsonSerializer.Deserialize<Request>(element, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }

    public bool  IsJson(string body)
    {
        try
        {
            if(body.StartsWith('{') && body.EndsWith('}'))
            {
                JToken.Parse(body);
            }
            else if (body.StartsWith('[') && body.EndsWith(']'))
            {
                JArray.Parse(body);
            }
            else
            {
                return false;
            }
            return true;
        }
        catch (Exception)
        {

            return false;
        }
    }
}