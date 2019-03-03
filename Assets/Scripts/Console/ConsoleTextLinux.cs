using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Threading;

#if UNITY_STANDALONE_LINUX

using System.IO;
using System.Runtime.InteropServices;

public class ConsoleTextLinux : IConsoleUI
{
    bool IsDumb()
    {
        return System.Console.BufferWidth == 0 || System.Console.IsInputRedirected || System.Console.IsOutputRedirected;
    }

    void ReaderThread()
    {

    }

    char[] buf = new char[1024];
    public void Init()
    {
        System.Console.WriteLine("Dumb console: " + IsDumb());
        if (IsDumb())
        {
            m_ReaderThread = new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                while (true)
                {
                    var read = System.Console.In.Read(buf, 0, buf.Length);
                    if (read > 0)
                    {
                        m_CurrentLine += new string(buf, 0, read);
                    }
                    else
                        break;
                }
            });
            m_ReaderThread.Start();
        }
        System.Console.Clear();
        m_CurrentLine = "";
        DrawInputline();
    }

    public void Shutdown()
    {
        OutputString("Console shutdown");
    }

    public void ConsoleUpdate()
    {
        // Handling for cases where the terminal is 'dumb', i.e. cursor etc.
        // and no individual keys fired
        if (IsDumb())
        {
            var lines = m_CurrentLine.Split('\n');
            if (lines.Length > 1)
            {
                for (int i = 0; i < lines.Length - 1; i++)
                    Console.EnqueueCommand(lines[i]);
                m_CurrentLine = lines[lines.Length - 1];
            }
            return;
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

        if (!IsDumb() && message.Length > 0 && message[0] == '>')
        {
            var oldColor = System.Console.ForegroundColor;
            System.Console.ForegroundColor = System.ConsoleColor.Green;
            System.Console.WriteLine(message);
            System.Console.ForegroundColor = oldColor;
        }
        else
            System.Console.WriteLine(message);

        DrawInputline();
    }

    public void SetOpen(bool open)
    {
    }

    public void SetPrompt(string prompt)
    {
    }

    void ClearInputLine()
    {
        if (IsDumb())
            return;

        System.Console.CursorLeft = 0;
        System.Console.CursorTop = System.Console.BufferHeight - 1;
        System.Console.Write(new string(' ', System.Console.BufferWidth - 1));
        System.Console.CursorLeft = 0;
    }

    void DrawInputline()
    {
        if (IsDumb())
            return;

        System.Console.CursorLeft = 0;
        System.Console.CursorTop = System.Console.BufferHeight - 1;
        System.Console.Write(m_CurrentLine + new string(' ', System.Console.BufferWidth - m_CurrentLine.Length - 1));
        System.Console.CursorLeft = m_CurrentLine.Length;
    }

    string m_CurrentLine;
    private Thread m_ReaderThread;
    //TextWriter m_PreviousOutput;
}
#endif
