using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

#if UNITY_STANDALONE_LINUX

using System.IO;
using System.Runtime.InteropServices;

public class ConsoleTextLinux : IConsoleUI
{
    public void Init()
    {
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

        if(message.Length > 0 && message[0] == '>')
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

    void ClearInputLine()
    {
        System.Console.CursorLeft = 0;
        System.Console.CursorTop = System.Console.BufferHeight - 1;
        System.Console.Write(new string(' ', System.Console.BufferWidth - 1));
        System.Console.CursorLeft = 0;
    }

    void DrawInputline()
    {
        System.Console.CursorLeft = 0;
        System.Console.CursorTop = System.Console.BufferHeight - 1;
        System.Console.Write(m_CurrentLine + new string(' ', System.Console.BufferWidth - m_CurrentLine.Length - 1));
        System.Console.CursorLeft = m_CurrentLine.Length;
    }

    string m_CurrentLine;
    //TextWriter m_PreviousOutput;
}
#endif
