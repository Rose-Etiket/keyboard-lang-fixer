namespace KeyboardLangFixer;

internal static class InputSender
{
    public static uint SendBackspaces(int count)
    {
        if (count <= 0) return 0;

        var inputs = new NativeMethods.INPUT[count * 2];
        for (int i = 0; i < count; i++)
        {
            inputs[i * 2] = KeyInput(NativeMethods.VK_BACK, down: true);
            inputs[i * 2 + 1] = KeyInput(NativeMethods.VK_BACK, down: false);
        }
        uint sent = NativeMethods.SendInput((uint)inputs.Length, inputs, System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.INPUT>());
        if (sent != inputs.Length)
        {
            int err = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            Logger.Log($"SendBackspaces: requested={inputs.Length} sent={sent} GetLastError={err}");
        }
        return sent;
    }

    public static uint SendText(string text)
    {
        var inputs = new List<NativeMethods.INPUT>(text.Length * 2);

        foreach (char c in text)
        {
            switch (c)
            {
                case '\r':
                case '\n':
                    inputs.Add(KeyInput(NativeMethods.VK_RETURN, down: true));
                    inputs.Add(KeyInput(NativeMethods.VK_RETURN, down: false));
                    break;
                case '\t':
                    inputs.Add(KeyInput(NativeMethods.VK_TAB, down: true));
                    inputs.Add(KeyInput(NativeMethods.VK_TAB, down: false));
                    break;
                default:
                    inputs.Add(UnicodeInput(c, down: true));
                    inputs.Add(UnicodeInput(c, down: false));
                    break;
            }
        }

        var arr = inputs.ToArray();
        uint sent = NativeMethods.SendInput((uint)arr.Length, arr, System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.INPUT>());
        if (sent != arr.Length)
        {
            int err = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            Logger.Log($"SendText('{text}'): requested={arr.Length} sent={sent} GetLastError={err}");
        }
        return sent;
    }

    private static NativeMethods.INPUT KeyInput(int vk, bool down) => new()
    {
        type = NativeMethods.INPUT_KEYBOARD,
        U = new NativeMethods.InputUnion
        {
            ki = new NativeMethods.KEYBDINPUT
            {
                wVk = (ushort)vk,
                wScan = 0,
                dwFlags = down ? 0u : NativeMethods.KEYEVENTF_KEYUP,
                time = 0,
                dwExtraInfo = UIntPtr.Zero
            }
        }
    };

    private static NativeMethods.INPUT UnicodeInput(char c, bool down) => new()
    {
        type = NativeMethods.INPUT_KEYBOARD,
        U = new NativeMethods.InputUnion
        {
            ki = new NativeMethods.KEYBDINPUT
            {
                wVk = 0,
                wScan = c,
                dwFlags = NativeMethods.KEYEVENTF_UNICODE | (down ? 0u : NativeMethods.KEYEVENTF_KEYUP),
                time = 0,
                dwExtraInfo = UIntPtr.Zero
            }
        }
    };
}
