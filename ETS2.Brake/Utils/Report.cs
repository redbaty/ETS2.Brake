using System;
using System.Drawing;
using Console = Colorful.Console;

namespace ETS2.Brake.Utils
{
    internal static class Report
    {
        public static void Debug(string message)
        {
            Any("Debug", Color.Gray, message, Color.Gray);
        }

        public static void Error(string message)
        {
            Any("Error", Color.Red, message, Color.Gray);
        }

        public static void Info(string message)
        {
            Any("Info", Color.CornflowerBlue, message, Color.Gray);
        }

        public static void Success(string message)
        {
            Any("Success", Color.LimeGreen, message, Color.Gray);
        }

        public static void Warning(string message)
        {
            Any("Warn", Color.Yellow, message, Color.Gray);
        }

        private static void Any(string type, Color typeColor, string message, Color messageColor)
        {
            Console.Write($"[{DateTime.Now.ToLongTimeString()}]", Color.Gray);
            Console.Write($"[{type}] ", typeColor);
            Console.Write(message, messageColor);
            Console.Write("\n");
        }
    }
}