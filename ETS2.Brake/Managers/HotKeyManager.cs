using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;

namespace ETS2.Brake.Managers
{
    /// <summary>
    ///     Manages hotkeys
    /// </summary>
    public static partial class HotKeyManager
    {
        private static MessageWindow _wnd;

        private static Dictionary<Keys, bool> KeysPressed { get; } = new Dictionary<Keys, bool>();

        static HotKeyManager()
        {
            var uiThread = new Thread(() => Application.Run(new MessageWindow()));
            uiThread.SetApartmentState(ApartmentState.STA);
            uiThread.Start();
        }

        /// <summary>
        ///     Adds a key to the watch list
        /// </summary>
        /// <param name="key"></param>
        /// <exception cref="Exception"></exception>
        public static void Add(Keys key)
        {
            if (_wnd != null)
                _wnd.GlobalKeyboardHook.HookedKeys.Add(key);
            else
                throw new Exception("Please use the event loaded");
        }

        /// <summary>
        ///     Fires when a key is pressed. (Can repeat)
        /// </summary>
        public static event EventHandler<KeyEventArgs> HotKeyPressedDown;

        /// <summary>
        ///     Fires when a key is released
        /// </summary>
        public static event EventHandler<KeyEventArgs> HotKeyPressedUp;

        /// <summary>
        ///     Gets if a certain key is pressed
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool IsPressed(this Keys key) => KeysPressed.ContainsKey(key) && KeysPressed[key];

        /// <summary>
        ///     Fires when the form finishes loading
        /// </summary>
        public static event EventHandler Loaded;

        private static void OnHotKeyPressedDown(KeyEventArgs e)
        {
            HotKeyPressedDown?.Invoke(null, e);
        }

        private static void OnHotKeyPressedUp(KeyEventArgs e)
        {
            HotKeyPressedUp?.Invoke(null, e);
        }

        private static void OnLoaded()
        {
            Loaded?.Invoke(null, EventArgs.Empty);
        }
    }
}