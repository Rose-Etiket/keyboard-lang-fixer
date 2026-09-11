namespace KeyboardLangFixer;

internal sealed class TypingMonitor
{
    private readonly LayoutManager _layouts;
    private readonly WordDictionaries _dictionaries;
    private readonly List<KeyRecord> _buffer = new();
    private IntPtr _lastForegroundWindow = IntPtr.Zero;

    // Only ever check/correct the first word typed right after the user places
    // the caret (clicks, switches window, or moves with arrow/Home/End/etc.) —
    // never mid-sentence. Guessing wrong once at the start is a minor annoyance;
    // guessing wrong (or switching the layout) in the middle of a sentence the
    // user is deliberately typing in a second language is a real, disruptive bug.
    private bool _armed = true;

    private const int MaxBufferedKeys = 25;

    public event Action<string, string>? CorrectionMade; // (wrong, fixed) — for diagnostics/logging

    public TypingMonitor(LayoutManager layouts, WordDictionaries dictionaries)
    {
        _layouts = layouts;
        _dictionaries = dictionaries;
    }

    public void Arm() => _armed = true;

    public void OnBackspacePressed()
    {
        if (_buffer.Count > 0)
            _buffer.RemoveAt(_buffer.Count - 1);
    }

    public void OnKeyTyped(uint vk, uint scanCode, byte[] keyState)
    {
        var currentWindow = NativeMethods.GetForegroundWindow();
        if (currentWindow != _lastForegroundWindow)
        {
            _lastForegroundWindow = currentWindow;
            _buffer.Clear();
            _armed = true;
        }

        var currentHkl = _layouts.GetForegroundLayout();

        // Word-boundary keys, regardless of what they'd translate to.
        if (vk == NativeMethods.VK_RETURN)
        {
            HandleWordBoundary(currentHkl, "\r");
            return;
        }
        if (vk == NativeMethods.VK_TAB)
        {
            HandleWordBoundary(currentHkl, "\t");
            return;
        }
        if (vk == NativeMethods.VK_SPACE)
        {
            HandleWordBoundary(currentHkl, " ");
            return;
        }

        if (IsNavigationOrControlKey(vk))
        {
            _buffer.Clear();
            _armed = true; // the caret may have moved — treat next word as a fresh start
            return;
        }

        var translated = _layouts.TranslateKey(vk, scanCode, keyState, currentHkl);
        if (translated is null || translated.Length == 0)
            return; // dead key or unmapped — ignore

        if (translated.Length == 1 && IsPunctuationBoundary(translated[0]))
        {
            HandleWordBoundary(currentHkl, translated);
            return;
        }

        _buffer.Add(new KeyRecord { Vk = vk, ScanCode = scanCode, KeyState = keyState });
        if (_buffer.Count > MaxBufferedKeys)
            _buffer.Clear(); // too long to be a real word — likely a code/URL/password, leave it alone
    }

    private void HandleWordBoundary(IntPtr currentHkl, string boundaryText)
    {
        if (_armed)
        {
            ProcessWord(currentHkl, boundaryText);
            _armed = false; // never touch subsequent words in the same typing streak
        }
        _buffer.Clear();
    }

    private void ProcessWord(IntPtr currentHkl, string boundaryText)
    {
        if (_buffer.Count == 0) return;
        if (!_layouts.BothLayoutsAvailable) return;

        var currentLang = _layouts.GetLanguageOf(currentHkl);
        if (currentLang is null) return; // some other layout we don't handle

        var otherLang = currentLang == AppLanguage.English ? AppLanguage.Persian : AppLanguage.English;
        var otherHkl = currentLang == AppLanguage.English ? _layouts.PersianLayout : _layouts.EnglishLayout;

        var actualWord = BuildWord(currentHkl);
        if (string.IsNullOrEmpty(actualWord)) return;

        if (_dictionaries.IsValid(currentLang.Value, actualWord))
            return; // already a real word in the language being typed — leave it alone

        var altWord = BuildWord(otherHkl);
        if (string.IsNullOrEmpty(altWord)) return;

        if (!_dictionaries.IsValid(otherLang, altWord))
            return; // neither language recognizes it — don't guess, avoid false corrections

        int backspaceCount = actualWord.Length + boundaryText.Length;
        string replacement = altWord + boundaryText;

        var fgWindow = NativeMethods.GetForegroundWindow();
        var classNameBuf = new System.Text.StringBuilder(256);
        NativeMethods.GetClassName(fgWindow, classNameBuf, classNameBuf.Capacity);
        var titleBuf = new System.Text.StringBuilder(256);
        NativeMethods.GetWindowText(fgWindow, titleBuf, titleBuf.Capacity);
        Logger.Log($"Correcting '{actualWord}' -> '{altWord}' | backspaces={backspaceCount} " +
                   $"| target window class='{classNameBuf}' title='{titleBuf}'");

        // We're still inside the low-level keyboard hook callback here, and the
        // boundary key that triggered this (space/enter/punctuation) has not yet
        // been forwarded past our hook (that happens when HookCallback returns and
        // calls CallNextHookEx) — let alone actually delivered to the foreground
        // app's message queue. Sending the corrective Backspace/retype right now
        // races with that still-pending original keystroke and can land before it,
        // deleting the wrong character or silently doing nothing. Deferring to a
        // background thread with a short delay lets the original keystroke finish
        // its trip through the input pipeline first.
        System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                System.Threading.Thread.Sleep(50);
                uint backSent = InputSender.SendBackspaces(backspaceCount);
                uint textSent = InputSender.SendText(replacement);
                _layouts.RequestLayoutSwitch(otherHkl);

                if (Settings.SoundEnabled)
                    System.Console.Beep(880, 70); // short, soft cue that the language just switched

                Logger.Log($"  -> backspacesSent={backSent}/{backspaceCount} textEventsSent={textSent}");
            }
            catch (Exception ex)
            {
                Logger.Log($"  -> EXCEPTION during correction: {ex}");
            }
        });

        CorrectionMade?.Invoke(actualWord, altWord);
    }

    private string? BuildWord(IntPtr hkl)
    {
        var sb = new System.Text.StringBuilder(_buffer.Count);
        foreach (var key in _buffer)
        {
            var ch = _layouts.TranslateKey(key.Vk, key.ScanCode, key.KeyState, hkl);
            if (string.IsNullOrEmpty(ch)) return null;
            sb.Append(ch);
        }
        return sb.ToString();
    }

    private static bool IsPunctuationBoundary(char c) =>
        c is '.' or ',' or '!' or '?' or ';' or ':' or ')' or '(' or '"' or '\'' or '،' or '؛' or '؟';

    private static bool IsNavigationOrControlKey(uint vk) => vk switch
    {
        0x1B => true,             // Escape
        0x21 or 0x22 => true,     // Page Up/Down
        0x23 or 0x24 => true,     // End/Home
        0x25 or 0x26 or 0x27 or 0x28 => true, // Arrows
        0x2D or 0x2E => true,     // Insert/Delete
        >= 0x70 and <= 0x87 => true, // F1-F24
        _ => false
    };
}
