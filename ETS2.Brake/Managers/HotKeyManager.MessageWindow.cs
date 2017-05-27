using System.Windows.Forms;
using ETS2.Brake.Utils;

namespace ETS2.Brake.Managers
{
    public static partial class HotKeyManager
    {
        private class MessageWindow : Form
        {
            public GlobalKeyboardHook GlobalKeyboardHook { get; }

            public MessageWindow()
            {
                GlobalKeyboardHook = new GlobalKeyboardHook();
                _wnd = this;

                GlobalKeyboardHook.KeyUp += GlobalKeyboardHookOnKeyUp;
                GlobalKeyboardHook.KeyDown += GlobalKeyboardHookOnKeyDown;
                OnLoaded();
            }

            protected override void SetVisibleCore(bool value) => base.SetVisibleCore(false);

            private static void AddOrSet(Keys key, bool value)
            {
                if (KeysPressed.ContainsKey(key))
                    KeysPressed[key] = value;
                else
                    KeysPressed.Add(key, value);
            }

            private static void GlobalKeyboardHookOnKeyDown(object sender, KeyEventArgs keyEventArgs)
            {
                AddOrSet(keyEventArgs.KeyCode, true);
                OnHotKeyPressedDown(keyEventArgs);
            }

            private static void GlobalKeyboardHookOnKeyUp(object sender, KeyEventArgs keyEventArgs)
            {
                AddOrSet(keyEventArgs.KeyCode, false);
                OnHotKeyPressedUp(keyEventArgs);
            }
        }
    }
}