using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace AnimeQuoteWall.GUI.Services;

/// <summary>
/// Ensures only one instance of AnimeQuoteWall runs. A second launch signals the first instance
/// (over a local named pipe) to come to the foreground, then exits - the normal "clicking the app
/// again just focuses the existing window" behavior. Uses a named Mutex + named pipe; no extra
/// dependency.
/// </summary>
public sealed class SingleInstanceManager : IDisposable
{
    // Per-user names so the single-instance scope is the current user's session.
    private static readonly string MutexName = $"AnimeQuoteWall.SingleInstance.{Environment.UserName}";
    private static readonly string PipeName = $"AnimeQuoteWall.Pipe.{Environment.UserName}";
    private const string ShowCommand = "show";

    private Mutex? _mutex;
    private CancellationTokenSource? _cts;
    private bool _ownsMutex;
    private bool _disposed;

    /// <summary>Raised (on a background thread) when another instance asks this one to show itself.</summary>
    public event EventHandler? ShowRequested;

    /// <summary>
    /// Returns true if this is the first/primary instance. If false, the caller should call
    /// <see cref="SignalExistingInstance"/> and exit.
    /// </summary>
    public bool IsPrimaryInstance()
    {
        _mutex = new Mutex(initiallyOwned: true, MutexName, out bool createdNew);
        _ownsMutex = createdNew;
        return createdNew;
    }

    /// <summary>Tells the already-running primary instance to show its window.</summary>
    public static void SignalExistingInstance()
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.Connect(1000);
            using var writer = new StreamWriter(client) { AutoFlush = true };
            writer.WriteLine(ShowCommand);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SingleInstanceManager.SignalExistingInstance: {ex.Message}");
        }
    }

    /// <summary>Starts listening for signals from secondary instances (primary instance only).</summary>
    public void StartListening()
    {
        _cts = new CancellationTokenSource();
        _ = Task.Run(() => ListenLoopAsync(_cts.Token))
            .ContinueWith(t => { if (t.IsFaulted && t.Exception != null) System.Diagnostics.Debug.WriteLine($"SingleInstanceManager listen faulted: {t.Exception.GetBaseException().Message}"); },
                TaskScheduler.Default);
    }

    private async Task ListenLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                using var server = new NamedPipeServerStream(PipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                await server.WaitForConnectionAsync(token).ConfigureAwait(false);
                using var reader = new StreamReader(server);
                var line = await reader.ReadLineAsync().ConfigureAwait(false);
                if (string.Equals(line?.Trim(), ShowCommand, StringComparison.OrdinalIgnoreCase))
                    ShowRequested?.Invoke(this, EventArgs.Empty);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SingleInstanceManager.ListenLoop: {ex.Message}");
                try { await Task.Delay(500, token).ConfigureAwait(false); }
                catch (OperationCanceledException) { break; }
            }
        }
    }

    /// <summary>Stops the listener and releases the mutex.</summary>
    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        if (_mutex != null)
        {
            try
            {
                if (_ownsMutex)
                    _mutex.ReleaseMutex();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SingleInstanceManager.Dispose release: {ex.Message}");
            }
            _mutex.Dispose();
            _mutex = null;
        }
    }
}
