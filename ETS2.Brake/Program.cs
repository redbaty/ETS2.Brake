using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ETS2.Brake.Managers;
using ETS2.Brake.Utils;
using Console = Colorful.Console;

namespace ETS2.Brake
{
    internal static class Program
    {
        private static readonly Settings Settings = new Settings();

        private static void ConsoleManagerOnConsoleClosing(object o, EventArgs args)
        {
            JoystickManager.Reset();
            GameManager.OverlayProcess.OverlayInterface.Disconnect();
        }

        /// <summary>
        ///     Start <c>HotkeyManager</c>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private static void HotKeyManagerOnLoaded(object sender, EventArgs eventArgs)
        {
            HotKeyManager.Add(Keys.W);
            HotKeyManager.Add(Keys.A);
            HotKeyManager.Add(Keys.S);
            HotKeyManager.Add(Keys.D);
            HotKeyManager.HotKeyPressedDown += GameManager.OnKeyDown;
            HotKeyManager.HotKeyPressedUp += GameManager.OnKeyUp;
            Report.Success("Hotkeys loaded and applied");
        }
        
        [STAThread]
        private static void Main()
        {
            var entryAssembly = Assembly.GetEntryAssembly();

            Console.WriteAscii("ETS2 Brake Sys", Color.MediumSpringGreen);
            Console.WriteLine($"Version: {entryAssembly?.GetName().Version}", Color.LimeGreen);

            if (!Settings.Load("config.json"))
            {
                Report.Info("Do you wish to generate a configuration file (config.json)?");

                while (true)
                {
                    var response = Console.ReadLine().ToLower();
                    if (response == "y" || response == "yes")
                    {
                        try
                        {
                            Settings.Save("config.json");
                        }
                        catch (Exception ex)
                        {
                            Report.Error($"Failed to save config file. {ex.Message}");
                        }
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

            JoystickManager.Reset();
            HotKeyManager.Loaded += HotKeyManagerOnLoaded;
            ConsoleManager.Enable();
            ConsoleManager.ConsoleClosing += ConsoleManagerOnConsoleClosing;
            UpdateManager.CheckForUpdates();
            GameManager.Settings = Settings;

            try
            {
                Settings.Save("config.json");
            }
            catch (Exception ex)
            {
                Report.Error($"Failed to save config file. {ex.Message}");
            }

            if (Settings.IsIncreaseRatioEnabled)
                Settings.ResetIncreaseRatioTimeSpan = new TimeSpan(0, 0, 0, 1, 0);

            GameManager.HookLoop();
        }
    }
}