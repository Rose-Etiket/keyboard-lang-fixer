namespace KeyboardLangFixer;

internal static class Logger
{
    private static readonly string LogPath = Path.Combine(AppContext.BaseDirectory, "diagnostic.log");
    private static readonly object Lock = new();

    public static void Log(string message)
    {
        if (!Settings.LoggingEnabled) return;

        try
        {
            lock (Lock)
            {
                File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss.fff}] {message}{Environment.NewLine}");
            }
        }
        catch
        {
            // best-effort diagnostics only
        }
    }
}
