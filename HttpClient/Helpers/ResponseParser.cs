using System.Net.Security;
using System.Text;

namespace HttpClient.Helpers;

internal class ResponseParser : IDisposable
{
    private const int BufferSize = 4096;
    
    private readonly SslStream _sslStream;
    private readonly byte[] _buffer;
    
    private int _bufferOffset;
    private int _bufferLength;
    
    public bool EndOfStream { get; private set; }

    public ResponseParser(SslStream sslStream)
    {
        _sslStream = sslStream;
        _buffer =  new byte[BufferSize];
        _bufferOffset = 0;
        _bufferLength = 0;
    }

    public async Task<string?> ReadLineAsync()
    {
        if (EndOfStream)
        {
            return null;
        }
        
        var bytes = new List<byte>();
        while (true)
        {
            if (_bufferOffset == _bufferLength)
            {
                _bufferOffset = 0;

                _bufferLength = await _sslStream.ReadAsync(_buffer);

                if (_bufferLength == 0)
                {
                    EndOfStream = true;
                    
                    return bytes.Count == 0
                        ? null
                        : throw new InvalidDataException("Incomplete response");
                }
            }

            while (_bufferOffset < _bufferLength)
            {
                bytes.Add(_buffer[_bufferOffset++]);

                if (bytes.Count >= 2 && bytes[^1] == '\n' && bytes[^2] == '\r')
                {
                    bytes.RemoveRange(bytes.Count - 2, 2);
                    return Encoding.ASCII.GetString(bytes.ToArray());
                }
            }
        }
    }

    public async Task<string?> ReadBytesAsync(long count)
    {
        if (EndOfStream)
        {
            return null;
        }
        
        var bytes = new byte[count];
        var writeOffset = 0;

        while (true)
        {
            if (_bufferOffset == _bufferLength)
            {
                _bufferOffset = 0;

                _bufferLength = await _sslStream.ReadAsync(_buffer);

                if (_bufferLength == 0 && writeOffset == count)
                {
                    EndOfStream = true;
                    Encoding.ASCII.GetString(bytes);
                }
                if (_bufferLength == 0)
                {
                    throw new InvalidDataException("Incomplete response");
                }
            }

            while (_bufferOffset < _bufferLength)
            {
                bytes[writeOffset++] = _buffer[_bufferOffset++];

                if (writeOffset == count)
                {
                    return Encoding.ASCII.GetString(bytes);
                }
            }
        }
    }

    public async Task<string?> ReadChunkAsync()
    {
        if (EndOfStream)
        {
            return null;
        }

        var chunkBytes = await this.ReadLineAsync();

        if (chunkBytes == null || chunkBytes == "0")
        {
            EndOfStream = true;
            return null;
        }
        
        var byteCount = long.Parse(chunkBytes, System.Globalization.NumberStyles.HexNumber);
        var chunk = await this.ReadBytesAsync(byteCount);
        _ = await this.ReadBytesAsync(2);
        return chunk;
    }
    
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _sslStream.Dispose();
    }
}