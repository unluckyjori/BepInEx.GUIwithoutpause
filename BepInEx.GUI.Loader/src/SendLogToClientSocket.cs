using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using BepInEx.Logging;

namespace BepInEx.GUI.Loader;

internal class SendLogToClientSocket : ILogListener
{
    private readonly Process _process;
    private readonly Thread _thread;
    private readonly Queue<LogEventArgs> _logQueue = new();

    private readonly object _queueLock = new();
    private readonly int _freePort;

    private const int SLEEP_MILLISECONDS = 17;
    private const string GUI_Socket_IP = "127.0.0.1";
    private bool _isDisposed = false;

    internal SendLogToClientSocket(Process process, int freePort)
    {
        _process = process;
        _freePort = freePort;
        _thread = new Thread(() =>
        {
            IPAddress ipAddress = IPAddress.Parse(GUI_Socket_IP);
            TcpListener listener = new TcpListener(ipAddress, _freePort);
            listener.Start();

            while (true)
            {
                Log.Info($"[SendLogToClient] Accepting Socket.");
                Socket clientSocket = listener.AcceptSocket();
                if (_isDisposed)
                {
                    break;
                }

                SendPacketsToClientUntilConnectionIsClosed(clientSocket);
            }
        });

        _thread.Start();
    }


    private void SendPacketsToClientUntilConnectionIsClosed(Socket clientSocket)
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
                    clientSocket.Send(logPacket);
                }
                catch (Exception e)
                {
                    Log.Error($"Error while trying to send log to socket: {e} {Environment.NewLine}Disconnecting socket.");
                    return;
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

        if ($"{eventArgs.Data}" != "Chainloader startup complete" || !eventArgs.Level.Equals(LogLevel.Message))
        {
            if (Config.CloseWindowWhenGameLoadedConfig.Value)
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