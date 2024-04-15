using System.Net.Sockets;
using BepInEx.Logging;

namespace BepInEx.GUI.Loader;

internal static class Log
{
    private static ManualLogSource _logSource { set; get; }

    internal static int Send(this Socket socket, LogPacket packet, bool isFirst)
    {
        int result = -1;
        try
        {
            if (!isFirst && !socket.Connected)
            {
                return -1;
            }
            result = socket.Send(packet.Bytes
                );//, 0, packet.Bytes.Length, SocketFlags.None, out SocketError errorCode);
            //Debug(errorCode);
        }
        catch (Exception e)
        {
            Error(socket);
            Error(packet); //packet is not the issue --ruled out by testing
            throw new Exception($"What the fuck?{Environment.NewLine}{e}");
        }

        return result;
    }
    internal static void Init() => _logSource = Logger.CreateLogSource(EntryPoint.GUID);
    internal static void Fatal(object data) => _logSource.LogFatal(data);
    internal static void Error(object data) => _logSource.LogError(data);
    internal static void Warning(object data) => _logSource.LogWarning(data);
    internal static void Message(object data) => _logSource.LogMessage(data);
    internal static void Debug(object data) => _logSource.LogDebug(data);
    internal static void Info(object data) => _logSource.LogInfo(data);

    private static void Error(this Socket socket)
    {
        if (socket is null)
        {
            Error($"[Socket is null]");
            return;
        }

        Error($"[Socket is not null], [Socket::is {(socket.Connected ? "connected" : "not connected")}]");
    }
}
