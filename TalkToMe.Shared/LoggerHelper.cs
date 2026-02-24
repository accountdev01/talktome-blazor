using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TalkToMe.Shared
{
    public static class LoggerHelper
    {
        private static readonly string logFolder = Path.Combine(AppContext.BaseDirectory, "logs");
        private static readonly object filelock = new object();
        private static DateTime _lastCleanupDate = DateTime.MinValue;

        /// <summary>
        /// เขียน Log ออกทั้ง Console (สำหรับ docker logs) และไฟล์ (สำหรับ Volume)
        /// </summary>
        public static void WriteLog(string page, string msg)
        {
            string timestamp = DateTime.Now.ToString("MM-dd-yyyy HH:mm:ss.fff", CultureInfo.InvariantCulture);
            string logEntry = $"{timestamp}: [{page}] | {msg}";

            Console.WriteLine(logEntry);

            try
            {
                if (!Directory.Exists(logFolder))
                {
                    Directory.CreateDirectory(logFolder);
                }

                if (_lastCleanupDate.Date != DateTime.Now.Date)
                {
                    CleanupOldLogs();
                    _lastCleanupDate = DateTime.Now;
                }

                var fileName = $"{DateTime.Now.ToString("MM-dd-yyyy", CultureInfo.InvariantCulture)}.log";
                var logFilePath = Path.Combine(logFolder, fileName);

                lock (filelock)
                {
                    File.AppendAllText(logFilePath, logEntry + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CRITICAL ERROR] Logging Failed: {ex.Message}");
            }
        }

        private static void CleanupOldLogs()
        {
            try
            {
                var directoryInfo = new DirectoryInfo(logFolder);
                if (!directoryInfo.Exists) return;

                var files = directoryInfo.GetFiles("*.log")
                                         .OrderBy(f => f.CreationTime)
                                         .ToList();

                int maxFiles = 10;
                if (files.Count > maxFiles)
                {
                    int filesToDelete = files.Count - maxFiles;
                    for (int i = 0; i < filesToDelete; i++)
                    {
                        files[i].Delete();
                        Console.WriteLine($"[INFO] LoggerHelper: Deleted old log file: {files[i].Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] CleanupOldLogs failed: {ex.Message}");
            }
        }
    }
}