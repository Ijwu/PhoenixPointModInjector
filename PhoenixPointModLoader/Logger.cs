using System;
using System.IO;
using System.Reflection;

namespace PhoenixPointModLoader
{
    internal static class Logger
    {
        internal static string LogPath { get; set; }

        internal static void InitializeLogging(string logFile)
        {
            Logger.LogPath = logFile;
            Version PPMLVersion = Assembly.GetExecutingAssembly().GetName().Version;
            using (var logWriter = File.CreateText(LogPath))
            {
                logWriter.WriteLine($"PPModLoader -- PPML v{PPMLVersion} -- {DateTime.Now}");
            }
        }

        internal static void Log(string message, params object[] formatObjects)
        {
            if (string.IsNullOrEmpty(LogPath)) return;
            using (var logWriter = File.AppendText(LogPath))
            {
                logWriter.WriteLine(DateTime.Now.ToLongTimeString() + " - " + message, formatObjects);
            }
        }
    }
}
