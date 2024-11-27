namespace WinFormsApp1.Services;

public static class LogService
{
    private static readonly string LogFile = "debug.log";
    private static bool _consoleEnabled;
    
    public static void EnableConsoleOutput()
    {
        _consoleEnabled = true;
    }
    
    public static void Log(string message)
    {
        var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
        
        if (_consoleEnabled)
        {
            Console.WriteLine(logMessage);
        }
        
        // Log to file with full path information
        var fullPath = Path.GetFullPath(LogFile);
        File.AppendAllText(LogFile, $"{logMessage} (Log file: {fullPath}){Environment.NewLine}");
    }
} 