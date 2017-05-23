using System;
using System.Runtime.InteropServices;

namespace ETS2.Brake
{
    static partial class ConsoleManager
    {
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

        internal delegate bool EventHandler(CtrlType sig);
        static EventHandler _handler;

        public static event System.EventHandler ConsoleClosing;

        private static bool Handler(CtrlType sig)
        {
            OnConsoleClosing();
            switch (sig)
            {
                case CtrlType.CTRL_C_EVENT:
                case CtrlType.CTRL_LOGOFF_EVENT:
                case CtrlType.CTRL_SHUTDOWN_EVENT:
                case CtrlType.CTRL_CLOSE_EVENT:
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
    }
}
