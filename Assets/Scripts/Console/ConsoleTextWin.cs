using UnityEngine;
using System;

#if UNITY_STANDALONE_WIN

using System.IO;
using System.Runtime.InteropServices;

public class ConsoleTextWin : IConsoleUI
{
    [DllImport("Kernel32.dll")]
    private static extern bool AttachConsole(uint processId);

    [DllImport("Kernel32.dll")]
    private static extern bool AllocConsole();

    [DllImport("Kernel32.dll")]
    private static extern bool FreeConsole();

    [DllImport("Kernel32.dll")]
    private static extern bool SetConsoleTitle(string title);

    [DllImport("Kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    static extern bool SetForegroundWindow(IntPtr hwnd);

    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    public ConsoleTextWin(string consoleTitle, bool restoreFocus)
    {
        m_RestoreFocus = restoreFocus;
        m_ConsoleTitle = consoleTitle;
    }

    public void Init()
    {
        if (!AttachConsole(0xffffffff))
        {
            if (m_RestoreFocus)
            {
                m_ForegroundWindow = GetForegroundWindow();
                m_ResetWindowTime = Time.time + 1;
            }
            AllocConsole();
        }
        m_PreviousOutput = System.Console.Out;
        SetConsoleTitle(m_ConsoleTitle);
        System.Console.BackgroundColor = System.ConsoleColor.Black;
        System.Console.Clear();
        System.Console.SetOut(new StreamWriter(System.Console.OpenStandardOutput()) { AutoFlush = true });
        m_CurrentLine = "";
        DrawInputline();
    }

    public void Shutdown()
    {
        OutputString("Console shutdown");
        System.Console.SetOut(m_PreviousOutput);
        FreeConsole();
    }

    public void ConsoleUpdate()
    {
        if(m_ForegroundWindow != IntPtr.Zero && Time.time > m_ResetWindowTime)
        {
            ShowWindow(m_ForegroundWindow, 9);
            SetForegroundWindow(m_ForegroundWindow);
            m_ForegroundWindow = IntPtr.Zero;
        }

        if (!System.Console.KeyAvailable)
            return;

        var keyInfo = System.Console.ReadKey();

        switch (keyInfo.Key)
        {
            case ConsoleKey.Enter:
                Console.EnqueueCommand(m_CurrentLine);
                m_CurrentLine = "";
                DrawInputline();
                break;
            case ConsoleKey.Escape:
                m_CurrentLine = "";
                DrawInputline();
                break;
            case ConsoleKey.Backspace:
                if (m_CurrentLine.Length > 0)
                    m_CurrentLine = m_CurrentLine.Substring(0, m_CurrentLine.Length - 1);
                DrawInputline();
                break;
            case ConsoleKey.UpArrow:
                m_CurrentLine = Console.HistoryUp(m_CurrentLine);
                DrawInputline();
                break;
            case ConsoleKey.DownArrow:
                m_CurrentLine = Console.HistoryDown();
                DrawInputline();
                break;
            case ConsoleKey.Tab:
                m_CurrentLine = Console.TabComplete(m_CurrentLine);
                DrawInputline();
                break;
            default:
                {
                    if (keyInfo.KeyChar != '\u0000')
                    {
                        m_CurrentLine += keyInfo.KeyChar;
                        DrawInputline();
                    }
                }
                break;
        }
    }

    public void ConsoleLateUpdate()
    {
    }

    public bool IsOpen()
    {
        return true;
    }

    public void OutputString(string message)
    {
        ClearInputLine();
        System.Console.WriteLine(message);
        DrawInputline();
    }

    public void SetOpen(bool open)
    {
    }

    void ClearInputLine()
    {
        System.Console.CursorLeft = 0;
        System.Console.CursorTop = System.Console.BufferHeight - 1;
        System.Console.BackgroundColor = System.ConsoleColor.Black;
        System.Console.Write(new string(' ', System.Console.BufferWidth - 1));
        System.Console.CursorLeft = 0;
    }

    void DrawInputline()
    {
        System.Console.CursorLeft = 0;
        System.Console.CursorTop = System.Console.BufferHeight - 1;
        System.Console.BackgroundColor = System.ConsoleColor.Blue;
        System.Console.Write(m_Prompt + m_CurrentLine + new string(' ', System.Console.BufferWidth - m_CurrentLine.Length - m_Prompt.Length - 1));
        System.Console.CursorLeft = m_CurrentLine.Length + m_Prompt.Length;
    }

    public void SetPrompt(string prompt)
    {
        m_Prompt = prompt;
    }

    string m_Prompt = "";
    bool m_RestoreFocus;
    string m_ConsoleTitle;
    float m_ResetWindowTime;
    IntPtr m_ForegroundWindow;

    string m_CurrentLine;
    TextWriter m_PreviousOutput;
}
#endif
