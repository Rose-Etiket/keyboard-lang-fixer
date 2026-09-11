namespace KeyboardLangFixer;

internal sealed class MouseHook : IDisposable
{
    private const int WH_MOUSE_LL = 14;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_MBUTTONDOWN = 0x0207;

    private readonly NativeMethods.LowLevelMouseProc _proc;
    private IntPtr _hookHandle = IntPtr.Zero;

    public event Action? Clicked;

    public MouseHook()
    {
        _proc = HookCallback;
    }

    public void Install()
    {
        using var curProcess = System.Diagnostics.Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule!;
        _hookHandle = NativeMethods.SetWindowsHookExMouse(
            WH_MOUSE_LL,
            _proc,
            NativeMethods.GetModuleHandle(curModule.ModuleName!),
            0);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            if (nCode >= 0)
            {
                int msg = wParam.ToInt32();
                if (msg is WM_LBUTTONDOWN or WM_RBUTTONDOWN or WM_MBUTTONDOWN)
                {
                    Clicked?.Invoke();
                }
            }
        }
        catch
        {
            // Never let an exception escape a global low-level hook.
        }

        return NativeMethods.CallNextHookExMouse(_hookHandle, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_hookHandle != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_hookHandle);
            _hookHandle = IntPtr.Zero;
        }
    }
}
