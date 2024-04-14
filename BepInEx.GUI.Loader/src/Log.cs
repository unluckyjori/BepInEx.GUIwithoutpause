using System.Net.Sockets;
using BepInEx.Logging;

namespace BepInEx.GUI.Loader;

internal static class Log
{
    private static ManualLogSource _logSource { set; get; }
    private const string sourceName = $"{nameof(BepInEx)}.{nameof(GUI)}.{nameof(Loader)}";

    internal static void Init() => _logSource = Logger.CreateLogSource(sourceName);
    internal static void Fatal(this object data) => _logSource.LogFatal(data);
    internal static void Error(this object data) => _logSource.LogError(data);
    internal static void Warning(this object data) => _logSource.LogWarning(data);
    internal static void Message(this object data) => _logSource.LogMessage(data);
    internal static void Debug(this object data) => _logSource.LogDebug(data);
    internal static void Info(this object data) => _logSource.LogInfo(data);
    internal static int Send(this Socket socket, LogPacket packet) => socket.Send(packet.Bytes);
}
