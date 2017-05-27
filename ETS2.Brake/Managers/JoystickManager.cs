using ETS2.Brake.Utils;
using vJoyInterfaceWrap;

namespace ETS2.Brake.Managers
{
    internal static class JoystickManager
    {
        private const uint Id = 1;

        public static int MaxValue { get; private set; }
        private static vJoy Joystick { get; } = new vJoy();

        public static void Reset()
        {
            Joystick.SetAxis(0, Id, HID_USAGES.HID_USAGE_X);
        }

        public static void SetValue(int value)
        {
            Joystick.SetAxis(value, Id, HID_USAGES.HID_USAGE_X);
        }

        public static bool ValidateAndStart()
        {
            if (!Joystick.vJoyEnabled())
            {
                Report.Error("vJoy driver not enabled: Failed Getting vJoy attributes.\n");
                return false;
            }

            var status = Joystick.GetVJDStatus(Id);
            if (status != VjdStat.VJD_STAT_FREE)
            {
                Report.Error("Joystick is not free");
                return false;
            }


            if (status != VjdStat.VJD_STAT_OWN &&
                (status != VjdStat.VJD_STAT_FREE || Joystick.AcquireVJD(Id)))
            {
                long maxValue = 0;
                Joystick.GetVJDAxisMax(Id, HID_USAGES.HID_USAGE_X, ref maxValue);
                MaxValue = (int) maxValue;
                return true;
            }

            Report.Error("Failed to acquire vJoy device number {0}");


            return false;
        }
    }
}