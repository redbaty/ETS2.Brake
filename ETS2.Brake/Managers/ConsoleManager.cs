using System;
using System.Runtime.InteropServices;

namespace ETS2.Brake.Managers
{
    internal static class ConsoleManager
    {
        private static EventHandler _handler;

        public static event System.EventHandler ConsoleClosing;

        public static void Enable()
        {
            _handler += Handler;
            SetConsoleCtrlHandler(_handler, true);
        }

        private static bool Handler(Enum.ConsoleManager.CtrlType sig)
        {
            OnConsoleClosing();
            switch (sig)
            {
                case Enum.ConsoleManager.CtrlType.CTRL_C_EVENT:
                case Enum.ConsoleManager.CtrlType.CTRL_LOGOFF_EVENT:
                case Enum.ConsoleManager.CtrlType.CTRL_SHUTDOWN_EVENT:
                case Enum.ConsoleManager.CtrlType.CTRL_CLOSE_EVENT:
                default:
                    return false;
            }
        }

        private static void OnConsoleClosing()
        {
            ConsoleClosing?.Invoke(null, EventArgs.Empty);
        }

        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

        internal delegate bool EventHandler(Enum.ConsoleManager.CtrlType sig);
    }
}