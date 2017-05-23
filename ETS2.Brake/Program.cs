using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Capture;
using Capture.Hook;
using Capture.Interface;
using ETS2.Brake.Managers;
using ETS2.Brake.Utils;
using Console = Colorful.Console;
using Math = ETS2.Brake.Utils.Math;

namespace ETS2.Brake
{
    internal static class Program
    {
        private const uint Id = 1;
        private const int MaximumBreakAmount = 5;

        private static long _maxValue;
        private static int _currentBreakAmount;
        private static CaptureProcess _captureProcess;

        private static int MaxValue => (int) _maxValue;

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

                var progressValue = Math.ByPercentage((decimal) value / MaximumBreakAmount * 100, 10);
                var progressString = "";

                for (var i = 0; i < progressValue; i++)
                    progressString += "█";

                if (progressString.Length < 10)
                {
                    var delta = 10 - progressString.Length;
                    for (var i = 0; i < delta; i++)
                        progressString += "░";
                }

                _captureProcess.CaptureInterface.SetText($"{progressString} {progressValue * 10}%");
            }
        }

        private static int ByPercentage(int percentage)
        {
            return Math.ByPercentage(percentage, MaxValue);
        }

        [STAThread]
        private static void Main(string[] args)
        {
            Console.WriteAscii("ETS2 Brake Sys", Color.MediumSpringGreen);

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
            while (System.Console.KeyAvailable) System.Console.ReadKey(true);
        }

        private static void AttachProcess(string processname)
        {
            var exeName = Path.GetFileNameWithoutExtension(processname);

            var processes = Process.GetProcessesByName(exeName);
            foreach (var process in processes)
            {
                if (process.MainWindowHandle == IntPtr.Zero)
                    continue;
                
                if (HookManager.IsHooked(process.Id))
                    continue;

                const Direct3DVersion direct3DVersion = Direct3DVersion.AutoDetect;
                var cc = new CaptureConfig
                {
                    Direct3DVersion = direct3DVersion,
                    ShowOverlay = true
                };

                var captureInterface = new CaptureInterface();
                captureInterface.RemoteMessage += CaptureInterface_RemoteMessage;
                _captureProcess = new CaptureProcess(process, cc, captureInterface);

                break;
            }

            if (_captureProcess == null)
                Report.Error("Could not find euro truck process. Hook is offline");
            else
                Report.Success("Hook is online");
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
                if (hotKeyEventArgs.KeyCode == Keys.S)
                {
                    if (CurrentBreakAmount < MaximumBreakAmount)
                        CurrentBreakAmount++;
                }
                else if (hotKeyEventArgs.KeyCode == Keys.W && CurrentBreakAmount > 0)
                {
                    CurrentBreakAmount = 0;
                }
        }
    }
}