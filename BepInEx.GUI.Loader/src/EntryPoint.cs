global using System;
global using System.Collections.Generic;
global using System.Text;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using BepInEx.Configuration;
using BepInEx.Logging;
using Mono.Cecil;

namespace BepInEx.GUI.Loader;

internal static class EntryPoint
{
    public static IEnumerable<string> TargetDLLs { get; } = Array.Empty<string>();

    public static void Patch(AssemblyDefinition _) { }

    public static void Initialize()
    {
        Log.Init();
        try
        {
            InitializeInternal();
        }
        catch (Exception e)
        {
            Log.Error($"Failed to initialize : ({e.GetType()}) {e.Message}{Environment.NewLine}{e}");
        }
    }

    private static void InitializeInternal()
    {
        Config.Init(Paths.ConfigPath);

        var consoleConfig = (ConfigEntry<bool>)typeof(BepInPlugin).Assembly.
            GetType("BepInEx.ConsoleManager", true).
            GetField("ConfigConsoleEnabled",
            BindingFlags.Static | BindingFlags.Public).GetValue(null);
        if (!Config.EnableBepInExGUIConfig.Value)
        {
            Log.Info("Custom BepInEx.GUI is disabled in the config, aborting launch.");
        }
        else if (consoleConfig.Value)
        {
            consoleConfig.Value = false;
            Log.Warning("Disabled old console restart game for changes to take effect");
        }
        else
        {
            FindAndLaunchGUI();
        }
    }

    private static string FindGUIExecutable()
    {
        const string GuiFileName = "bepinex_gui";
        string modName = Config.ThunderstoreModNameConfig.Value;
        string autherName = Config.AutherNameConfig.Value;
        string GuiPath = $"{Paths.PatcherPluginPath}\\{autherName}-{modName}\\";
        string str = $"{Paths.PatcherPluginPath}\\{autherName}-{modName}\\{GuiFileName}.exe";
        var fileName = Path.GetFileName(GuiPath);
        if (fileName == $"{GuiFileName}.exe")
        {
            var versInfo = FileVersionInfo.GetVersionInfo(str);
            if (versInfo.FileMajorPart == 3)
            {
                Log.Info($"Found bepinex_gui v3 executable in {str}");
                return str;
            }
        }
        //if not find returns null
        return SearchForGuiExecuteable();
    }
    public static string SearchForGuiExecuteable()
    {
        const string GuiFileName = "bepinex_gui";
        foreach (var filePath in Directory.GetFiles(Paths.PatcherPluginPath, "*", SearchOption.AllDirectories))
        {
            // No platform check because proton is used for RoR2 and it handles it perfectly anyway:
            // It makes the Process.Start still goes through proton and makes the bep gui
            // that was compiled for Windows works fine even in linux operating systems.
            var fileName = Path.GetFileName(filePath);

            if (fileName == $"{GuiFileName}.exe")
            {
                var versInfo = FileVersionInfo.GetVersionInfo(filePath);
                //versInfo.GetType().GetField()
                if (versInfo.FileMajorPart == 3)
                {
                    Log.Info($"Found bepinex_gui v3 executable in {filePath}");
                    return filePath;
                }
            }
        }
        return null;
    }

    private static void FindAndLaunchGUI()
    {
        Log.Info("Finding and launching GUI");

        var executablePath = FindGUIExecutable();
        if (executablePath != null)
        {
            var freePort = FindFreePort();
            var process = LaunchGUI(executablePath, freePort);
            if (process != null)
            {
                Logger.Listeners.Add(new SendLogToClientSocket(freePort));
                Logger.Listeners.Add(new CloseProcessOnChainloaderDone(process));
            }
            else
            {
                Log.Info("LaunchGUI failed");
            }
        }
        else
        {
            Log.Info("bepinex_gui executable not found.");
        }
    }

    private static int FindFreePort()
    {
        int port = 0;
        Socket socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        try
        {
            IPEndPoint localEP = new(IPAddress.Any, 0);
            socket.Bind(localEP);
            localEP = (IPEndPoint)socket.LocalEndPoint;
            port = localEP.Port;
        }
        finally
        {
            socket.Close();
        }

        return port;
    }

    private static Process LaunchGUI(string executablePath, int socketPort)
    {
        string[] args =
        [
            typeof(Paths).Assembly.GetName().Version.ToString(),//arg[1] Version
            Paths.ProcessName,                                  //arg[2] Target name
            Paths.GameRootPath,                                 //arg[3] Game folder -P -F
            $"{Paths.BepInExRootPath}\\LogOutput.log",          //arg[4] BepInEx output -P -F
            Config.ConfigFilePath,                              //arg[5] ConfigPath
            Process.GetCurrentProcess().Id.ToString(),          //arg[6] Process Id
            socketPort.ToString(),                              //arg[7] socket port reciver
           // Process.GetCurrentProcess().Handle.ToString(),
        ];
        var processStartInfo = new ProcessStartInfo(fileName: executablePath, FormatArguments(args));
        processStartInfo.WorkingDirectory = Path.GetDirectoryName(executablePath);

        return Process.GetCurrentProcess();
    }
    private static string FormatArguments(this string[] args, int i = 0)
    {
        if (i == args.Length - 1)
        {
            return $"\"{args[i]}\"";
        }
        return $"\"{args[i]}\" {FormatArguments(args, ++i)}";
    }
}
