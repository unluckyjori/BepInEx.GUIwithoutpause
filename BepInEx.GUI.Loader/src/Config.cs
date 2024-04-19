using System.IO;
using BepInEx.Configuration;

namespace BepInEx.GUI.Loader;

internal static class Config
{
    internal static ConfigFile File { get; set; }
    internal static string ConfigFilePath { get; private set; }
    internal const string ConfigFileName = "BepInEx.GUI.cfg";
    internal static void Init(string path)
    {
        ConfigFilePath = Path.Combine(path, ConfigFileName);
        File = new ConfigFile(ConfigFilePath, true);

        EnableBepInExGUIConfig = File.Bind(Settings, EnableBepInExGUIConfigKey,
            true, EnableBepInExGUIConfigDescription);

        CloseWindowWhenGameLoadedConfig = File.Bind(Settings, CloseWindowWhenGameLoadedConfigKey,
            false, CloseWindowWhenGameLoadedConfigDescription);

        CloseWindowWhenGameClosesConfig = File.Bind(Settings, CloseWindowWhenGameClosesConfigKey,
            true, CloseWindowWhenGameClosesConfigDescription);

        AllowChangingConfigValues = File.Bind(Premissions, AllowChangingConfigValuesKey,
            false, AllowChangingConfigValuesDescription);
    }

    private const string Settings = nameof(Settings);
    #region Settings
    public static ConfigEntry<bool> EnableBepInExGUIConfig { get; private set; }

    private const string EnableBepInExGUIConfigKey = "Enable BepInEx GUI";
    private const string EnableBepInExGUIConfigDescription = "Enable the custom BepInEx GUI";
    public static ConfigEntry<bool> CloseWindowWhenGameLoadedConfig { get; private set; }

    private const string CloseWindowWhenGameLoadedConfigKey = "Close Window When Game Loaded";
    private const string CloseWindowWhenGameLoadedConfigDescription = "Close the graphic user interface window when the game is loaded";
    public static ConfigEntry<bool> CloseWindowWhenGameClosesConfig { get; private set; }

    private const string CloseWindowWhenGameClosesConfigKey = "Close Window When Game Closes";
    private const string CloseWindowWhenGameClosesConfigDescription = "Close the graphic user interface window when the game closes";
    #endregion

    private const string Premissions = nameof(Premissions);
    #region Permission
    public static ConfigEntry<bool> AllowChangingConfigValues { get; private set; }

    private const string AllowChangingConfigValuesKey = "Auto Disable BepInEx Console when using GUI mod";
    private const string AllowChangingConfigValuesDescription = "Auto Disable BepInEx Console when using GUI mod";
    #endregion
}