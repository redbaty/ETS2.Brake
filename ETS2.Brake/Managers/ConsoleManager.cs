using System;
using System.Runtime.InteropServices;

namespace ETS2.Brake.Managers
{
    internal static partial class ConsoleManager
    {
        private static EventHandler _handler;

        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

        public static event System.EventHandler ConsoleClosing;

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

        public static void Enable()
        {
            _handler += Handler;
            SetConsoleCtrlHandler(_handler, true);
        }

        private static void OnConsoleClosing()
        {
            ConsoleClosing?.Invoke(null, EventArgs.Empty);
        }

        internal delegate bool EventHandler(Enum.ConsoleManager.CtrlType sig);
    }
}