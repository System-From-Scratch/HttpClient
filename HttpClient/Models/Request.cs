using System.Text;

namespace HttpClient.Models;

public class Request
{
    public HttpMethod? Method { get; internal set; }
    public string? Data { get; init; }
    private readonly string _endpoint;
    private readonly Dictionary<string, string> _headers;

    public Request(string endpoint, string? data = null)
        :  this(HttpMethod.Get, endpoint, data)
    {
    }

    public Request(HttpMethod method, string endpoint, string? data = null)
    {
        Method = method;
        _endpoint = '/' + endpoint.TrimStart('/');
        _headers = new Dictionary<string, string>();
        Data = data;
    }

    public Request SetHeader(string key, string value)
    {
        this._headers[key] = value;
        return this;
    }

    internal string Build()
    {
        var builder = new StringBuilder();
        builder.Append(Method ?? HttpMethod.Get);
        builder.Append(' ');
        builder.Append(_endpoint);
        builder.Append(' ');
        builder.AppendLine("HTTP/1.1");

        foreach (var header in _headers)
        {
            builder.AppendLine($"{header.Key}: {header.Value}");
        }
        
        builder.AppendLine();
        builder.Append(Data);
        
        return builder.ToString();
    }
}