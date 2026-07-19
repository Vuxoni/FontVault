using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace FontVault.Services;

public static class FontService
{
    private const uint WM_FONTCHANGE = 0x001D;
    private static readonly IntPtr HWND_BROADCAST = new(0xFFFF);
    private static readonly ConcurrentDictionary<string, int> LoadedFonts =
        new(StringComparer.OrdinalIgnoreCase);

    [DllImport("gdi32.dll",
        EntryPoint = "AddFontResourceExW",
        CharSet = CharSet.Unicode,
        SetLastError = true)]
    private static extern int AddFontResourceEx(
        string fontFilePath,
        uint flags,
        IntPtr reserved);

    [DllImport("gdi32.dll",
        EntryPoint = "RemoveFontResourceExW",
        CharSet = CharSet.Unicode,
        SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RemoveFontResourceEx(
        string fontFilePath,
        uint flags,
        IntPtr reserved);

    [DllImport("user32.dll",
        EntryPoint = "SendNotifyMessageW",
        CharSet = CharSet.Unicode,
        SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SendNotifyMessage(
        IntPtr windowHandle,
        uint message,
        IntPtr wParam,
        IntPtr lParam);

    public static async Task<bool> LoadTemporaryFontAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return false;

        string extension = Path.GetExtension(filePath);

        if (!extension.Equals(".ttf", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".otf", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string normalizedPath = Path.GetFullPath(filePath);

        if (LoadedFonts.ContainsKey(normalizedPath))
            return true;

        int fontsAdded = await Task.Run(() =>
            AddFontResourceEx(
                normalizedPath,
                0, 
                IntPtr.Zero));

        if (fontsAdded <= 0)
            return false;

        LoadedFonts[normalizedPath] = fontsAdded;
        NotifyApplications();

        return true;
    }

    public static async Task<bool> UnloadTemporaryFontAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;

        string normalizedPath = Path.GetFullPath(filePath);

        if (!LoadedFonts.TryGetValue(normalizedPath, out int loadCount))
            return false;

        bool allRemoved = true;

        for (int i = 0; i < loadCount; i++)
        {
            bool removed = await Task.Run(() =>
                RemoveFontResourceEx(
                    normalizedPath,
                    0,
                    IntPtr.Zero));

            if (!removed)
            {
                allRemoved = false;
                break;
            }
        }

        if (!allRemoved)
            return false;

        LoadedFonts.TryRemove(normalizedPath, out _);
        NotifyApplications();

        return true;
    }

    public static async Task UnloadAllTemporaryFontsAsync()
    {
        string[] paths = LoadedFonts.Keys.ToArray();

        foreach (string path in paths)
        {
            await UnloadTemporaryFontAsync(path);
        }
    }

    public static bool IsLoaded(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;

        return LoadedFonts.ContainsKey(Path.GetFullPath(filePath));
    }

    private static void NotifyApplications()
    {
        SendNotifyMessage(
            HWND_BROADCAST,
            WM_FONTCHANGE,
            IntPtr.Zero,
            IntPtr.Zero);
    }
}