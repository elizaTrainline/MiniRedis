using System.Net;
using System.Net.Sockets;
using System.Text;
using MiniRedis.Core;

namespace MiniRedis.Server;

public sealed class MiniRedisServer
{
    private readonly CommandProcessor _processor;
    private readonly int _port;
    private TcpListener? _listener;
    private readonly CancellationTokenSource _cts = new();

    public MiniRedisServer(CommandProcessor processor, int port)
    { 
        _processor = processor; 
        _port = port; 
    }

    public async Task StartAsync()
    {
        _listener = new TcpListener(IPAddress.Loopback, _port);
        _listener.Start();

        while (!_cts.IsCancellationRequested)
        {
            TcpClient client;
            try 
            { 
                client = await _listener.AcceptTcpClientAsync(_cts.Token); 
            }
            catch (OperationCanceledException) 
            { 
                break; 
            }
            _ = HandleClientAsync(client, _cts.Token);
        }
    }

    public void Stop()
    {
        _cts.Cancel();
        _listener?.Stop();
        Console.WriteLine("[MiniRedis] Server stopped.");
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
    {
        using var _ = client;
        using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
        {
            AutoFlush = true,
            NewLine = "\r\n"
        };

        await writer.WriteLineAsync("# MiniRedis ready. Commands: PING | SET k v [EX s] | GET k | DEL k | EXPIRE k s | TTL k | INCR k | KEYS | FLUSHALL | SAVE");

        while (!ct.IsCancellationRequested && client.Connected)
        {
            var line = await reader.ReadLineAsync();
            if (line is null) break;
            if (string.IsNullOrWhiteSpace(line)) continue;

            var resp = _processor.Process(line);
            await writer.WriteLineAsync(resp);
        }
    }
}