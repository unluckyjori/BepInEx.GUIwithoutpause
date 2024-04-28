global using System;
global using System.Collections.Generic;
global using System.Text;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;
using BepInEx.Configuration;
using BepInEx.Logging;
using Instances;
using Mono.Cecil;

namespace BepInEx.GUI.Loader;

internal static class EntryPoint
{
    public static IEnumerable<string> TargetDLLs { get; } = [];
    public static SendLogToClientSocket GUI_Sender;
#if false
	public static Harmony _harmony;
#endif

    public const string GUID = $"{nameof(BepInEx)}.{nameof(GUI)}.{nameof(Loader)}";
    private const SearchOption searchOption = SearchOption.AllDirectories;
    private const string GuiFileFullName = "bepinex_gui.exe";
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

#if false
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

        ConfigEntry<bool> consoleConfig = BepInExConsoleManager.GetField("ConfigConsoleEnabled",
            BindingFlags.Static | BindingFlags.Public).GetValue(null) as ConfigEntry<bool>;

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
        ProcessArguments processStartInfo = new ProcessArguments(LaunchGUI(executablePath, freePort));

        ProcessInstance process = processStartInfo.Start();
        //process.OutputDataReceived += OutputDataReceived;
        //process.ErrorDataReceived += ErrorDataReceived;

        //C# = Target
        //Rust = Host
        if (process == null)
        {
            Log.Info("LaunchGUI failed");
            return;
        }
        GUI_Sender = new SendLogToClientSocket(process, freePort);
        Logger.Listeners.Add(GUI_Sender);
        //System.Threading.Thread _thread = new(async () =>
        //{
        //    Thread.Sleep(1500);
        //    const string str = "Hello World from C# standart ";
        //    await process.SendInputDataAsync($"{str}input");
        //    await process.SendErrorDataAsync($"{str}error");
        //    string a = await process.GetProcess().StandardOutput.ReadToEndAsync();
        //    Log.Error($"FOUND ERROR IN STANDART ERROR: {a} YIPPEEEEEEE");
        //});
        //process.GetProcess().CancelErrorRead();
        //process.GetProcess().CancelOutputRead();
        //System.Threading.Thread __thread = new(() =>
        //{
        //    Thread.Sleep(1500);
        //    const string str = "Hello World from C# standart ";
        //    process.SendInputData($"{str}input");
        //    process.SendErrorData($"{str}error");
        //    string a = process.GetProcess().StandardOutput.ReadToEnd();
        //    Log.Error($"FOUND ERROR IN STANDART ERROR: {a} YIPPEEEEEEE");
        //});

    }

    private static bool receivedStdErr = false;
    private static bool receivedStdOut = false;
    private static void OutputDataReceived(object sender, string data)
    {
        if (receivedStdOut)
        {

        }
        Log.Warning($"{sender} || {data}");
        receivedStdOut = true;
    }
    private static void ErrorDataReceived(object sender, string data)
    {
        if (receivedStdErr)
        {

        }
        Log.Error($"{sender} || {data}");
        receivedStdErr = true;
    }
    //Process ---->
    //Stream Readers
    //std::inputData
    //std::inputError
    //Stream Writers
    //std::outputData
    //std::outputError

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
        DirectoryInfo icon_dir = new DirectoryInfo(Path.Combine(Paths.GameRootPath, "BepInEx.GUI.Icon"));
        if (!icon_dir.Exists)
        {
            icon_dir.Create();
        }

        string icon = SavedIcon(icon_dir);
        SaveIcon(icon_dir).Wait();
        if (icon == null)
            icon = SaveIcon(icon_dir).Result;
        if (icon == null)
            icon = SavedIcon(icon_dir);
        if (icon == null)
            icon = "None";

        string[] argList =
        [
            $"\"{typeof(Paths).Assembly.GetName().Version}\" ", //arg[1] Version
			$"\"{Paths.ProcessName}\" ",                        //arg[2] Target name
			$"\"{Paths.GameRootPath}\" ",                       //arg[3] Game folder -P -F
			$"\"{Paths.BepInExRootPath}\\LogOutput.log\" ",     //arg[4] BepInEx output -P -F
			$"\"{Config.ConfigFilePath}\" ",                    //arg[5] ConfigPath
			$"\"{Process.GetCurrentProcess().Id}\" ",           //arg[6] Process Id
			$"\"{port}\" ",                                     //arg[7] socket port reciver
			$"\"{icon}\""
        ];

        foreach (string arg in argList)
            Log.Fatal(arg);

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
    private static async Task<string> SaveIcon(DirectoryInfo icon_dir)
    {
        string powershell = icon_dir.FullName;
        if (!powershell.EndsWith('\\'))
        {
            powershell += '\\';
        }
        powershell += "CopyInfo.ps1";

        string icon_file = icon_dir.FullName;
        if (!icon_file.EndsWith('\\'))
        {
            icon_file += '\\';
        }
        icon_file += "icon.png";
        if (!await SaveShell(powershell, icon_file))
        {
            return null;
        }


        ProcessArguments shellargs = new ProcessArguments("powershell.exe",
            $"-NoProfile -ExecutionPolicy ByPass -File \"{powershell}\"");
        ProcessInstance shell = shellargs.Start();

        shell.ErrorDataReceived += delegate (object _, string data)
        {
            if (data == "Script is done")
            {
                shell.Kill();
            }
        };

        return icon_file;
    }

    /// <param name="path"></param>
    /// <param name="dir">Game Directory</param>
    /// <param name="name"></param>
    /// <returns>the path to the icon file</returns>
    /// ProcessName = Path.GetFileNameWithoutExtension(executablePath);
    /// eg ..\..\..\Lethal Company.exe = Lethal Company
    public static string SavedIcon(DirectoryInfo icon_dir)
    {
        string icon_file = icon_dir.FullName;
        if (!icon_file.EndsWith('\\'))
        {
            icon_file += '\\';
        }
        icon_file += "icon.";

        string icon_path = SavedIcon(icon_file);

        return icon_path;
    }

    private static string SavedIcon(string icon_file)
    {
        string[] SupportedFileExtensions = ["png", "ico", "jpeg", "jpg", "gif", "webp", "tiff"];
        foreach (string ext in SupportedFileExtensions)
        {
            string icon = $"{icon_file}{ext}";
            if (File.Exists(icon))
            {
                Log.Warning($"Icon already exists at path: {icon}");
                return icon;
            }
        }

        return null;
    }


    private static async Task<bool> SaveShell(string powershell, string icon_file)
    {
        if (File.Exists(powershell))
        {
            return true;
        }

        string Shell = $"{PowerShell.Script}Export-Icon -Path \"{Paths.ExecutablePath}\" -Destination \"{icon_file}\" ";

        FileStream fileStream = File.Create(powershell);
        try
        {
            UTF8Encoding UTF8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            byte[] bytes = UTF8.GetBytes(Shell);
            await fileStream.WriteAsync(bytes, 0, bytes.Length);
            await fileStream.FlushAsync();
        }
        catch (Exception)
        {
            return File.Exists(powershell);
        }

        return true;
    }
}