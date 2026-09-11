namespace WhisperSharp.Helper
{
    [Flags]
    public enum LogType : int
    {
        None = 0,
        Info = 2,
        Error = 4,
        Warnning = 8,
    }

    public class LogHelper
    {
        private static object obj = new object();

        public static string DefaultLogFolder { get; set; }
        public static LogType LogType { get; set; }
        public static bool EnableLog { get; set; }

        public static void LogInfo(string message, string logFilePath = null)
        {
            Log(LogType.Info, message, logFilePath);
        }

        public static void LogError(string message, string logFilePath = null)
        {
            Log(LogType.Error, message, logFilePath);
        }

        public static void LogWarnning(string message, string logFilePath = null)
        {
            Log(LogType.Warnning, message, logFilePath);
        }

        private static void Log(LogType logType, string message, string logFilePath = null)
        {
            if (!EnableLog)
            {
                return;
            }

            string filePath = logFilePath;

            if (string.IsNullOrEmpty(logFilePath))
            {
                string logFolder = DefaultLogFolder ?? "log";

                if (!Directory.Exists(logFolder))
                {
                    Directory.CreateDirectory(logFolder);
                }

                filePath = Path.Combine(logFolder, DateTime.Today.ToString("yyyyMMdd") + ".txt");
            }

            bool isNewLine = message == Environment.NewLine;

            DateTime now = DateTime.Now;

            string content = isNewLine ? string.Empty : $"{now.ToString("yyyy-MM-dd HH:mm:ss.fff")}({logType}):{message}";

            lock (obj)
            {
                File.AppendAllLines(filePath, new string[] { content });
            }
        }
    }
}
