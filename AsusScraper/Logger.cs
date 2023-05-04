using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace AsusScraper;

public static class LogController
{
    private static readonly string BasePath = Directory.GetCurrentDirectory();
    private static readonly string LogBasePath = Path.Combine(BasePath, "Logs");
    private static readonly string LogPath = Path.Combine(LogBasePath, "current.log");

    public static bool LogVerbose = true;

    private static bool _logRunning = true;

    static LogController()
    {
    }

    public static void Init()
    {
        _logRunning = false;

        if (!Directory.Exists(LogBasePath))
        {
            Directory.CreateDirectory(LogBasePath);
        }

        if (File.Exists(LogPath))
        {
            string time = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string newPath = Path.Combine(LogBasePath, $"log_restored_{time}.log");
            File.Move(LogPath, newPath);
        }

        string timeReadable = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        using StreamWriter writer = File.AppendText(LogPath);
        string logHeader = $"=================================\r\n" +
                           $"== AsusScraper Log                ==\r\n" +
                           $"== Time: {timeReadable,19}   ==\r\n" +
                           $"=================================\r\n";

        writer.WriteLine(logHeader);
        writer.Close();

        _logRunning = true;
    }

    public static async Task WriteLine(LogModel model, [CallerMemberName] string callingMethod = null)
    {
        if (!_logRunning) return;

        string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        string callerString = callingMethod != null ? $"[{model.Sender}/{callingMethod}]" : $"{model.Sender}";

        string logLineInfo = $"{time,19} {callerString}";
        string logLine = $"{logLineInfo} - {model.Message}";
        string verboseLineSpacer = new string(' ', logLineInfo.Length);

        using StreamWriter writer = File.AppendText(LogPath);
        await writer.WriteLineAsync(logLine);
        if (LogVerbose && model.VerboseMessage != string.Empty) await writer.WriteLineAsync($"{verboseLineSpacer} - {model.VerboseMessage}");
        writer.Close();
    }

    public static void Flush()
    {
        _logRunning = false;

        string time = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string newPath = Path.Combine(LogBasePath, $"log_{time}.log");
        File.Move(LogPath, newPath);
    }
}