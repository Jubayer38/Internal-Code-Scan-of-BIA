using BIA.Entity.Collections;

namespace BIA.Helper
{
    public static class LogPathHelper
    {
        public static string GetExLogPath()
        {
            // Base directory of the application
            string baseDir = AppContext.BaseDirectory;

            // Cross-platform log path
            string logFolder = Path.Combine(baseDir, "ExLogs");

            // Ensure directory exists
            if (!Directory.Exists(logFolder))
            {
                Directory.CreateDirectory(logFolder);
            }

            return logFolder;
        }
        public static string GetAPILogPath()
        {
            // Base directory of the application
            string baseDir = AppContext.BaseDirectory;

            // Cross-platform log path
            string logFolder = Path.Combine(baseDir, "ApiLogs");

            // Ensure directory exists
            if (!Directory.Exists(logFolder))
            {
                Directory.CreateDirectory(logFolder);
            }

            return logFolder;
        }
        public static string GetDebugLogPath()
        {
            // Base directory of the application
            string baseDir = AppContext.BaseDirectory;

            // Cross-platform log path
            string logFolder = Path.Combine(baseDir, "DebugLogs");

            // Ensure directory exists
            if (!Directory.Exists(logFolder))
            {
                Directory.CreateDirectory(logFolder);
            }

            return logFolder;
        }
    }

}
