using System.IO;
using BepInEx.Configuration;

namespace BepInEx.GUI.Loader;

internal static class Config
{
    internal const string FileName = "BepInEx.GUI.cfg";
    internal static string ConfigFilePath { get; private set; }

    private static ConfigFile File { get; set; }

    internal const string EnableBepInExGUIConfigKey = "Enable BepInEx GUI";
    internal const string EnableBepInExGUIConfigDescription = "Enable the custom BepInEx GUI";
    internal static ConfigEntry<bool> EnableBepInExGUIConfig { get; private set; }

    internal const string CloseWindowWhenGameLoadedConfigKey = "Close Window When Game Loaded";
    internal const string CloseWindowWhenGameLoadedConfigDescription = "Close the graphic user interface window when the game is loaded";
    internal static ConfigEntry<bool> CloseWindowWhenGameLoadedConfig { get; private set; }

    internal const string CloseWindowWhenGameClosesConfigKey = "Close Window When Game Closes";
    internal const string CloseWindowWhenGameClosesConfigDescription = "Close the graphic user interface window when the game closes";
    internal static ConfigEntry<bool> CloseWindowWhenGameClosesConfig { get; private set; }

    private const string Disclaimer = "\nDO NOT TOUCH OR CHANGE UNLESS YOU KNOW WHAT YOU ARE DOING";
    internal static ConfigEntry<string> AutherNameConfig { get; private set; }

    internal const string AutherNameConfigKey = "Auther Name";
    internal const string AutherNameConfigDescription = $"To Change The Auther Name For Finding the Executable{Disclaimer}";
    internal static ConfigEntry<string> ThunderstoreModNameConfig { get; private set; }

    internal const string ThunderstoreModNameConfigKey = "Thunderstore Mod Name";
    internal const string ThunderstoreModNameConfigDescription = $"To Change The Thunderstore Mod Name For Finding the Executable{Disclaimer}";
    internal static void Init(string folderFullPath)
    {
        ConfigFilePath = Path.Combine(folderFullPath, FileName);
        File = new ConfigFile(ConfigFilePath, true);

        EnableBepInExGUIConfig = File.Bind("Settings", EnableBepInExGUIConfigKey, true, EnableBepInExGUIConfigDescription);

        CloseWindowWhenGameLoadedConfig = File.Bind("Settings",
            CloseWindowWhenGameLoadedConfigKey, false, CloseWindowWhenGameLoadedConfigDescription);

        CloseWindowWhenGameClosesConfig = File.Bind("Settings",
            CloseWindowWhenGameClosesConfigKey, true, CloseWindowWhenGameClosesConfigDescription);

        AutherNameConfig = File.Bind("Auther name", AutherNameConfigKey, "CatsArmy", AutherNameConfigDescription);

        ThunderstoreModNameConfig = File.Bind("Thunderstore mod name", ThunderstoreModNameConfigKey, "BepInEx_GUI",
            ThunderstoreModNameConfigDescription);
    }
}
