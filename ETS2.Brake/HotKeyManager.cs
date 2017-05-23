using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using GlobalHotKey;

namespace ETS2.Brake
{
    public static class HotKeyManager
    {
        private static MessageWindow _wnd;

        static HotKeyManager()
        {
            Thread t = new Thread(() =>
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

        class MessageWindow : Form
        {
            public GlobalKeyboardHook GlobalKeyboardHook { get; }


            public MessageWindow()
            {
                GlobalKeyboardHook = new GlobalKeyboardHook();
                _wnd = this;

                GlobalKeyboardHook.KeyUp += GkhOnKeyUp;
                GlobalKeyboardHook.KeyDown += GlobalKeyboardHookOnKeyDown;
                OnLoaded();
            }


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
    }
}