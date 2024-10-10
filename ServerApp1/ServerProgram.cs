using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

var port = 5000;
var categories = new List<Category>
        {
            new Category { cid = 1, name = "Beverages" },
            new Category { cid = 2, name = "Condiments" },
            new Category { cid = 3, name = "Confections" }
        };

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
            response.Status = "missing method,  missing date";
        }

        else

        {
            var request = JsonSerializer.Deserialize<Request>(msg, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            if (request.Method == "xxxx")
            {
                response.Status = "illegal method";
            }
            else
            if (request.Method == "create" || request.Method == "read" || request.Method == "update" || request.Method == "delete" || request.Method == "echo")
            {
                if (request.Body == "" && request.Path == "")
                {
                    response.Status = "missing resource";
                }
                else if (!long.TryParse(request.Date, out long timestamp) && (timestamp >= 0 && timestamp <= DateTimeOffset.UtcNow.ToUnixTimeSeconds()))
                {
                    response.Status = "illegal date";
                }
                else if (request.Body == "" && request.Method != "read" && request.Method != "delete")
                {
                    response.Status = "missing body";
                }
                else if (request.Body == "Hello World" && request.Method != "echo")
                {
                    response.Status = "illegal body";
                }
                else if (request.Method == "echo")
                {
                    response.Body = request.Body;
                }
                else
                {
                    if (!request.Path.StartsWith("/api/categories"))
                    {
                        response.Status = "4 Bad Request";
                        var options = new JsonSerializerOptions
                        {
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
                        };

                        string jsonResponse = JsonSerializer.Serialize(response, options);
                        Console.WriteLine(jsonResponse);

                    }
                }

            }
        }


        /*categories.RemoveAll(c => c.cid == 2);
        foreach (var category in categories)
        {
            if(category.cid==2)
            Console.WriteLine($"cid: {category.cid}, name: {category.name}");
        }*/

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        Console.WriteLine(json);

        buffer = Encoding.UTF8.GetBytes(json);
        stream.Write(buffer);





    }
    catch { } 
}

    public static class Util
    {
        public static string ToJson(this object data)
        {
            return JsonSerializer.Serialize(data, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        }

        public static T FromJson<T>(this string element)
        {
            return JsonSerializer.Deserialize<T>(element, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        }

        public static void SendRequest(this TcpClient client, string request)
        {
            var msg = Encoding.UTF8.GetBytes(request);
            client.GetStream().Write(msg, 0, msg.Length);
        }

        public static Response ReadResponse(this TcpClient client)
        {
            var strm = client.GetStream();
            //strm.ReadTimeout = 250;
            byte[] resp = new byte[2048];
            using (var memStream = new MemoryStream())
            {
                int bytesread = 0;
                do
                {
                    bytesread = strm.Read(resp, 0, resp.Length);
                    memStream.Write(resp, 0, bytesread);

                } while (bytesread == 2048);

                var responseData = Encoding.UTF8.GetString(memStream.ToArray());


                //return JsonSerializer.Deserialize<Response>(responseData);
                // if the naming policy is used you need to do the same on the server side
                return JsonSerializer.Deserialize<Response>(responseData, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            }
        }
    }
