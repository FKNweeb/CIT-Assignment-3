using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

public class Server{
    private readonly int _port;
    public Server(int port){
        _port = port;
    }

    public void Run() {
        var server = new TcpListener(IPAddress.Loopback, _port);
        server.Start();

        Console.WriteLine($"Server started on port {_port}");

        while (true)
        {
            var client = server.AcceptTcpClient();
            Console.WriteLine("Client Connected!!");
            Task.Run(() => HandleClient(client));
        }
    }
    private void HandleClient(TcpClient client){
        try
        {
            var stream = client.GetStream();
            string msg = ReadFromStream(stream);

            Console.WriteLine($"Message from client: " + msg);
            
            var response = new Response{};

            if (msg == "{}") { 
                response.Status = "missing method missing date";
                var json = ToJson(response);
                WriteToStream(stream, json);
            } else {
                
                var request = FromJson(msg);

                string[] validMethods = {"create", "read", "update", "delete", "echo"};

                if (!validMethods.Contains(request.Method)) { 
                    response.Status = "illegal method";
                    var json = ToJson(response);
                    WriteToStream(stream, json);
                 }
                 else if (request.Path == null && !(request.Method == "echo")) {
                    response.Status = "missing resource";
                    var json = ToJson(response);
                    WriteToStream(stream, json);
                 }
                 else if (request.Date == null){
                    response.Status = "missing date";
                    var json = ToJson(response);
                    WriteToStream(stream, json);
                 }
                 else if (!IsValidUnixTimestamp(request.Date)){
                    response.Status = "illegal date";
                    var json = ToJson(response);
                    WriteToStream(stream, json);
                 }
                 else if (RequiresBody(request.Method) && request.Body == null){
                    response.Status = "missing body";
                    var json = ToJson(response);
                    WriteToStream(stream, json);
                 }
                 else if (request.Method == "echo"){
                    response.Status = "1 OK";
                    response.Body = request.Body;
                    var json = ToJson(response);
                    WriteToStream(stream, json);
                 }
                 else if (RequiresBody(request.Method) && !IsJsonType(request.Body)){
                    response.Status = "illegal body";
                    var json = ToJson(response);
                    WriteToStream(stream, json);
                 }
                 else if (!ValidatePath(request.Method, request.Path)){
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
    public string ReadFromStream(NetworkStream stream){
        var buffer = new byte[1024];
        int readCount = stream.Read(buffer);
        return Encoding.UTF8.GetString(buffer, 0, readCount);
    }

    public void WriteToStream(NetworkStream stream, string msg){
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
    public static bool IsValidUnixTimestamp(string input)
    {
        // Step 1: Try to parse the string to a long
        if (long.TryParse(input, out long unixTime))
        {
            // Step 2: Check if it's within the valid range for Unix timestamps
            return unixTime >= 0 && unixTime <= 253402300799; // 253402300799 = 9999-12-31 23:59:59 UTC
        }

        return false; // If parsing fails, it's not a valid Unix timestamp
    }
    public static bool RequiresBody(string input){
        if (input == "update") { return true; }
        else if (input == "create") { return true; }
        return false;
    }
    public static bool IsJsonType(string input){
        
        try
        {
            if (!(input.StartsWith("{") && input.EndsWith("}"))) { return false; }
            JsonDocument.Parse(input);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
    public static bool ValidatePath(string method, string path){
        if (path.EndsWith("/")) { return false; }
        string[] requiresId = {"update", "delete"};
        string[] notAllowedId = { "create" };
        var r = Regex.Match(path, "^/api/categories(/[1-9]*)?$");
        
        bool isIdPresent = r.Groups[1].Success;
        if (!isIdPresent && requiresId.Contains(method)) { return false; }
        else if (isIdPresent && notAllowedId.Contains(method)) { return false; }
        return true;
    } 
}