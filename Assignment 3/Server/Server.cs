using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

public class Server{
    private readonly int _port;
    private static List<Category> _categories = new List<Category> {
        new Category { cid = 1, name = "Beverages"},
        new Category { cid = 2, name = "Condiments"},
        new Category { cid = 3, name = "Confections"}
    };
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
            } else {
                var request = FromJson(msg);

                string[] validMethods = {"create", "read", "update", "delete", "echo"};

                if (!validMethods.Contains(request.Method)) { 
                    response.Status = "illegal method";
                 }
                 else if (request.Path == null && !(request.Method == "echo")) {
                    response.Status = "missing resource";
                 }
                 else if (request.Date == null){
                    response.Status = "missing date";
                 }
                 else if (!IsValidUnixTimestamp(request.Date)){
                    response.Status = "illegal date";
                 }
                 else if (RequiresBody(request.Method) && request.Body == null){
                    response.Status = "missing body";
                 }
                 else if (request.Method == "echo"){
                    response.Status = "1 OK";
                    response.Body = request.Body;
                 }
                 else if (RequiresBody(request.Method) && !IsJsonType(request.Body)){
                    response.Status = "illegal body";
                 }
                 else if (!ValidatePath(request.Method, request.Path)){
                    response.Status = "4 Bad Request";
                 }
                 else {
                    switch (request.Method)
                    {
                        case "update":
                            if (UpdateData( request.Path, request.Body)){
                                response.Status = "3 Updated";
                            } else {
                                response.Status = "5 Not Found";
                            }
                            break;
                        case "read":
                            response.Status = "1 Ok";
                            var body = ReadData(request.Path);
                            response.Body = body;
                            if (body == "") {
                                response.Status = "5 Not Found";
                            }
                            break;
                        case "create":
                            response.Status = "2 Created";
                            response.Body = Add(request.Body);
                            break;
                        case "delete":
                            response.Status = "1 Ok";
                            if (!Delete(request.Path)) { response.Status = "5 Not found"; }
                            break;
                    }
                }
            }
            var json = ToJson(response);
            WriteToStream(stream, json);
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
    public static bool RequiresBody(string method){
        if (method == "update") { return true; }
        else if (method == "create") { return true; }
        else if (method == "echo") { return true; }
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
        if (!r.Groups[0].Success) { return false; }
        if (!isIdPresent && requiresId.Contains(method)) { return false; }
        else if (isIdPresent && notAllowedId.Contains(method)) { return false; }
        return true;
    } 
    public static bool UpdateData(string path, string body){
        var match = Regex.Match(path, "[0-9]+$");
        Category jsonBody = JsonSerializer.Deserialize<Category>(body);
        if (!DoesExist(jsonBody.cid)) { return false; }
        if(!DoesExist(int.Parse(match.Value))) { return false; }
        _categories[jsonBody.cid-1].name = jsonBody.name;
        return true;
    }
    public static string ReadData(string path){
        var match = Regex.Match(path, "[0-9]+$");
        if (match.Success) { 
            if (DoesExist(int.Parse(match.Value))) { return JsonSerializer.Serialize(_categories[int.Parse(match.Value)-1]); 
            }
            return "";
        }
        return JsonSerializer.Serialize(_categories);
    }
    public static bool DoesExist(int index){
        return index <= _categories.Capacity;
    }
    public static string Add(string body){
        Category jsonBody = JsonSerializer.Deserialize<Category>(body);
        _categories.Add(new Category { cid = _categories.Capacity, name = jsonBody.name });
        return JsonSerializer.Serialize(_categories[_categories.Capacity - 1]);
    }
    public static bool Delete(string path){
        var match = Regex.Match(path, "[0-9]+$");
        var value = int.Parse(match.Value);
        if (!DoesExist(value)) { return false; }
        _categories.RemoveAt(value - 1);
        return true;
    }
}