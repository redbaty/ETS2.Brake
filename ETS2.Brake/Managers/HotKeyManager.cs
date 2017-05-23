using System;
using System.Threading;
using System.Windows.Forms;
using ETS2.Brake.Utils;

namespace ETS2.Brake.Managers
{
    public static class HotKeyManager
    {
        private static MessageWindow _wnd;

        static HotKeyManager()
        {
            var t = new Thread(() =>
            {
                _wnd = new MessageWindow();
                Application.Run(_wnd);
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
        }

        public static event EventHandler<KeyEventArgs> HotKeyPressedUp;
        public static event EventHandler<KeyEventArgs> HotKeyPressedDown;
        public static event EventHandler Loaded;

        public static void Add(Keys key)
        {
            if (_wnd != null)
                _wnd.GlobalKeyboardHook.HookedKeys.Add(key);
            else
                throw new Exception("Please use the event loaded");
        }

        private static void OnHotKeyPressedUp(KeyEventArgs e)
        {
            HotKeyPressedUp?.Invoke(null, e);
        }

        private static void OnLoaded()
        {
            Loaded?.Invoke(null, EventArgs.Empty);
        }

        private static void OnHotKeyPressedDown(KeyEventArgs e)
        {
            HotKeyPressedDown?.Invoke(null, e);
        }

        private class MessageWindow : Form
        {
            public MessageWindow()
            {
                GlobalKeyboardHook = new GlobalKeyboardHook();
                _wnd = this;

                GlobalKeyboardHook.KeyUp += GkhOnKeyUp;
                GlobalKeyboardHook.KeyDown += GlobalKeyboardHookOnKeyDown;
                OnLoaded();
            }

            public GlobalKeyboardHook GlobalKeyboardHook { get; }


            private void GlobalKeyboardHookOnKeyDown(object sender, KeyEventArgs keyEventArgs)
            {
                OnHotKeyPressedDown(keyEventArgs);
            }

            private void GkhOnKeyUp(object sender, KeyEventArgs keyEventArgs)
            {
                OnHotKeyPressedUp(keyEventArgs);
            }

            protected override void SetVisibleCore(bool value)
            {
                base.SetVisibleCore(false);
            }
        }
    }
}