using System.Net;
using System.Net.Sockets;
using System.Threading;
using BepInEx.Logging;
using Instances;

namespace BepInEx.GUI.Loader;

internal class SendLogToClientSocket : ILogListener
{
    private readonly IProcessInstance _process;
    private readonly Thread _thread;

    private readonly Queue<LogEventArgs> _logQueue = new();
    private readonly object _queueLock = new();
    private readonly int _freePort;

    private const int SLEEP_MILLISECONDS = 17;
    private const string GUI_Socket_IP = "127.0.0.1";
    private const string PrefixLogs = "[SendLogToClient]";

    private bool _hasFirstLog = false;
    private bool _isDisposed = false;

    internal SendLogToClientSocket(IProcessInstance process, int freePort)
    {
        _process = process;
        _freePort = freePort;
        _thread = new Thread(() =>
        {
            IPAddress ipAddress = IPAddress.Parse(GUI_Socket_IP);
            TcpListener listener = new TcpListener(ipAddress, _freePort);
            listener.Start();
#if !RELEASE
            Log.Info($"{PrefixLogs} Accepting Socket.");
#endif
            for (int i = 0; i < 5; i++)
            {
#if !RELEASE
                if (i == 4)
                {
                    Log.Warning($"{PrefixLogs} :: [i:{i} :: Last connection attempt] Accepting Socket.");
                }
                else if (i > 0)
                {
                    Log.Warning($"{PrefixLogs} :: [i:{i}] Accepting Socket.");
                }
#endif
                Socket clientSocket = listener.AcceptSocket();
                if (_isDisposed)
                {
                    break;
                }

                SendPacketsToClientUntilConnectionIsClosed(clientSocket);
                Thread.Sleep(SLEEP_MILLISECONDS);

            }
#if !RELEASE
            Log.Error($"{PrefixLogs} :: [Listener has encountered too many lost connections to GUI aborting connection]");
#endif
        });

        _thread.Start();
    }

    /// <summary>
    /// Send all the logs to the gui as long as there are some other wise wait for logs to arrive.
    /// </summary>
    /// <param name="socket">The socket that connects to the client</param>
    private void SendPacketsToClientUntilConnectionIsClosed(Socket socket)
    {
        for (bool i = true; i; Thread.Sleep(SLEEP_MILLISECONDS))
        {
            if (_isDisposed)
            {
                break;
            }

            while (_logQueue.Count > 0)
            {
                LogEventArgs log;
                lock (_queueLock)
                {
                    log = _logQueue.Peek();
                }

                LogPacket logPacket = new LogPacket(log);
                try
                {
                    socket.Send(logPacket, _hasFirstLog);
                }
                catch (Exception e)
                {
                    Log.Error($"Error while trying to send log to socket: {e}{Environment.NewLine}Disconnecting socket.");
                    throw e;
                }

                lock (_queueLock)
                {
                    _ = _logQueue.Dequeue();
                }
            }
        }

    }

    private void KillBepInExGUIProcess()
    {
        Log.Message("Closing BepInEx.GUI");
        try
        {
            _process.Kill();
            Logger.Listeners.Remove(EntryPoint.GUI_Sender);
        }
        catch (Exception e)
        {
            Log.Error($"Error while trying to kill BepInEx GUI Process: {e}");
        }
        finally
        {
            Dispose();
        }
    }

    public void LogEvent(object sender, LogEventArgs eventArgs)
    {
        if (_isDisposed)
        {
            return;
        }

        if (eventArgs.Data == null)
        {
            Log.Warning("EventArgs is potentialy null");
            return;
        }

        if (!_hasFirstLog)
        {
            if (eventArgs.Level == LogLevel.Message &&
                eventArgs.Source.SourceName == "BepInEx" &&
                eventArgs.Data.ToString().StartsWith("BepInEx"))
            {
                _hasFirstLog = true;
            }
        }

        if (Config.CloseWindowWhenGameLoadedConfig.Value)
        {
            if ($"{eventArgs.Data}" != "Chainloader startup complete" || !eventArgs.Level.Equals(LogLevel.Message))
            {
                KillBepInExGUIProcess();
                return;
            }
        }

        lock (_queueLock)
        {
            _logQueue.Enqueue(eventArgs);
        }
    }

    public void Dispose()
    {
        _isDisposed = true;
    }
}