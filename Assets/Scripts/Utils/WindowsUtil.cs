using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN

public class WindowsUtil
{
    [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
    private static extern bool SetWindowPos(System.IntPtr hwnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);

    public delegate bool EnumWindowsProc(System.IntPtr hWnd, System.IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc enumProc, System.IntPtr lParam);


    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern int GetWindowThreadProcessId(System.IntPtr handle, out int processId);

    [DllImport("user32.dll")]
    public static extern IntPtr FindWindowEx(IntPtr parentWindow, IntPtr previousChildWindow, string windowClass, string windowTitle);

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(System.IntPtr hwnd, ref Rect rectangle);

    static public IntPtr[] GetProcessWindows(int processId)
    {
        List<IntPtr> output = new List<IntPtr>();
        IntPtr winPtr = IntPtr.Zero;
        do
        {
            winPtr = FindWindowEx(IntPtr.Zero, winPtr, null, null);
            int id;
            GetWindowThreadProcessId(winPtr, out id);
            if (id == processId)
                output.Add(winPtr);
        } while (winPtr != IntPtr.Zero);

        return output.ToArray();
    }

    public struct Rect
    {
        public int Left { get; set; }
        public int Top { get; set; }
        public int Right { get; set; }
        public int Bottom { get; set; }
    }

    public static bool GetProcessRect(System.Diagnostics.Process process, ref Rect rect)
    {
        IntPtr[] winPtrs = WindowsUtil.GetProcessWindows(process.Id);

        for(int i=0;i< winPtrs.Length;i++)
        {
            bool gotRect = WindowsUtil.GetWindowRect(winPtrs[i], ref rect);
            if (gotRect && (rect.Left != 0 && rect.Top != 0))
                return true;
        }
        return false;
    }

    public static void SetWindowPosition(int x, int y, int sizeX = 0, int sizeY = 0)
    {
        System.Diagnostics.Process process = System.Diagnostics.Process.GetCurrentProcess();
        process.Refresh();

        EnumWindows(delegate (System.IntPtr wnd, System.IntPtr param)
        {
            int id;
            GetWindowThreadProcessId(wnd, out id);
            if (id == process.Id)
            {
                SetWindowPos(wnd, 0, x, y, sizeX, sizeY, sizeX * sizeY == 0 ? 1 : 0);
                return false;
            }

            return true;
        }, System.IntPtr.Zero);
    }
}

#endif
