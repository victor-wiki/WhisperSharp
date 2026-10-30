using Newtonsoft.Json;
using System.IO;
using WhisperSharp.Helper;
using WhisperSharp.WinApp.Model;

namespace DatabaseManager.Core
{
    public class SettingManager 
    {
        public static string ConfigRootFolder => FilePathHelper.ConfigFolder;

        public static Setting Setting { get; private set; } = new Setting();

        static SettingManager()
        {
            LoadConfig();
        }       

        public static string ConfigFilePath
        {
            get
            {
                return Path.Combine(ConfigRootFolder, "Setting.json");
            }
        }

        public static void LoadConfig()
        {
            if (File.Exists(ConfigFilePath))
            {
                Setting = (Setting)JsonConvert.DeserializeObject(File.ReadAllText(ConfigFilePath), typeof(Setting));
            }
        }

        public static void SaveConfig(Setting setting)
        {
            Setting = setting;
            string content = JsonConvert.SerializeObject(setting, Formatting.Indented);

            File.WriteAllText(ConfigFilePath, content);
        }        
    }
}
