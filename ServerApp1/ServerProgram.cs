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
            var request = msg.FromJson<Request>(); 
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
                    }
                    else
                    {
                        string prefix = "/api/categories";
                        var partUrl = request.Path.Substring(prefix.Length);
                        Console.WriteLine(partUrl);
                        if (partUrl == "")
                        {
                            if ( request.Method == "update" || request.Method == "delete")
                            {
                                response.Status = "4 Bad Request";
                            }
                            else if (request.Method == "read")
                            {
                                response.Status = "1 Ok";
                                //response.Body = "";
                                response.Body = categories.ToJson();
                            }
                            else if (request.Method == "create")
                            {
                                var reqBody = request.Body.FromJson<Category>();
                                int lastCid = categories.Max(c => c.cid);
                                categories.Add(new Category { cid = lastCid + 1, name = reqBody.name });
                                response.Status = "2 Created";
                                response.Body = categories.FirstOrDefault(c => c.cid == lastCid + 1).ToJson();
                            } 
                        }
                        else
                        {
                            if (partUrl.StartsWith("/"))
                            {
                                if (request.Method == "create")
                                {
                                    response.Status = "4 Bad Request";
                                }
                                else
                                {
                                    partUrl = partUrl.TrimStart('/');

                                    if (int.TryParse(partUrl, out int reqCid))
                                    {
                                        //Console.WriteLine("we get Cid to read,delete and update");
                                        if(categories.Exists(c => c.cid == reqCid))
                                        {
                                            //Console.WriteLine("we can do read,delete and update");
                                            if(request.Method == "read")
                                            {
                                                response.Status = "1 Ok";
                                                //response.Body = "";
                                                Console.WriteLine(categories.FirstOrDefault(c => c.cid == reqCid));
                                                response.Body = categories.FirstOrDefault(c => c.cid == reqCid).ToJson();
                                            }
                                            else if (request.Method == "update")
                                            {
                                                var updCat = categories.FirstOrDefault(c => c.cid == reqCid);
                                                var reqBody = request.Body.FromJson<Category>();
                                                updCat.name = reqBody.name;
                                                response.Status = "3 updated";
                                                //response.Body = "";
                                                response.Body = updCat.ToJson();
                                            }
                                            else if (request.Method == "delete")
                                            {
                                                categories.RemoveAll(c => c.cid == reqCid);
                                                response.Status = "1 Ok";
                                            }
                                        }
                                        else
                                        {
                                            response.Status = "5 not found";
                                        }

                                    }
                                    else
                                    {
                                        response.Status = "4 Bad Request";
                                    }
                                }
                            }
                            else
                            {
                                response.Status = "4 Bad Request";
                            }   
                        }       
                    }
                }
            }
        }
       var json =response.ToJson();
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
    }

