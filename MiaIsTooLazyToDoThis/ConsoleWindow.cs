﻿using System;
using System.Runtime.InteropServices;

namespace MiaIsTooLazyToDoThis
{
    static class ConsoleWindow
    {
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public const int SW_HIDE = 0;
        public const int SW_SHOW = 5;
    }
}
