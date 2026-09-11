using System.Reflection;

namespace WhisperSharp.Helper
{
    public class PathHelper
    {
        public static string Location => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static string AssetsFolder => Path.Combine(Location, "Assets");
        public static string DataFolder => Path.Combine(Location, "Data");
        public static string ConfigFolder => Path.Combine(Location, "Config");

        static PathHelper()
        {
            if(!Directory.Exists(AssetsFolder))
            {
                Directory.CreateDirectory(AssetsFolder);
            }

            if (!Directory.Exists(DataFolder))
            {
                Directory.CreateDirectory(DataFolder);
            }
        }

        public static string GetAssetsFilePath(string fileName)
        {
            return Path.Combine(AssetsFolder, fileName);
        }

        public static string GetDataFilePath(string fileName)
        {
            return Path.Combine(DataFolder, fileName);
        }

        public static string GetConfigFilePath(string fileName)
        {
            return Path.Combine(ConfigFolder, fileName);
        }
    }
}
