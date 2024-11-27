namespace WinFormsApp1;

static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        using var keyboardHook = new KeyboardHook();
        Application.Run();
    }
}