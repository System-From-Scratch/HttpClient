using System.Net;
using System.Net.Security;
using System.Text;
using HttpClient.Helpers;

namespace HttpClient.Models;

public class Response : IDisposable
{
    public IReadOnlyDictionary<string, string> Headers { get; private set; }
    public HttpStatusCode StatusCode { get; private set; }

    private readonly ResponseParser _parser;

    private Response(SslStream sslStream)
    {
        Headers = new Dictionary<string, string>();
        StatusCode = HttpStatusCode.OK;
        _parser = new ResponseParser(sslStream);
    }

    public static async Task<Response> CreateAsync(SslStream sslStream)
    {
        var response = new Response(sslStream);
        await response.ReadMetadata();
        return response;
    }

    public async Task<string?> ReadToEndAsync()
    {
        if (_parser.EndOfStream)
        {
            return null;
        }
        
        if (Headers.TryGetValue("Content-Length", out var contentLength))
        {
            var length = Convert.ToInt64(contentLength);
            return await _parser.ReadBytesAsync(length);
        }

        if (Headers.TryGetValue("Transfer-Encoding", out var transferEncoding))
        {
            if ("chunked".Equals(transferEncoding, StringComparison.OrdinalIgnoreCase))
            {
                var chunksBuilder = new StringBuilder();
                string? chunk;

                while ((chunk = await this._parser.ReadChunkAsync()) != null)
                {
                    chunksBuilder.Append(chunk);
                }
                
                return chunksBuilder.ToString();
            }
        }
        
        var linesBuilder = new StringBuilder();
        string? line;

        while ((line = await this._parser.ReadLineAsync()) != null)
        {
            linesBuilder.AppendLine(line);
        }
        
        return linesBuilder.ToString();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _parser.Dispose();
    }

    private async Task ReadMetadata()
    {
        var statusLine = await _parser.ReadLineAsync();

        if (string.IsNullOrWhiteSpace(statusLine))
        {
            throw new InvalidDataException("Invalid response");
        }
        
        var statusCode = statusLine.Split(' ')[1];
        StatusCode = Enum.Parse<HttpStatusCode>(statusCode);

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string? headerLine;
        while ((headerLine = await _parser.ReadLineAsync()) != string.Empty)
        {
            if (headerLine == null)
            {
                throw new InvalidDataException("Invalid response");
            }
            
            var separator = headerLine.IndexOf(':');
            
            if (separator <= 0)
            {
                throw new InvalidDataException($"Invalid HTTP header: {headerLine}");
            }
            
            var headerKey = headerLine[..separator].Trim();
            var headerValue = headerLine[(separator + 1)..].Trim();
            headers[headerKey] = headerValue;
        }
        
        Headers = headers;
    }
}