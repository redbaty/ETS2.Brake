﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ETS2.Brake.Managers;
using ETS2.Brake.Utils;
using Overlay;
using Overlay.Hook;
using Overlay.Interface;
using Console = Colorful.Console;
using Math = ETS2.Brake.Utils.Math;

namespace ETS2.Brake
{
    internal static class Program
    {
        private const uint Id = 1;

        private static bool IsRunning { get; set; }

        private static readonly Settings Settings = new Settings();
        private static CancellationTokenSource _resetToken = new CancellationTokenSource();
        private static OverlayProcess _overlayProcess;

        private static long _maxValue;
        private static decimal _currentBreakAmount;

        private static decimal CurrentBreakAmount
        {
            get => _currentBreakAmount;
            set
            {
                if (value > Settings.MaximumBreakAmount)
                    value = Settings.MaximumBreakAmount;

                _currentBreakAmount = value;
                var percentageValue = (value / Settings.MaximumBreakAmount) * 100;
                JoystickManager.Joystick.SetAxis(
                    ByPercentage((int) percentageValue), Id,
                    HID_USAGES.HID_USAGE_X);

                _overlayProcess?.OverlayInterface.SetProgress((int) percentageValue);
                _overlayProcess?.OverlayInterface.SetText($"{percentageValue / 100:P}");
            }
        }

        private static int ByPercentage(int percentage)
        {
            return Math.ByPercentage(percentage, _maxValue);
        }

        [STAThread]
        private static void Main()
        {
            Console.WriteAscii("ETS2 Brake Sys", Color.MediumSpringGreen);

            if (!Settings.Load("config.json"))
            {
                Report.Info("Do you wish to generate a configuration file (config.json)?");

                while (true)
                {
                    var response = Console.ReadLine().ToLower();
                    if (response == "y" || response == "yes")
                    {
                        Settings.Save("config.json");
                        break;
                    }
                    if (response == "n" || response == "n")
                    {
                        Report.Error("Failed loading the configuration file. Using default instead (5)");
                        break;
                    }

                    Report.Error("Sorry, type (y)es/(n)o only");
                }
            }
            if (!JoystickManager.ValidateAndStart())
                return;

            Report.Success("Joystick have been acquired");
            ResetJoystick();

            HotKeyManager.Loaded += HotKeyManagerOnLoaded;
            ConsoleManager.Enable();
            ConsoleManager.ConsoleClosing += ConsoleManagerOnConsoleClosing;
            UpdateManager.CheckForUpdates();
            Settings.Save("config.json");

            if (Settings.IsIncreaseRatioEnabled)
            {
                Settings.MaximumBreakAmount = (int)_maxValue;
                Settings.IncreaseRatio = 150;
            }

            while (true)
            {
                var process = Process.GetProcesses().Where(p => p.ProcessName == "eurotrucks2").ToList();

                var item = process.Any() ? process.First() : null;
                if (item != null && !IsRunning)
                {
                    if (item.MainWindowHandle == IntPtr.Zero)
                        continue;

                    if (HookManager.IsHooked(item.Id))
                        continue;

                    try
                    {
                        AttachProcess("eurotrucks2");
                    }
                    catch
                    {
                        AttachProcess("eurotrucks2");
                    }
                }

                if (item == null && IsRunning)
                {
                    IsRunning = false;
                    ResetJoystick();
                }

                Thread.Sleep(5000);
            }
        }

        private static void HotKeyManagerOnLoaded(object sender, EventArgs eventArgs)
        {
            HotKeyManager.Add(Keys.W);
            HotKeyManager.Add(Keys.A);
            HotKeyManager.Add(Keys.S);
            HotKeyManager.Add(Keys.D);
            HotKeyManager.HotKeyPressedDown += OnKeyDown;
            Report.Success("Hotkeys loaded and applied");
        }

        private static void AttachProcess(string processname)
        {
            IsRunning = true;
            var exeName = Path.GetFileNameWithoutExtension(processname);

            var processes = Process.GetProcessesByName(exeName);
            foreach (var process in processes)
            {
                if (process.MainWindowHandle == IntPtr.Zero)
                    continue;

                if (HookManager.IsHooked(process.Id))
                    continue;

                var cc = new OverlayConfig
                {
                    Direct3DVersion = Direct3DVersion.Unknown,
                    ShowOverlay = true
                };

                var captureInterface = new OverlayInterface();
                captureInterface.RemoteMessage += CaptureInterface_RemoteMessage;
                _overlayProcess = new OverlayProcess(process, cc, captureInterface);
                _overlayProcess.OverlayInterface.SetText($"ETS.Brake loaded");
                break;
            }

            if (_overlayProcess == null)
                Report.Error("Could not find euro truck process. Hook is offline");
            else
                Report.Success("Hook is online");
        }

        private static void CaptureInterface_RemoteMessage(MessageReceivedEventArgs message)
        {
            switch (message.MessageType)
            {
                case MessageType.Debug:
                    if (Debugger.IsAttached)
                        Report.Debug(message.Message);
                    break;
                case MessageType.Information:
                    Report.Info(message.Message);
                    break;
                case MessageType.Warning:
                    Report.Warning(message.Message);
                    break;
                case MessageType.Error:
                    Report.Error(message.Message);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void ResetJoystick()
        {
            JoystickManager.Joystick.ResetVJD(Id);
            JoystickManager.Joystick.GetVJDAxisMax(Id, HID_USAGES.HID_USAGE_X, ref _maxValue);
            CurrentBreakAmount = 0;
        }

        private static void ConsoleManagerOnConsoleClosing(object o, EventArgs args) => ResetJoystick();

        private static void OnKeyDown(object sender, KeyEventArgs keyEventArgs)
        {
            if (WindowsUtils.GetActiveWindowTitle() == "Euro Truck Simulator 2")
                if (Keys.S.IsPressed())
                {
                    _resetToken.Cancel();
                    _resetToken = new CancellationTokenSource();

                    if (CurrentBreakAmount >= Settings.MaximumBreakAmount) return;

                    if (Settings.IsIncreaseRatioEnabled)
                    {
                        var increaseAmount = Settings.IncreaseRatio;
                        Settings.CurrentIncreaseRatio += increaseAmount;
                        CurrentBreakAmount += Settings.CurrentIncreaseRatio;
                    }
                    else
                        CurrentBreakAmount++;

                    Task.Factory.StartNew(() =>
                        {
                            Thread.Sleep(Settings.ResetIncreaseRatioTimeSpan);
                            Settings.CurrentIncreaseRatio = Settings.IncreaseRatio;
                        },
                        _resetToken.Token);
                }
                else if (Keys.W.IsPressed() && CurrentBreakAmount > 0)
                {
                    CurrentBreakAmount = 0;
                    Settings.CurrentIncreaseRatio = Settings.IncreaseRatio;
                }
        }
    }
}