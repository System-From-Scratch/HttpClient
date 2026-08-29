using System.Text;
using System.Text.Json;
using HttpClient.Models;

namespace Main;

class Program
{
    static async Task Main(string[] args)
    {
        var baseUrl = new Uri("https://httpbin.org/");
        var httpClient = new HttpClient.HttpClient(baseUrl);

        var response = await httpClient.SendAsync(HttpMethod.Get, "range/4000");
        var result = await response.ReadToEndAsync();
        Console.WriteLine(result);
    }

    private async void CreateUser()
    {
        var baseUrl = new Uri("https://ca374f02032e77d96c60.free.beeceptor.com/");
        var httpClient = new HttpClient.HttpClient(baseUrl);

        var data = new
        {
            Username = "username",
            Password = "password",
            DisplayName = "John Doe",
            Description = new StringBuilder()
                .AppendLine("Hello World!")
                .AppendLine("My Birthday: 1 Jan 2026")
                .AppendLine("I love travelling")
                .ToString(),
            MyNovel = File.ReadAllText(@"C:\Users\Saurabh\Downloads\output-onlinefiletools.txt")
        };

        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            IndentSize = 4,
            WriteIndented = true
        };
        
        var stringData = JsonSerializer.Serialize(data, serializerOptions);

        var request = new Request("api/users", stringData)
            .SetHeader("Content-Type", "application/json")
            .SetHeader("Accept", "application/json");
        
        var response = await httpClient.PostAsync(request);
        var result = await response.ReadToEndAsync();
        
        var resultParsed = JsonSerializer.Deserialize<object>(result);
        
        Console.WriteLine("Response Status: {0}", response.StatusCode);
        Console.WriteLine("Headers:");

        foreach (var header in response.Headers)
        {
            Console.WriteLine("\t{0}: {1}", header.Key, header.Value);
        }
        
        Console.WriteLine("Response:");
        Console.WriteLine(JsonSerializer.Serialize(resultParsed, serializerOptions));
    }
}