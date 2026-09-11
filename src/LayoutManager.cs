using System.Text;

namespace KeyboardLangFixer;

internal enum AppLanguage
{
    English,
    Persian
}

internal sealed class LayoutManager
{
    private const ushort LangIdEnglishUs = 0x0409;
    private const ushort LangIdPersian = 0x0429;

    public IntPtr EnglishLayout { get; private set; }
    public IntPtr PersianLayout { get; private set; }

    public bool BothLayoutsAvailable => EnglishLayout != IntPtr.Zero && PersianLayout != IntPtr.Zero;

    public void DetectInstalledLayouts()
    {
        int count = NativeMethods.GetKeyboardLayoutList(0, Array.Empty<IntPtr>());
        if (count <= 0) return;

        var list = new IntPtr[count];
        NativeMethods.GetKeyboardLayoutList(count, list);

        foreach (var hkl in list)
        {
            ushort langId = (ushort)((long)hkl & 0xFFFF);
            if (langId == LangIdEnglishUs && EnglishLayout == IntPtr.Zero)
                EnglishLayout = hkl;
            else if (langId == LangIdPersian && PersianLayout == IntPtr.Zero)
                PersianLayout = hkl;
        }
    }

    public AppLanguage? GetLanguageOf(IntPtr hkl)
    {
        ushort langId = (ushort)((long)hkl & 0xFFFF);
        if (langId == LangIdEnglishUs) return AppLanguage.English;
        if (langId == LangIdPersian) return AppLanguage.Persian;
        return null;
    }

    public IntPtr GetForegroundLayout()
    {
        var hwnd = NativeMethods.GetForegroundWindow();
        uint threadId = NativeMethods.GetWindowThreadProcessId(hwnd, out _);
        return NativeMethods.GetKeyboardLayout(threadId);
    }

    public void RequestLayoutSwitch(IntPtr hkl)
    {
        var hwnd = NativeMethods.GetForegroundWindow();
        NativeMethods.PostMessage(hwnd, NativeMethods.WM_INPUTLANGCHANGEREQUEST, IntPtr.Zero, hkl);
    }

    private readonly StringBuilder _buf = new(8);

    public string? TranslateKey(uint vk, uint scanCode, byte[] keyState, IntPtr hkl)
    {
        _buf.Clear();
        int result = NativeMethods.ToUnicodeEx(vk, scanCode, keyState, _buf, _buf.Capacity, 0, hkl);

        if (result < 0)
        {
            // Dead-key state got latched into the layout; issue a throwaway call to clear it.
            var clearBuf = new StringBuilder(8);
            NativeMethods.ToUnicodeEx(NativeMethods.VK_SPACE, 0, keyState, clearBuf, clearBuf.Capacity, 0, hkl);
            return null;
        }

        if (result <= 0) return null;
        return _buf.ToString();
    }
}
