namespace KeyboardLangFixer;

internal static class Settings
{
    private static readonly string SettingsPath = Path.Combine(AppContext.BaseDirectory, "settings.txt");

    public static bool LoggingEnabled { get; private set; } = true;
    public static bool SoundEnabled { get; private set; } = true;

    public static void Load()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return;

            foreach (var line in File.ReadAllLines(SettingsPath))
            {
                var parts = line.Split('=', 2);
                if (parts.Length != 2) continue;

                var value = parts[1].Trim();
                switch (parts[0].Trim())
                {
                    case "LoggingEnabled":
                        LoggingEnabled = bool.Parse(value);
                        break;
                    case "SoundEnabled":
                        SoundEnabled = bool.Parse(value);
                        break;
                }
            }
        }
        catch
        {
            // fall back to the defaults
        }
    }

    public static void SetLoggingEnabled(bool enabled)
    {
        LoggingEnabled = enabled;
        Save();
    }

    public static void SetSoundEnabled(bool enabled)
    {
        SoundEnabled = enabled;
        Save();
    }

    private static void Save()
    {
        try
        {
            File.WriteAllLines(SettingsPath,
            [
                $"LoggingEnabled={LoggingEnabled}",
                $"SoundEnabled={SoundEnabled}"
            ]);
        }
        catch
        {
            // best effort — worst case a choice doesn't survive a restart
        }
    }
}
