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
    public static IEnumerable<string> TargetDLLs { get; } = [];
    public static SendLogToClientSocket GUI_Sender;
#if !RELEASE
    public static Harmony _harmony;
#endif

    public const string GUID = $"{nameof(BepInEx)}.{nameof(GUI)}.{nameof(Loader)}";
    private const SearchOption searchOption = SearchOption.AllDirectories;
    private const string GuiFileFullName = "bepinex_gui.exe";
    private const bool OnlySearchGUI_IP_Port = true;
    private const string searchPattern = "*";


    /// <summary>
    /// Patch whichever assemblys you need to
    /// </summary>
    ///  <param name="_">The assemblys</param>
    public static void Patch(AssemblyDefinition _)
    {
        ///Do stuff if you need to
    }

    /// <summary>
    /// Called before patching occurs
    /// </summary>
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

#if !RELEASE
    /// <summary>
    /// Called after preloader has patched all assemblies and loaded them in
    /// Called after preloader has patched all assemblies and loaded them in
    /// </summary> 
    public static void Finish()
    {
        _harmony = new Harmony(EntryPoint.GUID);
        _harmony.PatchAll(typeof(EventArgsPatchTest));
}
#endif

    private static void InitializeInternal()
    {
        Config.Init(Paths.ConfigPath);

        if (!Config.EnableBepInExGUIConfig.Value)
        {
            Log.Info("Custom BepInEx.GUI is disabled in the config, aborting launch.");
            return;
        }

        Type BepInExConsoleManager = typeof(BepInPlugin).Assembly.GetType("BepInEx.ConsoleManager", true);

        ConfigEntry<bool> consoleConfig =
            (ConfigEntry<bool>)BepInExConsoleManager.GetField("ConfigConsoleEnabled", BindingFlags.Static | BindingFlags.Public)
            .GetValue(null);

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
        ProcessStartInfo processStartInfo = LaunchGUI(executablePath, freePort);
#if !RELEASE
        return;
#endif
        Process process = Process.Start(processStartInfo);

        ///Potentials overhead
        /// * parsing rust/c# strings for communication
        ///var i = process.StandardInput;
        ///var j = process.StandardOutput;
        ///###

        if (process == null)
        {
            Log.Info("LaunchGUI failed");
            return;
        }

        GUI_Sender = new SendLogToClientSocket(process, freePort);
        Logger.Listeners.Add(GUI_Sender);
    }

    /// <summary>
    /// Faster more relayable method using good assumptions
    /// </summary>
    /// <returns>The path to the GUI executable if found otherwise null</returns>
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
    /// search all folders and files in the "patchers" folder and the "plugins" folder
    /// </summary>
    /// <remarks>
    /// No platform check because proton is used for RoR2 and it handles it perfectly anyway:
    /// It makes the Process.Start still goes through proton and makes the bep gui
    /// that was compiled for Windows works fine even in linux operating systems.
    /// </remarks>
    /// <returns>The path to the GUI executable if found otherwise null</returns>
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
            IPEndPoint localEP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);
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

    private static ProcessStartInfo LaunchGUI(string executablePath, int port)
    {
        string arg8 = SaveIcon();
        string[] argList =
        [
            $"\"{typeof(Paths).Assembly.GetName().Version}\" ", //arg[1] Version
            $"\"{Paths.ProcessName}\" ",                        //arg[2] Target name
            $"\"{Paths.GameRootPath}\" ",                       //arg[3] Game folder -P -F
            $"\"{Paths.BepInExRootPath}\\LogOutput.log\" ",     //arg[4] BepInEx output -P -F
            $"\"{Config.ConfigFilePath}\" ",                    //arg[5] ConfigPath
            $"\"{Process.GetCurrentProcess().Id}\" ",           //arg[6] Process Id
            $"\"{port}\" ",                                     //arg[7] socket port reciver
            $"\"{arg8}\""
        ];

#if !RELEASE
        foreach (string arg in argList)
                Log.Fatal(arg);
#endif
        string args = string.Empty;
        foreach (string arg in argList)
            args += arg;

        ProcessStartInfo processStartInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            WorkingDirectory = Path.GetDirectoryName(executablePath),
            Arguments = args
        };

        return processStartInfo;
    }
    /// <param name="path"></param>
    /// <param name="dir">Game Directory</param>
    /// <param name="name"></param>
    /// <returns>the path to the icon file</returns>
    /// ProcessName = Path.GetFileNameWithoutExtension(executablePath);
    /// eg ..\..\..\Lethal Company.exe = Lethal Company
    public static string SaveIcon()
    {
        string icon_dir = $"{Paths.GameRootPath}{(Paths.GameRootPath.EndsWith('\\') ? string.Empty : "\\")}icon.";

        string icon_file = IconIsSaved(icon_dir);
        if (icon_file != null)
        {
            Log.Info("Icon was already saved");
            return icon_file;
        }

        return "None";
    }

    private static string IconIsSaved(string icon_file_path)
    {
        string[] SupportedFileExtensions = ["png", "ico", "jpeg", "jpg", "gif", "webp", "tiff"];
        foreach (string ext in SupportedFileExtensions)
        {
            string icon = $"{icon_file_path}{ext}";
            if (File.Exists(icon))
            {
                Log.Warning($"Icon already exists at path: {icon}");
                return icon;
            }
        }

        return null;
    }

#if !RELEASE
    /// <summary>
    /// the icon representation of an image that is contained in the specified file.
    /// </summary>
    /// <param name="path">the path to the icon</param>
    /// <returns>The icon of the path if it exists</returns>
    private static System.Drawing.Icon ExtractIconFromFilePath(string path)
    {
        System.Drawing.Icon result = null;

        try
        {
            result = System.Drawing.Icon.ExtractAssociatedIcon(path);
        }
        catch (Exception e)
        {
            Log.Warning($"Unable to extract the icon from the binary{Environment.NewLine}{e}");
        }

        return result;
    }
#endif

    //DEAD CODE for untill i figure out a fix to the System.Drawing.Common Issue
#if false
        System.Drawing.Icon icon = ExtractIconFromFilePath(Paths.ExecutablePath);

        if (icon == null)
        {
            Log.Warning("Failed to exctract the Icon from the game executable");
            return "None";
        }

        try
        {
            using (FileStream stream = new FileStream(icon_dir, FileMode.CreateNew))
            {
                icon.ToBitmap().Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                stream.Close();
            }
        }

        catch (Exception e)
        {
            Log.Debug($"Failed to save icon or somehow the saved icon was missed{Environment.NewLine}{e}");
            return "None";
        }

        return icon_dir;
#endif
}