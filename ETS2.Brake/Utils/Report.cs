﻿using System;
using System.Drawing;
using Console = Colorful.Console;

namespace ETS2.Brake.Utils
{
    internal static class Report
    {
        public static void Error(string message)
        {
            Any("Error", Color.Red, message, Color.Gray);
        }

        public static void Success(string message)
        {
            Any("Success", Color.LimeGreen, message, Color.Gray);
        }

        public static void Info(string message)
        {
            Any("Info", Color.CornflowerBlue, message, Color.Gray);
        }

        public static void Any(string type, Color typeColor, string message, Color messageColor)
        {
            Console.Write($"[{DateTime.Now.ToLongTimeString()}]", Color.Gray);
            Console.Write($"[{type}] ", typeColor);
            Console.Write(message, messageColor);
            Console.Write("\n");
        }
    }
}