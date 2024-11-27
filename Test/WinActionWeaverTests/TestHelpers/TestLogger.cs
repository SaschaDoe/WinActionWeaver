using System.Runtime.CompilerServices;

namespace WinActionWeaverTests.TestHelpers;

public class TestLogger : IDisposable
{
    private readonly StreamWriter _logFile;
    private readonly string _logPath;

    public TestLogger(string testName)
    {
        var directory = Path.Combine(TestContext.CurrentContext.TestDirectory, "logs");
        Directory.CreateDirectory(directory);
        _logPath = Path.Combine(directory, $"{testName}_{DateTime.Now:yyyyMMdd_HHmmss}.log");
        
        _logFile = new StreamWriter(_logPath, true) { AutoFlush = true };
        
        Log($"Test started at: {DateTime.Now}");
    }

    public void Log(string message, 
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var source = $"{Path.GetFileName(sourceFilePath)}:{sourceLineNumber} in {memberName}";
        var formattedMessage = $"[{timestamp}] [{source}] {message}";
        
        TestContext.WriteLine(formattedMessage);
        _logFile.WriteLine(formattedMessage);
    }

    public void Dispose()
    {
        _logFile.Dispose();
        TestContext.WriteLine($"Full log file: {_logPath}");
    }
} 