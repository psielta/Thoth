using System.Runtime.InteropServices;

namespace Thoth.Infrastructure.Terminals;

internal static class WindowsUserEnvironmentBlock
{
    public static Dictionary<string, string>? TryCreate()
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        var sessionId = WTSGetActiveConsoleSessionId();
        if (sessionId == 0xFFFFFFFF)
        {
            return null;
        }

        if (!WTSQueryUserToken(sessionId, out var token) || token == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            if (!CreateEnvironmentBlock(out var environmentBlock, token, false) || environmentBlock == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                return ParseEnvironmentBlock(environmentBlock);
            }
            finally
            {
                DestroyEnvironmentBlock(environmentBlock);
            }
        }
        finally
        {
            CloseHandle(token);
        }
    }

    private static Dictionary<string, string> ParseEnvironmentBlock(IntPtr environmentBlock)
    {
        var environment = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var cursor = environmentBlock;

        while (true)
        {
            var entry = Marshal.PtrToStringUni(cursor);
            if (string.IsNullOrEmpty(entry))
            {
                break;
            }

            var separatorIndex = entry.IndexOf('=');
            if (separatorIndex > 0)
            {
                environment[entry[..separatorIndex]] = entry[(separatorIndex + 1)..];
            }

            cursor = IntPtr.Add(cursor, (entry.Length + 1) * 2);
        }

        return environment;
    }

    [DllImport("kernel32.dll")]
    private static extern uint WTSGetActiveConsoleSessionId();

    [DllImport("wtsapi32.dll", SetLastError = true)]
    private static extern bool WTSQueryUserToken(uint sessionId, out IntPtr token);

    [DllImport("userenv.dll", SetLastError = true)]
    private static extern bool CreateEnvironmentBlock(out IntPtr environment, IntPtr token, bool inherit);

    [DllImport("userenv.dll", SetLastError = true)]
    private static extern bool DestroyEnvironmentBlock(IntPtr environment);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr handle);
}