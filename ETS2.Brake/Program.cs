using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Capture;
using Capture.Hook;
using Capture.Interface;

namespace ETS2.Brake
{
    class Program
    {
        public static int MaxValue => (int) _maxValue;

        private static uint Id = 1;
        private static long _maxValue;

        private static int ByPercentage(int percentage) => Math.ByPercentage(percentage, MaxValue);

        private static int CurrentBreakAmount
        {
            get => _currentBreakAmount;
            set
            {
                _currentBreakAmount = value;
                JoystickManager.Joystick.SetAxis(
                    ByPercentage((int) ((decimal) value / MaximumBreakAmount * 100)), Id,
                    HID_USAGES.HID_USAGE_X);
                Report.Info($"Break amount set to {value}/{MaximumBreakAmount}");

                //█
                var progressValue = Math.ByPercentage(((decimal) value / MaximumBreakAmount * 100), 10);
                var progressString = "";

                for (int i = 0; i < progressValue; i++)
                {
                    progressString += "█";
                }

                if (progressString.Length < 10)
                {
                    var delta = 10 - progressString.Length;
                    for (int i = 0; i < delta; i++)
                    {
                        progressString += "░";
                    }
                }

                _captureProcess.CaptureInterface.SetFps($"{progressString} {progressValue * 10}%");
            }
        }

        private static int MaximumBreakAmount = 5;
        private static int _currentBreakAmount;

        [STAThread]
        static void Main(string[] args)
        {
            Colorful.Console.WriteAscii("ETS2 Brake Sys", Color.MediumSpringGreen);

            if (!JoystickManager.ValidateAndStart())
                return;

            Report.Success("Joystick have been acquired");
            ResetJoystick();

            HotKeyManager.Loaded += (sender, eventArgs) =>
            {
                HotKeyManager.Add(Keys.S);
                HotKeyManager.Add(Keys.W);
                HotKeyManager.Add(Keys.X);
                HotKeyManager.HotKeyPressedDown += HotKeyManagerOnHotKeyPressedUp;
                Report.Success("Hotkeys loaded and applied");
            };

            ConsoleManager.Enable();
            ConsoleManager.ConsoleClosing += ConsoleManagerOnConsoleClosing;
            AttachProcess("eurotrucks2");


            while (true)
            {
                while (Console.KeyAvailable) Console.ReadKey(true);
            }
        }

        static int processId = 0;
        static Process _process;
        static CaptureProcess _captureProcess;

        private static void AttachProcess(string processname)
        {
            var exeName = Path.GetFileNameWithoutExtension(processname);

            var processes = Process.GetProcessesByName(exeName);
            foreach (var process in processes)
            {
                // Simply attach to the first one found.

                // If the process doesn't have a mainwindowhandle yet, skip it (we need to be able to get the hwnd to set foreground etc)
                if (process.MainWindowHandle == IntPtr.Zero)
                {
                    continue;
                }

                // Skip if the process is already hooked (and we want to hook multiple applications)
                if (HookManager.IsHooked(process.Id))
                {
                    continue;
                }

                const Direct3DVersion direct3DVersion = Direct3DVersion.AutoDetect;
                var cc = new CaptureConfig
                {
                    Direct3DVersion = direct3DVersion,
                    ShowOverlay = true
                };

                processId = process.Id;
                _process = process;

                var captureInterface = new CaptureInterface();
                captureInterface.RemoteMessage += CaptureInterface_RemoteMessage;
                _captureProcess = new CaptureProcess(process, cc, captureInterface);
     
                break;
            }

            if (_captureProcess == null)
            {
                Report.Error("Could not find euro truck process. Hook is offline");
            }
            else
            {
                Report.Success("Hook is online");

          
            }
        }

        private static void CaptureInterface_RemoteMessage(MessageReceivedEventArgs message)
        {
            Report.Info(message.Message);
        }

        private static void ResetJoystick()
        {
            JoystickManager.Joystick.ResetVJD(Id);
            JoystickManager.Joystick.GetVJDAxisMax(Id, HID_USAGES.HID_USAGE_X, ref _maxValue);
            JoystickManager.Joystick.SetAxis(0, Id, HID_USAGES.HID_USAGE_X);
        }

        private static void ConsoleManagerOnConsoleClosing(object o, EventArgs args)
        {
            ResetJoystick();
        }

        private static void HotKeyManagerOnHotKeyPressedUp(object sender, KeyEventArgs hotKeyEventArgs)
        {
            if (WindowsUtils.GetActiveWindowTitle() == "Euro Truck Simulator 2")
            {
                // ReSharper disable once ConvertIfStatementToSwitchStatement
                if (hotKeyEventArgs.KeyCode == Keys.S)
                {
                    if (CurrentBreakAmount < MaximumBreakAmount)
                        CurrentBreakAmount++;
                }
                else if (hotKeyEventArgs.KeyCode == Keys.W && CurrentBreakAmount > 0)
                    CurrentBreakAmount = 0;
            }
        }
    }
}