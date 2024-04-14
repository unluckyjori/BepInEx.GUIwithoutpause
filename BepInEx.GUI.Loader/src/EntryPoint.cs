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
using HarmonyLib;
using Mono.Cecil;


namespace BepInEx.GUI.Loader;

internal static class EntryPoint
{
    public const string GUID = $"{nameof(BepInEx)}.{nameof(GUI)}.{nameof(Loader)}";

    public static SendLogToClientSocket GUI_Sender;
    public static IEnumerable<string> TargetDLLs { get; } = [];

    private const SearchOption searchOption = SearchOption.AllDirectories;
    private const string GuiFileFullName = "bepinex_gui.exe";
    private const bool OnlySearchGUI_IP_Port = true;
    private const string searchPattern = "*";

    public static Harmony _harmony;
    public static void Patch(AssemblyDefinition _) { }

    // Called before patching occurs
    public static void Initialize()
    {
        Log.Init();
        try
        {
            InitializeInternal();
        }
        catch (Exception e)
        {
            Log.Error($"Failed to initialize : ({e.GetType()}) {e.Message} {Environment.NewLine}{e}");
        }
    }

    // Called after preloader has patched all assemblies and loaded them in
    // At this point it is fine to reference patched assemblies
    public static void Finish()
    {
        _harmony = new Harmony(EntryPoint.GUID);
        _harmony.PatchAll(typeof(EventArgsPatchTest));
    }

    private static void InitializeInternal()
    {
        Config.Init(Paths.ConfigPath);
        ConfigEntry<bool> consoleConfig = (ConfigEntry<bool>)typeof(BepInPlugin).Assembly.
            GetType("BepInEx.ConsoleManager", true).
            GetField("ConfigConsoleEnabled",
            BindingFlags.Static | BindingFlags.Public).GetValue(null);

        if (!Config.EnableBepInExGUIConfig.Value)
        {
            Log.Info("Custom BepInEx.GUI is disabled in the config, aborting launch.");
            return;
        }

        if (consoleConfig.Value && Config.AllowChangingConfigValues.Value)
        {
            Log.Warning("Disabled old console restart game for changes to take effect");
            consoleConfig.Value = false;
            return;
        }

        FindAndLaunchGUI();
    }

    private static void FindAndLaunchGUI()
    {
        Log.Info("Finding and launching GUI");
        string executablePath = SearchForGUIExecutable();
        if (executablePath == null)
        {
            executablePath = FallbackSearchForGUIExecuteable();
            if (executablePath == null)
            {
                Log.Info("bepinex_gui executable not found.");
                return;
            }
        }
        int freePort = FindFreePort();
        Process process = LaunchGUI(executablePath, freePort);
        if (process == null)
        {
            Log.Info("LaunchGUI failed");
            return;
        }

        GUI_Sender = new SendLogToClientSocket(process, freePort);
        Logger.Listeners.Add(GUI_Sender);
    }

    //New method works 100% was tested
    private static string SearchForGUIExecutable()
    {
        Assembly assembly = typeof(EntryPoint).Assembly;
        int assemblyFolderIndex = assembly.Location.LastIndexOf('\\');
        string exeLocation = $"{assembly.Location[..assemblyFolderIndex]}\\{GuiFileFullName}";
        if (IsGUI(exeLocation))
        {
            return exeLocation;
        }

        return null;
    }

    private static bool IsGUI(string path)
    {
        if (Path.GetFileName(path) == GuiFileFullName)
        {
            FileVersionInfo versInfo = FileVersionInfo.GetVersionInfo(path);
            if (versInfo.FileMajorPart != 3)
            {
                Log.Warning($"Found bepinex_gui v{versInfo.FileMajorPart} executable in {path} while expecting v3");
                return true;
            }

            Log.Info($"Found bepinex_gui v3 executable in {path}");
            return true;
        }

        return false;
    }

    /// <summary>
    /// No platform check because proton is used for RoR2 and it handles it perfectly anyway:
    /// It makes the Process.Start still goes through proton and makes the bep gui
    /// that was compiled for Windows works fine even in linux operating systems.
    /// </summary>
    public static string FallbackSearchForGUIExecuteable()
    {
        Log.Info("Failed to quick find GUI.");
        Log.Info("Initiating Fallback Searching Method for executable in [BepInEx\\patchers, BepInEx\\plugins]");
        foreach (string file in Directory.GetFiles(Paths.PatcherPluginPath, searchPattern, searchOption))
        {
            if (IsGUI(file))
                return file;
        }
        foreach (string file in Directory.GetFiles(Paths.PluginPath, searchPattern, searchOption))
        {
            if (IsGUI(file))
                return file;
        }

        return null;
    }

    private static int FindFreePort()
    {
        int port = 0;
        Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        try
        {
            IPEndPoint localEP;

            if (OnlySearchGUI_IP_Port)
                localEP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);
            else
                localEP = new IPEndPoint(IPAddress.Any, port);

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

    private static Process LaunchGUI(string executablePath, int port)
    {
        string[] argList =
        [
            $"\"{typeof(Paths).Assembly.GetName().Version}\" ", //arg[1] Version
            $"\"{Paths.ProcessName}\" ",                              //arg[2] Target name
            $"\"{Paths.GameRootPath}\" ",                             //arg[3] Game folder -P -F
            $"\"{Paths.BepInExRootPath}\\LogOutput.log\" ",           //arg[4] BepInEx output -P -F
            $"\"{Config.ConfigFilePath}\" ",                          //arg[5] ConfigPath
            $"\"{Process.GetCurrentProcess().Id}\" ",                 //arg[6] Process Id
            $"\"{port}\""                                             //arg[7] socket port reciver
        ];

        string args = string.Empty;
        foreach (string arg in argList)
            args += arg;

        ProcessStartInfo processStartInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            WorkingDirectory = Path.GetDirectoryName(executablePath),
            Arguments = args
        };

        return Process.Start(processStartInfo);
    }
}