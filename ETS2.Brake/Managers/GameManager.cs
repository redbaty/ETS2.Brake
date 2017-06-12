using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ETS2.Brake.Utils;
using Overlay;
using Overlay.Hook;
using Overlay.Interface;
using Math = ETS2.Brake.Utils.Math;

namespace ETS2.Brake.Managers
{
    internal static class GameManager
    {
        public static Settings Settings = new Settings();
        private static CancellationTokenSource _increaseLoopResetToken = new CancellationTokenSource();
        private static CancellationTokenSource _increaseRatioResetToken = new CancellationTokenSource();
        private static decimal _currentBreakAmount;

        private static bool _isRunning;
        public static OverlayProcess OverlayProcess;

        /// <summary>
        ///     The current break amount
        /// </summary>
        private static decimal CurrentBreakAmount
        {
            get => _currentBreakAmount;
            set
            {
                if (value > JoystickManager.MaxValue)
                    value = JoystickManager.MaxValue;

                _currentBreakAmount = value;
                var percentageValue = value / JoystickManager.MaxValue * 100;
                JoystickManager.SetValue(Math.ByPercentage((int) percentageValue));

                OverlayProcess?.OverlayInterface.SetProgress((int) percentageValue);
                OverlayProcess?.OverlayInterface.SetText($"{percentageValue / 100:0%}");
            }
        }

        /// <summary>
        ///     A list of supported games
        /// </summary>
        private static Dictionary<string, string> SupportedGames { get; } = new Dictionary<string, string>
        {
            {"eurotrucks2", "Euro Truck Simulator 2"},
            {"amtrucks", "American Truck Simulator"}
        };

        /// <summary>
        ///     Gets/sets if the increase loop is running
        /// </summary>
        private static bool IsIncreaseLoopRunning
        {
            set
            {
                _increaseLoopResetToken = new CancellationTokenSource();

                if (value)
                    Task.Factory.StartNew(IncreaseLoop, _increaseLoopResetToken.Token);
                else
                    _increaseLoopResetToken.Cancel();
            }
        }

        /// <summary>
        ///     Gets/sets if the game and hook are running
        /// </summary>
        private static bool IsRunning
        {
            get => _isRunning;
            set
            {
                if (_isRunning && !value)
                    OverlayProcess.OverlayInterface.Disconnect();

                _isRunning = value;

                if (_isRunning)
                    try
                    {
                        var item = Process.GetProcesses().First(p => SupportedGames.Any(y => y.Key == p.ProcessName));
                        AttachProcess(item.ProcessName);
                    }
                    catch
                    {
                        Report.Error("Could not hook the overlay to the game.");
                    }
            }
        }

        /// <summary>
        ///     Attaches the dll to a process.
        /// </summary>
        /// <param name="processname"></param>
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

                var cc = new OverlayConfig
                {
                    Direct3DVersion = Direct3DVersion.Unknown,
                    ShowOverlay = true
                };

                var captureInterface = new OverlayInterface();
                captureInterface.RemoteMessage += OnRemoteMessage;
                OverlayProcess = new OverlayProcess(process, cc, captureInterface);
                OverlayProcess.OverlayInterface.SetText("ETS.Brake loaded");
                break;
            }

            if (OverlayProcess == null)
            {
                Report.Error("Could not find euro truck process. Hook is offline");
            }
            else
            {
                Report.Success("Hook is online");
                OverlayProcess.OverlayInterface.SetMemoryUsageVisibility(!Settings.ShowMemoryUsage);
            }
        }

        /// <summary>
        ///     The hook loop attaches the dll to the process.
        /// </summary>
        public static void HookLoop()
        {
            while (true)
            {
                var process = Process.GetProcesses().Where(p => SupportedGames.Any(y => y.Key == p.ProcessName))
                    .ToList();

                var item = process.Any() ? process.First() : null;

                if (item != null && !IsRunning)
                {
                    if (item.MainWindowHandle == IntPtr.Zero)
                        continue;

                    if (HookManager.IsHooked(item.Id))
                        continue;

                    IsRunning = true;
                }

                if (item == null && IsRunning)
                {
                    IsRunning = false;
                    JoystickManager.Reset();
                }

                item?.Dispose();
                Thread.Sleep(2000);
            }
        }

        /// <summary>
        ///     The <c>CurrentBreakAmount</c> amount increase loop
        /// </summary>
        private static void IncreaseLoop()
        {
            while (true)
            {
                if (_increaseLoopResetToken.IsCancellationRequested)
                    break;

                if (Settings.IsIncreaseRatioEnabled)
                {
                    Settings.CurrentIncreaseRatio += 50;
                    CurrentBreakAmount += Settings.CurrentIncreaseRatio;
                }
                else
                {
                    CurrentBreakAmount++;
                }

                Thread.Sleep(Settings.IncreaseDelay);
            }
        }

        /// <summary>
        ///     On key down (in game)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="keyEventArgs"></param>
        public static void OnKeyDown(object sender, KeyEventArgs keyEventArgs)
        {
            if (SupportedGames.Any(p => p.Value == WindowsUtils.GetActiveWindowTitle()))
            {
                if (Keys.S.IsPressed() && !Keys.W.IsPressed())
                {
                    if (CurrentBreakAmount >= JoystickManager.MaxValue) return;
                    _increaseRatioResetToken.Cancel();
                    _increaseRatioResetToken = new CancellationTokenSource();
                    IsIncreaseLoopRunning = true;
                }

                if (Keys.W.IsPressed() && !Keys.S.IsPressed() && CurrentBreakAmount > 0)
                {
                    IsIncreaseLoopRunning = false;
                    CurrentBreakAmount = 0;
                    Settings.CurrentIncreaseRatio = Settings.StartIncreaseRatio;
                }
            }
        }

        /// <summary>
        ///     On key release (while in game)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="keyEventArgs"></param>
        public static void OnKeyUp(object sender, KeyEventArgs keyEventArgs)
        {
            if (keyEventArgs.KeyCode == Keys.S)
            {
                IsIncreaseLoopRunning = false;
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(Settings.ResetIncreaseRatioTimeSpan);
                    if (!Keys.S.IsPressed())
                        Settings.CurrentIncreaseRatio = Settings.StartIncreaseRatio;
                }, _increaseRatioResetToken.Token);
            }
        }

        private static void OnRemoteMessage(MessageReceivedEventArgs message)
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
    }
}