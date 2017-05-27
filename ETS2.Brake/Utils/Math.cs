namespace ETS2.Brake.Utils
{
    internal static class Math
    {
        public static int ByPercentage(decimal percentage, decimal max) => (int) (percentage * max / 100);
    }
}