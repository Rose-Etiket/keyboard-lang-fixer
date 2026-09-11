using System.Windows.Forms;

namespace KeyboardLangFixer;

internal sealed class TrayAppContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly KeyboardHook _hook;
    private readonly MouseHook _mouseHook;
    private readonly LayoutManager _layouts = new();
    private readonly WordDictionaries _dictionaries = new();
    private readonly TypingMonitor _monitor;

    public TrayAppContext()
    {
        Settings.Load();
        Logger.Log($"Startup. sizeof(INPUT)={System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.INPUT>()} (expect 40 on x64)");

        _layouts.DetectInstalledLayouts();

        var dataDir = Path.Combine(AppContext.BaseDirectory, "data");
        _dictionaries.Load(dataDir);

        _monitor = new TypingMonitor(_layouts, _dictionaries);
        _monitor.CorrectionMade += OnCorrectionMade;

        _hook = new KeyboardHook();
        _hook.KeyTyped += _monitor.OnKeyTyped;
        _hook.BackspacePressed += _monitor.OnBackspacePressed;
        _hook.Install();

        // A click repositions the caret — the next word typed there is treated as
        // a fresh "start of typing" and is eligible for correction (see TypingMonitor).
        _mouseHook = new MouseHook();
        _mouseHook.Clicked += _monitor.Arm;
        _mouseHook.Install();

        var menu = new ContextMenuStrip();
        var statusItem = new ToolStripMenuItem(BuildStatusText()) { Enabled = false };
        menu.Items.Add(statusItem);
        menu.Items.Add(new ToolStripSeparator());

        var loggingItem = new ToolStripMenuItem("ثبت گزارش تشخیصی (log)")
        {
            CheckOnClick = true,
            Checked = Settings.LoggingEnabled
        };
        loggingItem.Click += (_, _) => Settings.SetLoggingEnabled(loggingItem.Checked);
        menu.Items.Add(loggingItem);

        var soundItem = new ToolStripMenuItem("بوق هنگام تغییر زبان")
        {
            CheckOnClick = true,
            Checked = Settings.SoundEnabled
        };
        soundItem.Click += (_, _) => Settings.SetSoundEnabled(soundItem.Checked);
        menu.Items.Add(soundItem);

        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("خروج", null, (_, _) => ExitApp());

        _trayIcon = new NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Visible = true,
            Text = "Keyboard Language Fixer",
            ContextMenuStrip = menu
        };

        if (!_layouts.BothLayoutsAvailable)
        {
            _trayIcon.ShowBalloonTip(5000, "Keyboard Language Fixer",
                "لایوت فارسی یا انگلیسی روی ویندوز پیدا نشد. برنامه غیرفعال می‌ماند تا هر دو لایوت نصب شوند.",
                ToolTipIcon.Warning);
        }
    }

    private string BuildStatusText() =>
        _layouts.BothLayoutsAvailable
            ? $"فعال ({_dictionaries.EnglishCount} EN / {_dictionaries.PersianCount} FA)"
            : "غیرفعال — لایوت فارسی/انگلیسی پیدا نشد";

    private void OnCorrectionMade(string wrong, string fixedWord)
    {
        Logger.Log($"Correction: '{wrong}' -> '{fixedWord}'");
    }

    private void ExitApp()
    {
        _trayIcon.Visible = false;
        _hook.Dispose();
        _mouseHook.Dispose();
        Application.Exit();
    }
}
