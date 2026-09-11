using System.Runtime.InteropServices;

namespace KeyboardLangFixer;

internal sealed class KeyRecord
{
    public required uint Vk { get; init; }
    public required uint ScanCode { get; init; }
    public required byte[] KeyState { get; init; }
}

internal sealed class KeyboardHook : IDisposable
{
    // Keep a strong reference to the delegate for the lifetime of the hook,
    // otherwise the GC can collect it out from under SetWindowsHookEx.
    private readonly NativeMethods.LowLevelKeyboardProc _proc;
    private IntPtr _hookHandle = IntPtr.Zero;

    public event Action<uint, uint, byte[]>? KeyTyped;   // vk, scanCode, keyState snapshot
    public event Action? BackspacePressed;

    public KeyboardHook()
    {
        _proc = HookCallback;
    }

    public void Install()
    {
        using var curProcess = System.Diagnostics.Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule!;
        _hookHandle = NativeMethods.SetWindowsHookEx(
            NativeMethods.WH_KEYBOARD_LL,
            _proc,
            NativeMethods.GetModuleHandle(curModule.ModuleName!),
            0);

        if (_hookHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                "Failed to install keyboard hook: " + Marshal.GetLastWin32Error());
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        // A low-level keyboard hook runs system-wide and on a tight OS timeout budget:
        // any unhandled exception here can make Windows silently disable the hook or
        // stall every process's keyboard input, so this must never throw and must
        // always fall through to CallNextHookEx.
        try
        {
            if (nCode >= 0)
            {
                var data = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);
                bool isInjected = (data.flags & NativeMethods.LLKHF_INJECTED) != 0;
                int msg = wParam.ToInt32();

                if (!isInjected && (msg == NativeMethods.WM_KEYDOWN || msg == NativeMethods.WM_SYSKEYDOWN))
                {
                    if (data.vkCode == NativeMethods.VK_BACK)
                    {
                        BackspacePressed?.Invoke();
                    }
                    else if (!IsModifierKey(data.vkCode))
                    {
                        var keyState = new byte[256];
                        NativeMethods.GetKeyboardState(keyState);
                        KeyTyped?.Invoke(data.vkCode, data.scanCode, keyState);
                    }
                }
            }
        }
        catch
        {
            // Swallow — see note above. Worst case this keystroke isn't analyzed.
        }

        return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    private static bool IsModifierKey(uint vk) => vk switch
    {
        NativeMethods.VK_SHIFT or 0xA0 or 0xA1 => true,   // Shift, LShift, RShift
        NativeMethods.VK_CONTROL or 0xA2 or 0xA3 => true, // Ctrl, LCtrl, RCtrl
        NativeMethods.VK_MENU or 0xA4 or 0xA5 => true,    // Alt, LAlt, RAlt
        NativeMethods.VK_CAPITAL => true,
        NativeMethods.VK_LWIN or NativeMethods.VK_RWIN => true,
        0x5D => true, // Apps/Menu key
        _ => false
    };

    public void Dispose()
    {
        if (_hookHandle != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_hookHandle);
            _hookHandle = IntPtr.Zero;
        }
    }
}
