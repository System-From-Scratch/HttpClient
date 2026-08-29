using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using HttpClient.Models;

namespace HttpClient;

public class HttpClient
{
    private readonly Uri _baseUrl;
    
    public HttpClient(Uri baseUrl)
    {
        _baseUrl = baseUrl;
    }

    private async Task<Response> SendAsync(Request request)
    {
        var payload = request.Data;
        var payloadCount = string.IsNullOrEmpty(payload)
            ? 0
            : Encoding.UTF8.GetByteCount(payload);

        var requestString = request
            .SetHeader("Host", _baseUrl.Host)
            .SetHeader("Content-Length", payloadCount.ToString())
            .Build();

        var bytes = Encoding.UTF8.GetBytes(requestString);

        using var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync(_baseUrl.Host, _baseUrl.Port);
        var sslStream = new SslStream(tcpClient.GetStream());
        await sslStream.AuthenticateAsClientAsync(_baseUrl.Host);
        
        await sslStream.WriteAsync(bytes);
        return await Response.CreateAsync(sslStream);
    }

    public Task<Response> SendAsync(HttpMethod method, string endpoint, string? data = null)
    {
        return SendAsync(new Request(method, endpoint, data));
    }
    
    public Task<Response> PostAsync(Request request)
    {
        request.Method = HttpMethod.Post;
        return SendAsync(request);
    }

    public Task<Response> GetAsync(Request request)
    {
        request.Method = HttpMethod.Get;
        return SendAsync(request);
    }

    public Task<Response> PutAsync(Request request)
    {
        request.Method = HttpMethod.Put;
        return SendAsync(request);
    }

    public Task<Response> PatchAsync(Request request)
    {
        request.Method = HttpMethod.Patch;
        return SendAsync(request);
    }

    public Task<Response> DeleteAsync(Request request)
    {
        request.Method = HttpMethod.Delete;
        return SendAsync(request);
    }
}