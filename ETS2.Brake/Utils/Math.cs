using ETS2.Brake.Managers;

namespace ETS2.Brake.Utils
{
    internal static class Math
    {
        public static int ByPercentage(decimal percentage, decimal max) => (int) (percentage * max / 100);

        public static int ByPercentage(int percentage) => Math.ByPercentage(percentage, JoystickManager.MaxValue);
    }
}