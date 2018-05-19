using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace TradeInfo
{
    class InputParser
    {
        private static char c;
        private static string buffer,pressed;

        public static string parseKeyPress() {
            string keyRead = Console.ReadKey(true).KeyChar.ToString();
            if (keyRead != null) Globals.focusTimer = 5000;
            return keyRead;

        }

        public static string parseLine() { return parseLine(false); }

        public static string parseLine(bool hide)
        {
            buffer = "";
            int curX_start = Console.CursorLeft;
            int curY_start = Console.CursorTop;
            while ((c = (char)Console.ReadKey(hide).KeyChar) != (char)13)
            {
                if (c == (char)8 && buffer.Length>0)
                {
                    buffer = buffer.Substring(0, buffer.Length - 1);
                    Console.SetCursorPosition(curX_start, curY_start);
                    if(!hide)Console.Write(buffer+" ");
                    Console.SetCursorPosition(Console.CursorLeft-1, Console.CursorTop);
                }
                else buffer += c;
                
            }
            return buffer;
        }

        public static void pressAnyKey()
        {
            Globals.consoleMsg("<type>i</type>Press any key to continue...");
            while (Console.ReadKey(true) == null)
            {
                Thread.Sleep(1000);
            }
        }
        public static void pressKey(ConsoleKey key)
        {
            while (Console.ReadKey(true).Key != key)
            {
                Thread.Sleep(1000);
            }
        }
    }
}
