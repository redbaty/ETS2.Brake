using vJoyInterfaceWrap;

namespace ETS2.Brake
{
    class JoystickManager
    {
        private static uint Id = 1;
        public static vJoy Joystick { get; } = new vJoy();

        public static vJoy.JoystickState JoystickReport { get; } = new vJoy.JoystickState();

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

            if (status == VjdStat.VJD_STAT_OWN || status == VjdStat.VJD_STAT_FREE && !Joystick.AcquireVJD(Id))
            {
                Report.Error("Failed to acquire vJoy device number {0}");
                return false;
            }

            return true;
        }
    }
}